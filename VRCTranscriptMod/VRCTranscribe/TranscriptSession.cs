#define USE_SHORT

using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Vosk;
using VRC.SDKBase;

#if USE_SHORT
using BUFFER_TYPE = System.Int16;
#else
using BUFFER_TYPE = System.Single;
#endif


namespace VRCTranscriptMod.VRCTranscribe {
    class Saying {
        public string fullTxt { get; private set; }

        public float timeStart { get; private set; }
        public float timeEnd { get; private set; }

        public Saying() {
            timeStart = Utils.GetTime();
            timeEnd = timeStart;
            fullTxt = "";
        }

        public void Update(string to) {
            fullTxt = to;
            timeEnd = Utils.GetTime();
        }

        // TODO: per-word time?
    }

    class AudioBuffer {
        public const int buffer_size = 32000;
        public BUFFER_TYPE[] buffer = new BUFFER_TYPE[buffer_size];
        public int buffer_head = 0;

        public Mutex readWriteMutex = new Mutex();

        public bool beingTranscribed = false;
        public float lastFillTime = 0.0f;
    }

    class TranscriptSession {

        public VRCPlayer associated_player;
        
        public float last_activity { get; private set; }

        private VoskRecognizer rec;

        private Model model;
        private float sample_rate;

        private List<Saying> past_sayings = new List<Saying>();
        private Saying active_saying;

        private SubtitleUi ui;

        private AudioBuffer[] audioBuffers = new AudioBuffer[4];

        private Mutex inferrenceMutex = new Mutex();

        private TranscriptSessionDebugger debugger;

        public TranscriptSession(Model model, float sample_rate) {
            this.model = model;
            this.sample_rate = sample_rate;

            for(int i=0; i<audioBuffers.Length; i++) {
                audioBuffers[i] = new AudioBuffer();
            }
        }

        private bool disposalInProgress = false;

        private string FullText;
        public void RunInferrence() {
            if(disposalInProgress) {
                MelonLogger.Warning("RunInferrence called mid-disposal!!");
                return;
            }
            AudioBuffer buff = GetFreeBufferForEating();
            if(buff == null) {
                MelonLogger.Warning("RunInferrence received null buff!");
                return;
            }
            if(buff.buffer_head < 100) return;
            if(!buff.readWriteMutex.WaitOne()) return;
            if(!inferrenceMutex.WaitOne()) return;
            buff.beingTranscribed = true;

            bool released_mutex = false;
            try {

                Vector3 remotePos = associated_player.gameObject.transform.Find("AnimationController/HeadAndHandIK/HeadEffector").position;
                Vector3 localPos = Networking.LocalPlayer.GetPosition();

                if((remotePos - localPos).magnitude > 20.0f) {
                    buff.buffer_head = 0;
                    return;
                }

                if(rec == null) rec = new VoskRecognizer(model, sample_rate);

                if(disposalInProgress) {
                    MelonLogger.Warning("Reached mid-disposal!!");
                    return;
                }

                if(active_saying == null) active_saying = new Saying();

                if(debugger == null) {
                    debugger = new TranscriptSessionDebugger(associated_player.prop_String_0);
                }

                debugger.onSubmitSamples(buff.buffer, buff.buffer_head);

                if(rec.AcceptWaveform(buff.buffer, buff.buffer_head)) {
                    inferrenceMutex.ReleaseMutex();
                    released_mutex = true;
                    MelonLogger.Msg(rec.Result());
                    string txt = VoskUtil.extractTextFromResult(rec.Result());
                    active_saying.Update(txt);

                    // Need to delete it, otherwise memory use grows like crazy.
                    Dispose();
                } else {
                    inferrenceMutex.ReleaseMutex();
                    released_mutex = true;
                    string txt = VoskUtil.extractTextFromResult(rec.PartialResult());
                    active_saying.Update(txt);
                }

                buff.buffer_head = 0;
            } finally {
                if(!released_mutex) inferrenceMutex.ReleaseMutex();
                buff.readWriteMutex.ReleaseMutex();
                buff.beingTranscribed = false;


                //FullText = MakeText();
            }
        }



        public void Dispose() {
            if(rec != null) {
                disposalInProgress = true;
                try {
                    while(!inferrenceMutex.WaitOne()) MelonLogger.Warning("Can't lock inferrenceMutex!!!");
                    rec.Dispose();
                } finally {
                    inferrenceMutex.ReleaseMutex();
                }

                debugger.onDispose();

                disposalInProgress = false;
            }
            rec = null;

            if((active_saying != null) && active_saying.fullTxt.Length > 0) {
                MelonLogger.Msg("Commit : " + active_saying.fullTxt);
                past_sayings.Add(active_saying);
            }
            
            active_saying = null;
        }

        bool disposed = false;
        public void FullDispose() {
            Dispose();
            if(ui != null) ui.Dispose();
            ui = null;
            disposed = true;

            if(debugger != null) {
                debugger.cleanup();
            }
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

        public int EatSamples(float[] samples) {
            AudioBuffer buff = GetFreeBufferForEating();
            if(buff == null) return -2;

            if(!buff.readWriteMutex.WaitOne(8)) return -1;
            try {
                last_activity = Utils.GetTime();
                if(buff.buffer_head == AudioBuffer.buffer_size) return 0;

                for(int i = 0; i < samples.Length; i++) {
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
                buff.buffer_head = buff.buffer_head + samples.Length;
                
                return samples.Length;
            } finally {
                buff.lastFillTime = last_activity;
                buff.readWriteMutex.ReleaseMutex();
            }
        }

        public int getBytesPending() {
            AudioBuffer buff = GetFreeBufferForEating();
            if(buff == null) return -2;

            return buff.buffer_head;
        }

        const float sepAge = 0.666f;
        const float maxAge = 8.0f; //seconds
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

        const int maxLines = 2;
        float lastTextUpdate = 0.0f;
        private string MakeText() {
            lastTextUpdate = Utils.GetTime();

            CleanUpOldSayings();


            // Merge past and active to make codepath simpler
            Saying[] allSayings;

            allSayings = new Saying[past_sayings.Count + 
                ((active_saying != null) ? 1 : 0)];
            for(int i=0; i<past_sayings.Count; i++) {
                allSayings[i] = past_sayings[i];
            }

            if(active_saying != null)
                allSayings[past_sayings.Count] = active_saying;


            // Concat allSayings
            string full = "";
            for(int i=0; i<allSayings.Length; i++) {
                float prevAge = 0.0f;
                if(i > 0) {
                    prevAge = allSayings[i - 1].timeEnd;
                }

                float diff = Math.Abs(allSayings[i].timeStart - prevAge);
                if(diff > sepAge) {
                    full = full + '\n';
                } else {
                    full = full + ' ';
                }
                full = full + allSayings[i].fullTxt;
            }

            if(full.Length > 2) {
                // Insert line breaks when a line is too long
                string tmp = "";
                int chars_in_current_line = 0;
                
                for(int i=0; i<full.Length; i++) {
                    char c = full[i];

                    tmp = tmp + c;
                    if(c == '\n') {
                        chars_in_current_line = 0;
                        continue;
                    }

                    chars_in_current_line++;

                    if(chars_in_current_line >= 48 && c == ' ') {
                        tmp = tmp + '\n';
                        chars_in_current_line = 0;
                    }
                }


                // Grab only the last ${maxLines} lines
                string[] lines = tmp.Split('\n');

                full = "";
                for(int i=0; i<lines.Length; i++) {
                    if(i >= (lines.Length - maxLines)) {
                        full = full + lines[i] + '\n';
                    }
                }
            }

            

            return full;
        }

        public string GetText() {
            return FullText;
        }

        public void Update() {
            if(associated_player == null) return;
            if(disposed) return;

            if(Utils.GetTime() - lastTextUpdate > 0.1f) {
                FullText = MakeText();
            }

            if(ui == null) ui = new SubtitleUi(this);

            ui.UpdateText();
        }

        public bool IsActive() {
            return rec != null;
        }
    }
}
