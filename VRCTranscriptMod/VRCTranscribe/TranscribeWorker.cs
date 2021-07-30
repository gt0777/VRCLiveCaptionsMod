using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Vosk;

namespace VRCTranscriptMod.VRCTranscribe {
    class TranscribeWorker {
        private Thread bg_thread;
        private SessionPool pool;
        private Model model;

        const int blank_samples_len = 48000 / 16;
        private float[] blank_samples = new float[blank_samples_len];
        

        public TranscribeWorker(Model model) {
            this.model = model;
            bg_thread = new Thread(run);

            USpeakHooker.OnRawAudio += rawAudio;

            this.pool = null;


            bg_thread.Start();



            // TODO: this sucks, any other way to avoid crash?
            /*
            VRChatUtilityKit.Ui.UiManager.OnQuickMenuOpened += () => {
                transcriptionPaused = true;
                bg_thread.Abort();
            };

            VRChatUtilityKit.Ui.UiManager.OnQuickMenuClosed += () => {
                transcriptionPaused = false;
            };

            VRChatUtilityKit.Ui.UiManager.OnBigMenuOpened += () => {
                transcriptionPaused = true;
                bg_thread.Abort();
            };

            VRChatUtilityKit.Ui.UiManager.OnBigMenuClosed += () => {
                transcriptionPaused = false;
            };
            */

            for(int i=0; i<blank_samples_len; i++) {
                blank_samples[i] = 0.0f;
            }

            VRChatUtilityKit.Utilities.NetworkEvents.OnRoomLeft += () => {
                MelonLogger.Msg("ROOM LEFT!! ROOM LEFT!!");
                CleanAndRestart();
            };
        }

        private Mutex inferenceBusyMutex = new Mutex();
        private bool transcriptionPaused = false;
        public void CleanAndRestart() {
            try {
                while(!inferenceBusyMutex.WaitOne()) MelonLogger.Error("FAIL TO GRAB MUTEX!!");

                if(bg_thread.IsAlive) bg_thread.Abort();
                if(pool != null) this.pool.DeleteAllSessions();
            } finally {
                inferenceBusyMutex.ReleaseMutex();
            }
        }

        private void rawAudio(VRCPlayer ply, float[] samples, int samplerate) {
            if(ply == null) return;
            if(samplerate < 1000) return;
            if(samples.Length < 4) return;
            if(!TranscriptPlayerOverrides.IsWhitelisted(Utils.GetUID(ply))) return;


            if(this.pool == null) {
                MelonLogger.Msg("Samplerate: " + samplerate.ToString());
                this.pool = new SessionPool(samplerate, model);
            }

            if(!bg_thread.IsAlive && !transcriptionPaused) {
                MelonLogger.Msg("Dead thread, restart");
                bg_thread = new Thread(run);
                bg_thread.Start();
            }

            try {
                TranscriptSession session = pool.GetOrCreateSession(Utils.GetUID(ply));
                session.associated_player = ply;
                int eaten = session.EatSamples(samples);
                if(eaten < samples.Length) {
                    MelonLogger.Warning("Buffer full! Ate only " + eaten.ToString());
                }
            }catch(Exception e) {
                MelonLogger.Error(e.ToString());
            }
        }

        float last_session_log = 0.0f;
        private void run() {
            while(true) {
                Thread.Sleep(10);
                try {
                    while(!inferenceBusyMutex.WaitOne()) MelonLogger.Warning("Unable to grab mutex??");

                    if(transcriptionPaused) continue;
                    if(pool == null) continue;

                    float time_now = Utils.GetTime();

                    try {
                        if(time_now - last_session_log > 8.0f) {
                            MelonLogger.Msg(pool.GetSessions().Count.ToString() + " sessions");
                            foreach(TranscriptSession session in pool.GetSessions()) {
                                MelonLogger.Msg("Session - Last active " + (time_now - session.last_activity).ToString());
                            }
                            last_session_log = time_now;
                        }
                        foreach(TranscriptSession session in pool.GetSessions()) {
                            if(transcriptionPaused) continue;

                            if(session.associated_player == null) {
                                // This shouldn't happen and it will break things
                                // Delete the session so it doesn't break.
                                pool.DeleteByValue(session);
                            }

                            if(session == null) continue;
                            
                            int bytes_pending = session.getBytesPending();
                            if(session.getBytesPending() > 4000 || (time_now - session.last_activity > 0.1f && session.getBytesPending() > 0)) {
                                // Time to empty the buffer by running inferrence
                                session.RunInferrence();
                            }


                            if((time_now - session.last_activity) > 48.0) {
                                // The player is no longer speaking, remove their session
                                MelonLogger.Msg(Utils.GetUID(session.associated_player) + " Player is no longer speaking, remove");
                                pool.DeleteSession(Utils.GetUID(session.associated_player));
                            } else if((time_now - session.last_activity) > 0.5f) {
                                // Dispose of the vosk recognizer to free memory.
                                //MelonLogger.Msg(Utils.GetUID(session.associated_player) + " Vosk dispose for memory saving");

                                if(session.IsActive()) {
                                    MelonLogger.Msg("Blank sample submit and dispose");
                                    session.EatSamples(blank_samples);
                                    session.RunInferrence();
                                    session.Dispose();
                                }
                            }
                        }
                    } catch(System.InvalidOperationException e) {
                        MelonLogger.Msg("InvalidOperationException, moving on " + e.ToString());
                    } catch(Exception e) {
                        MelonLogger.Error(e.ToString());
                    }
                } finally {
                    inferenceBusyMutex.ReleaseMutex();
                }
            }
        }

        public void Tick() {
            if(pool == null) return;

            foreach(TranscriptSession session in pool.GetSessions()) {
                if(session.associated_player == null) continue;

                try {
                    session.Update();
                }catch(Exception e) {
                    MelonLogger.Error(e.ToString());
                }
            }
        }
    }
}
