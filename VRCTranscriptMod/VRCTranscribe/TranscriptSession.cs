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
    class AudioBuffer {
        public const int buffer_size = 64000;
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

        private List<string> past_sayings = new List<string>();
        private string active_saying = "";

        private SubtitleUi ui;

        private AudioBuffer[] audioBuffers = new AudioBuffer[2];

        private Mutex inferrenceMutex = new Mutex();

        public TranscriptSession(Model model, float sample_rate) {
            this.model = model;
            this.sample_rate = sample_rate;

            for(int i=0; i<2; i++) {
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
            buff.beingTranscribed = true;
            if(!inferrenceMutex.WaitOne()) return;

            bool released_mutex = false;
            try {

                if(rec == null) rec = new VoskRecognizer(model, sample_rate);

                if(disposalInProgress) {
                    MelonLogger.Warning("Reached mid-disposal!!");
                    return;
                }
                if(rec.AcceptWaveform(buff.buffer, buff.buffer_head)) {
                    inferrenceMutex.ReleaseMutex();
                    released_mutex = true;
                    string txt = VoskUtil.extractTextFromResult(rec.Result());
                    active_saying = txt;

                    // Need to delete it, otherwise memory use grows like crazy.
                    Dispose();
                } else {
                    inferrenceMutex.ReleaseMutex();
                    released_mutex = true;
                    string txt = VoskUtil.extractTextFromResult(rec.PartialResult());
                    active_saying = txt;
                }

                buff.buffer_head = 0;
            } finally {
                FullText = MakeText();
                if(!released_mutex) inferrenceMutex.ReleaseMutex();
                buff.readWriteMutex.ReleaseMutex();
                buff.beingTranscribed = false;
            }
        }



        public void Dispose() {
            if(rec != null) {
                disposalInProgress = true;
                while(!inferrenceMutex.WaitOne()) MelonLogger.Warning("Can't lock inferrenceMutex!!!");
                rec.Dispose();
                inferrenceMutex.ReleaseMutex();
                disposalInProgress = false;
            }
            rec = null;

            past_sayings.Add(active_saying);
            active_saying = "";
        }

        bool disposed = false;
        public void FullDispose() {
            Dispose();
            if(ui != null) ui.Dispose();
            ui = null;
            disposed = true;
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

                    // TODO: For some reason, vosk needs the value to be multiplied by a large number like 1024
#if USE_SHORT
                    buff.buffer[buff.buffer_head + i] = (short)(samples[i] * 32768.0f);
#else
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

        private string MakeText() {
            // TODO
            //string full = String.Join(" ", past_sayings.ToArray()) + " " + active_saying;
            //return full.Substring(Math.Max(0, full.Length - 48));

            string full = "";
            foreach(string v in past_sayings) {
                full = full + v + " ";
            }
            full = full + active_saying;

            if(full.Length > 2) {
                /*
                full = full.Substring(Math.Max(0, full.Length - 128));

                if(full.Split(' ').Length > 1) {
                    string cutOffWord = full.Split(' ')[0];
                    if(cutOffWord.Length > 0)
                        full = full.Substring(cutOffWord.Length).TrimStart();
                }*/

                // Insert line breaks
                string tmp = "";
                int chars_in_current_line = 0;
                foreach(char c in full) {
                    tmp = tmp + c;
                    chars_in_current_line++;

                    if(chars_in_current_line >= 48 && c == ' ') {
                        tmp = tmp + '\n';
                        chars_in_current_line = 0;
                    }
                }


                string[] lines = tmp.Split('\n');

                full = "";
                for(int i=0; i<lines.Length; i++) {
                    if(i >= (lines.Length - 3)) {
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

            if(ui == null) ui = new SubtitleUi(this);

            ui.UpdateText();
        }

        public bool IsActive() {
            return rec != null;
        }
    }
}
