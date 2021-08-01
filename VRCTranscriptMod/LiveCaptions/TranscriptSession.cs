
using System;
using System.Collections.Generic;
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
    /// A transcript session
    /// </summary>
    class TranscriptSession {
        public IAudioSource audioSource;
        private IVoiceRecognizer recognizer = null;
        private SubtitleUi ui;
        
        private List<Saying> past_sayings = new List<Saying>();
        private Saying active_saying;

        private AudioBuffer[] audioBuffers = new AudioBuffer[4];

        private Mutex inferrenceMutex = new Mutex();

        public float last_activity { get; private set; }
        private int sample_rate;



        public TranscriptSession(IAudioSource src, int sample_rate) {
            audioSource = src;
            this.sample_rate = sample_rate;

            for(int i=0; i<audioBuffers.Length; i++) {
                audioBuffers[i] = new AudioBuffer();
            }

            recognizer = GameUtils.GetVoiceRecognizer();
            if(recognizer != null) recognizer.Init(this.sample_rate);

            VoiceRecognizerEvents.VoiceRecognizerChanged += () => {
                if(!inferrenceMutex.WaitOne()) return;
                try {
                    CommitSaying();
                    recognizer = GameUtils.GetVoiceRecognizer();
                    if(recognizer != null) recognizer.Init(this.sample_rate);
                } finally {
                    inferrenceMutex.ReleaseMutex();
                }
            };
        }

        public void RunInferrence() {
            if(recognizer == null) return;

            CommitSayingIfTooOld();

            AudioBuffer buff = GetFreeBufferForDigestion();
            if(buff == null) return;
            if(!inferrenceMutex.WaitOne()) return;
            buff.StartTranscribing();
            try {
                if(recognizer == null) return;

                if(active_saying == null) active_saying = new Saying();

                bool final = recognizer.Recognize(buff.buffer, buff.buffer_head);
                active_saying.Update(recognizer.GetText(), final);

                if(final) CommitSaying();
            } finally {
                inferrenceMutex.ReleaseMutex();
                buff.StopTranscribing();
            }
        }

        private void CommitSaying() {
            if(active_saying != null) {
                if(!active_saying.final && recognizer != null) {
                    // finalize it
                    recognizer.Flush();
                    active_saying.Update(recognizer.GetText(), true);
                }

                if(active_saying.fullTxt.Length > 0) {
                    past_sayings.Add(active_saying);
                }
            }
            active_saying = null;
        }

        public void CommitSayingIfTooOld() {
            if(active_saying != null)
                if((Utils.GetTime() - active_saying.timeEnd) > sepAge) CommitSaying();
        }

        public bool HasWords(){
            return active_saying != null && active_saying.fullTxt.Length > 0;
        }

        
        public void FullDispose() {
            GameUtils.Log("Fulldispose enter");
            recognizer = null;
            if(ui != null) ui.Dispose();
            ui = null;
            GameUtils.Log("Fulldispose exit");
        }

        private AudioBuffer GetFreeBufferForEating() {
            float max_time = -500.0f;
            AudioBuffer result = null;
            foreach(AudioBuffer buff in audioBuffers) {
                if(!buff.beingTranscribed) {
                    if(buff.lastFillTime > max_time) {
                        max_time = buff.lastFillTime;
                        result = buff;
                    }
                }
            }
            return result;
        }

        private AudioBuffer GetFreeBufferForDigestion() {
            return GetFreeBufferForEating(); //TODO: separate the logic
        }

        public int EatSamples(float[] samples, int len) {
            AudioBuffer buff = GetFreeBufferForEating();
            if(buff == null) return -2;

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
            }
        }

        public int getBytesPending() {
            AudioBuffer buff = GetFreeBufferForDigestion();
            if(buff == null) return -2;

            return buff.buffer_head;
        }

        public static float sepAge { get; private set; } = 0.666f;
        public static float maxAge { get; private set; } = 8.0f;
        private float GetNonsepMaxAge(int idx_to_start_from) {
            if(idx_to_start_from + 1 >= past_sayings.Count) return past_sayings[idx_to_start_from].timeEnd;

            for(int i=idx_to_start_from+1; i<past_sayings.Count; i++) {
                float prevEnd = past_sayings[i - 1].timeEnd;
                float currStart = past_sayings[i].timeStart;

                if(currStart - prevEnd > sepAge) {
                    return prevEnd;
                }
            }

            return past_sayings[past_sayings.Count - 1].timeEnd;
        }

        private void CleanUpOldSayings() {
            float currTime = Utils.GetTime();
            while(past_sayings.Count > 0 && (currTime - GetNonsepMaxAge(0)) > maxAge) {
                past_sayings.RemoveAt(0);
            }
        }

        TextGenerator textGen = new TextGenerator();
        private void UpdateText() {
            CleanUpOldSayings();
            textGen.UpdateText(past_sayings, active_saying);
        }

        public string GetText() {
            return textGen.FullText;
        }

        public void Update() {
            if(audioSource == null) return;

            UpdateText();

            if(ui == null) ui = new SubtitleUi(this);
            ui.UpdateText();
        }
    }
}
