// VRCLiveCaptionsMod - a mod for providing voice chat live captions
// Copyright(C) 2021  gt0777
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.If not, see<https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VRCLiveCaptionsMod.LiveCaptions.Abstract;
using VRCLiveCaptionsMod.LiveCaptions.GameSpecific;
using VRCLiveCaptionsMod.LiveCaptions.TranscriptData;

#if USE_SHORT
using BUFFER_TYPE = System.Int16;
#else
using BUFFER_TYPE = System.Single;
#endif


namespace VRCLiveCaptionsMod.LiveCaptions {

    /// <summary>
    /// A transcript session that calls the IVoiceRecognizer for voice recognition to generate Sayings
    /// contains a TextGenerator to generate the text from Sayings
    /// and a SubtitleUi to display the text
    /// </summary>
    class TranscriptSession {
        public IAudioSource audioSource;
        private IVoiceRecognizer recognizer = null;
        private SubtitleUi ui;
        
        private List<Saying> past_sayings = new List<Saying>();
        private Saying active_saying;

        private Queue<AudioBuffer> ready_for_processing = new Queue<AudioBuffer>();
        private Queue<AudioBuffer> ready_for_filling = new Queue<AudioBuffer>();
        private List<AudioBuffer> audioBuffers = new List<AudioBuffer>();
        private const int maxAudioBuffers = 16;

        private Mutex inferrenceMutex = new Mutex();
        private Mutex useVoiceRecognizerMutex = new Mutex();

        private int sample_rate;

#if DEBUG
        private TranscriptSessionDebugger debugger;
#endif

        public bool whitelisted { get; private set; } = false;
        public bool disposed { get; private set; } = false;

        public TranscriptSession(IAudioSource src, int sample_rate) {
            last_activity = Utils.GetTime();
            audioSource = src;
            this.sample_rate = sample_rate;

            for(int i=0; i<2; i++) {
                AudioBuffer buff = new AudioBuffer();
                audioBuffers.Add(buff);
                ready_for_filling.Enqueue(audioBuffers[i]);
            }


            while(!useVoiceRecognizerMutex.WaitOne()) { }
            try {
                recognizer = GameUtils.GetVoiceRecognizer();
                if(recognizer != null) recognizer.Init(this.sample_rate);
            } finally {
                useVoiceRecognizerMutex.ReleaseMutex();
            }

            VoiceRecognizerEvents.VoiceRecognizerChanged += () => {
                if(!inferrenceMutex.WaitOne()) return;
                try {
                    CommitSaying();
                    while(!useVoiceRecognizerMutex.WaitOne()) { }
                    try {
                        recognizer = GameUtils.GetVoiceRecognizer();
                        if(recognizer != null) recognizer.Init(this.sample_rate);
                    } finally {
                        useVoiceRecognizerMutex.ReleaseMutex();
                    }
                } finally {
                    inferrenceMutex.ReleaseMutex();
                }
            };

            if(audioSource != null)
                whitelisted = AudioSourceOverrides.IsWhitelisted(audioSource.GetUID());

            AudioSourceOverrides.OnAddedToWhitelist += (uid) => {
                if(audioSource == null) return;
                if(uid.Equals(src.GetUID())) whitelisted = true;
            };

            AudioSourceOverrides.OnRemovedFromWhitelist += (uid) => {
                if(audioSource == null) return;
                if(uid.Equals(src.GetUID())) whitelisted = false;
            };

#if DEBUG
            debugger = new TranscriptSessionDebugger(src.GetFriendlyName());
#endif
        }

        /// <summary>
        /// Runs inference on the active buffer and clears it, updating the active Saying.
        /// This is a very slow operation. It should be run in a background thread.
        /// This should never be run in a foreground thread.
        /// </summary>
        public void RunInference() {
            if(disposed) return;
            if(recognizer == null) return;

            if(ready_for_processing.Count == 0) return;
            AudioBuffer buff = ready_for_processing.Dequeue();

            if(!inferrenceMutex.WaitOne()) return;

            try {
                buff.StartTranscribing();

                bool final;
                while(!useVoiceRecognizerMutex.WaitOne()) { }
                try {
                    IVoiceRecognizer rec = recognizer;

#if DEBUG
                    debugger.onSubmitSamples(buff.buffer, buff.buffer_head);
#endif

                    final = rec.Recognize(buff.buffer, buff.buffer_head);

                    // It's possible after the long operation that we've been disposed, so exit silently
                    if(disposed) return;

                    if(active_saying == null) active_saying = new Saying();
                    active_saying.Update(rec.GetText(), final);
                } finally {
                    useVoiceRecognizerMutex.ReleaseMutex();
                }

                if(final) {
                    CommitSaying();
                    debugger.createMarker(true);
                }
            } finally {
                inferrenceMutex.ReleaseMutex();
                buff.StopTranscribing();
                ready_for_filling.Enqueue(buff);
            }
        }

        private void CommitSaying() {
            if(disposed) return;

            if(active_saying != null) {
                while(!useVoiceRecognizerMutex.WaitOne()) { }
                try {
                    if(!active_saying.final && recognizer != null) {
                        // finalize it
                        recognizer.Flush();
                        active_saying.Update(recognizer.GetText(), true);
#if DEBUG
                        debugger.createMarker();
#endif
                    }
                } finally {
                    useVoiceRecognizerMutex.ReleaseMutex();
                }

                if(active_saying != null && active_saying.fullTxt.Length > 0) {
                    if(past_sayings != null) {
                        past_sayings.Add(active_saying);
                    }
                }
            }
            active_saying = null;
        }

        /// <summary>
        /// This flushes the current audio buffer.
        /// This should be called when there has been some silence.
        /// </summary>
        public void FlushCurrentAudio(){
            if (ready_for_filling.Count == 0) return;

            AudioBuffer buff = ready_for_filling.Peek();
            buff.readWriteMutex.WaitOne();
            try {
                if(buff.buffer_head > 2) {
                    ready_for_filling.Dequeue();
                    buff.queued = true;
                    ready_for_processing.Enqueue(buff);
                }
            } finally {
                buff.readWriteMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Finalizes and flushes the active Saying if it's too old. This should be called
        /// when there has been some silence.
        /// </summary>
        public void CommitSayingIfTooOld() {
            if(active_saying != null && ready_for_processing.Count == 0)
                if((Utils.GetTime() - active_saying.timeEnd) > sepAge) CommitSaying();
        }

        /// <summary>
        /// Returns whether or not the active Saying has any text in it
        /// </summary>
        /// <returns>false if the active Saying is blank, true if it isn't</returns>
        public bool HasWords(){
            return active_saying != null && active_saying.fullTxt.Length > 0;
        }

        /// <summary>
        /// This should be called when the sesson is being destroyed.
        /// </summary>
        public void FullDispose() {
            disposed = true;

            while(!useVoiceRecognizerMutex.WaitOne()) { }
            try {
                recognizer = null;
            } finally {
                useVoiceRecognizerMutex.ReleaseMutex();
            }
            if(ui != null) ui.Dispose();
            ui = null;
            audioBuffers = null;
            past_sayings = null;

#if DEBUG
            debugger.cleanup();
#endif
        }

        /// <summary>
        /// Time in seconds (Utils.GetTime()) when the last samples were submitted
        /// </summary>
        public float last_activity { get; private set; }


        private void EnsureAdditionalBuffers() {
            if(ready_for_filling.Count == 0) {
                if(audioBuffers.Count >= maxAudioBuffers) return;

                AudioBuffer buff = new AudioBuffer();
                buff.lastFillTime = Utils.GetTime();
                ready_for_filling.Enqueue(buff);
                audioBuffers.Add(buff);

                //GameUtils.Log("Allocated 1 extra buffer for " + audioSource.GetFriendlyName() + ". Total count: " + audioBuffers.Count.ToString());
            }
        }

        private float lastCleaning = 0.0f;
        private void DeleteUnnecesssaryBuffersIfNeeded() {
            if(audioBuffers.Count > 2) {
                float time = Utils.GetTime();

                if(time - lastCleaning < 5.0) return;
                lastCleaning = time;

                List<AudioBuffer> to_remove = new List<AudioBuffer>();
                foreach(AudioBuffer buff in audioBuffers) {
                    if(buff.queued || buff.buffer_head > 1) continue;
                    if(!ready_for_filling.Contains(buff)) continue;

                    if((time - buff.lastFillTime) > 10.0) {
                        to_remove.Add(buff);
                    }
                }

                audioBuffers = new List<AudioBuffer>(audioBuffers.Where(x => !to_remove.Contains(x)));
                ready_for_filling = new Queue<AudioBuffer>(ready_for_filling.Where(x => !to_remove.Contains(x)));

                //if(to_remove.Count > 0)
                //    GameUtils.Log("Removed " + to_remove.Count + " buffers from " + audioSource.GetFriendlyName() + ". Total count: " + audioBuffers.Count.ToString());
            }
        }

        /// <summary>
        /// Processes the given samples and saves them for later inference
        /// </summary>
        /// <param name="samples">The float sample array (values between -1.0 and 1.0)</param>
        /// <param name="len">Number of samples to read from the array</param>
        /// <returns>The number of samples that were actually sved,
        ///     or -1 if the buffer mutex could not be obtained</returns>
        public int EatSamples(float[] samples, int len) {
            EnsureAdditionalBuffers();

            if(ready_for_filling.Count == 0) return 0;

            AudioBuffer buff = ready_for_filling.Peek();

            if(!buff.readWriteMutex.WaitOne(1)) return -1;
            try {
                last_activity = Utils.GetTime();
                if(buff.buffer_head == AudioBuffer.buffer_size) return 0;

                for(int i = 0; i < len; i++) {
                    if((buff.buffer_head + i) >= AudioBuffer.buffer_size) {
                        buff.buffer_head = AudioBuffer.buffer_size;
                        return i;
                    }

#if USE_SHORT
                    buff.buffer[buff.buffer_head + i] = (short)(samples[i] * 32768.0f);
#else
                    // TODO: For some reason, vosk needs the value to be multiplied by a large number like 1024
                    buff.buffer[buff.buffer_head + i] = samples[i] * 1024.0f;
#endif
                }
                buff.buffer_head = buff.buffer_head + len;
                
                return len;
            } finally {
                buff.lastFillTime = last_activity;
                buff.readWriteMutex.ReleaseMutex();
                
                if(buff.ShouldBeQueued()) {
                    ready_for_filling.Dequeue();
                    buff.queued = true;
                    ready_for_processing.Enqueue(buff);
                }
            }
        }

        /// <summary>
        /// Returns the number of buffers that are pending inference.
        /// </summary>
        /// <returns>The number of buffers pending inference</returns>
        public int GetBuffersPending() {
            if(disposed) return -1;

            return ready_for_processing.Count;
        }

        /// <summary>
        /// Maximum time gap in seconds between Sayings before they should be considered separate
        /// </summary>
        public static float sepAge { get; private set; } = 0.666f;

        /// <summary>
        /// The maximum age of a Saying. Sayings older than this shall be destroyed, if they're not
        /// connected to other Sayings that are younger than this value.
        /// </summary>
        public static float maxAge { get; private set; } = 8.0f;

        /// <summary>
        /// Gets the age of a Saying's index, taking into account the fact that
        /// it may be connected to other Sayings with a gap smaller than sepAge.
        /// 
        /// For example, if the 0th Saying's TimeEnd is 10 seconds old, but
        /// the 1st Saying's TimeStart is 9.99 seconds old and TimeEnd is 3 seconds old,
        /// and there exists no other Sayings, then 3 seconds will be returned.
        /// </summary>
        /// <param name="idx_to_start_from">The Saying's index</param>
        /// <returns>The minimum age of the Saying. If +infinity, something has gone wrong</returns>
        private float GetNonsepMaxAge(int idx_to_start_from) {
            if(past_sayings[idx_to_start_from] == null) return float.PositiveInfinity;
            if(idx_to_start_from + 1 >= past_sayings.Count) return past_sayings[idx_to_start_from].timeEnd;

            for(int i=idx_to_start_from+1; i<past_sayings.Count; i++) {
                if(past_sayings[i - 1] == null || past_sayings[i] == null) return float.PositiveInfinity;
                float prevEnd = past_sayings[i - 1].timeEnd;
                float currStart = past_sayings[i].timeStart;

                if(currStart - prevEnd > sepAge) {
                    return prevEnd;
                }
            }

            if(past_sayings[past_sayings.Count - 1] == null) return float.PositiveInfinity;
            return past_sayings[past_sayings.Count - 1].timeEnd;
        }

        /// <summary>
        /// Removes old values from past_sayings that are no longer required.
        /// </summary>
        private void CleanUpOldSayings() {
            float currTime = Utils.GetTime();
            while(past_sayings.Count > 0 && (currTime - GetNonsepMaxAge(0)) > maxAge) {
                past_sayings.RemoveAt(0);
            }
        }

        private TextGenerator textGen = new TextGenerator();

        /// <summary>
        /// Cleans up old sayings and calls for the text to be updated
        /// </summary>
        private void UpdateText() {
            CleanUpOldSayings();
            textGen.UpdateText(past_sayings, active_saying);
        }

        /// <summary>
        /// Gets the text of what has been said in the current session,
        /// in a caption-friendly form (with 2 max lines and auto scrolling)
        /// </summary>
        /// <returns>Caption-friendly filtered string of what is being said</returns>
        public string GetText() {
            return textGen.FullText;
        }



        /// <summary>
        /// Updates everything that requires updating. This should be called
        /// once every frame.
        /// </summary>
        public void Update() {
            if(disposed) return;

            DeleteUnnecesssaryBuffersIfNeeded();

            if(audioSource == null) return;


            UpdateText();

            if(ui == null) ui = new SubtitleUi(this);
            ui.UpdateText();
        }
    }
}
