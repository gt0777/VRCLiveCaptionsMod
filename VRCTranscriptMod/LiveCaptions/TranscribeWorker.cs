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

using System;
using System.Threading;
using UnityEngine;
using VRCLiveCaptionsMod.LiveCaptions.Abstract;
using VRCLiveCaptionsMod.LiveCaptions.GameSpecific;

namespace VRCLiveCaptionsMod.LiveCaptions {
    /// <summary>
    /// The worker that manages the session pool and sessions.
    /// This class runs a background thread that loops through each session
    /// and acts on them as needed.
    /// It also hooks into IGameProvider's events
    /// </summary>
    public class TranscribeWorker {
        private Thread bg_thread = null;
        private SessionPool pool = null;
        
        public TranscribeWorker() {
            GameUtils.GetProvider().AllAudioSourcesRemoved += () => CleanAndRestart();
            GameUtils.GetProvider().AudioEmitted += rawAudio;

            Settings.DisableChanging += (bool to) => {
                if(to) CleanAndRestart();
            };
        }

        private Mutex inferenceBusyMutex = new Mutex();
        private void CleanAndRestart() {
            try {
                while(!inferenceBusyMutex.WaitOne()) GameUtils.LogError("FAIL TO GRAB MUTEX!!");

                if(bg_thread != null && bg_thread.IsAlive) bg_thread.Abort();
                if(pool != null) pool.DeleteAllSessions();
            } finally {
                inferenceBusyMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Position of local player's head
        /// </summary>
        private Vector3 localPosition = Vector3.zero;

        private void rawAudio(IAudioSource src, float[] samples, int len, int samplerate) {
            if(Settings.Disabled) return;

            
            float maxDistMultiplier = 1.0f;
            if(!AudioSourceOverrides.IsWhitelisted(src.GetUID())) {
                if(!Settings.AutoTranscribeWhenInRange) return;
            } else {
                maxDistMultiplier = 2.0f;
            }



            Vector3 remotePlayerPos = src.GetPosition();
            if((remotePlayerPos - localPosition).sqrMagnitude > (Settings.transcribe_range * Settings.transcribe_range * maxDistMultiplier * maxDistMultiplier)) return;

            
            if(pool == null) {
                pool = new SessionPool(samplerate);
            }

            if(bg_thread == null || (!bg_thread.IsAlive)) {
                bg_thread = new Thread(run);
                bg_thread.Start();
            }

            try {
                TranscriptSession session = pool.GetOrCreateSession(src);
                int eaten = session.EatSamples(samples, len);
                if(eaten < samples.Length) {
                    GameUtils.LogWarn("Buffer full! Ate only " + eaten.ToString());
                }
            }catch(Exception e) {
                GameUtils.LogError(e.ToString());
            }
        }
        
        /// <summary>
        /// The background thread loop that calls for inference.
        /// </summary>
        private void run() {
            while(true) {
                Thread.Sleep(10);
                try {
                    while(!inferenceBusyMutex.WaitOne()) GameUtils.LogWarn("Unable to grab mutex??");

                    if(Settings.Disabled) continue;
                    if(pool == null) continue;

                    float time_now = Utils.GetTime();

                    try {
                        foreach(TranscriptSession session in pool.GetSessions()) {
                            if(session == null) continue;
                            
                            if(session.GetSamplesPending() > 4000 || (time_now - session.last_activity > 0.1f && session.GetSamplesPending() > 0)) {
                                // Time to empty the buffer by running inferrence
                                session.RunInference();
                            }


                            if((time_now - session.last_activity) > 96.0) {
                                // The player is no longer speaking, remove their session if they're not
                                // whitelisted
                                if(!session.whitelisted || ((time_now - session.last_activity) > 600.0)) {
                                    GameUtils.Log(session.audioSource.GetFriendlyName() + " Player is no longer speaking, remove");
                                    pool.DeleteSession(session);
                                    break; // collection was modified, enumeration may not resume
                                }
                            } else if((time_now - session.last_activity) > 0.5f) {
                                if(session.GetSamplesPending() > 0) session.RunInference();

                                session.CommitSayingIfTooOld();
                            }
                        }
                    } catch(Exception e) {
                        GameUtils.LogError("In run(): " + e.ToString());
                    }
                } finally {
                    inferenceBusyMutex.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// The foreground tick method that should be called once every frame.
        /// This updates all of the subtitle UIs.
        /// </summary>
        public void Tick() {
            if(VRC.SDKBase.Networking.LocalPlayer != null) {
                localPosition = VRC.SDKBase.Networking.LocalPlayer.GetBonePosition(HumanBodyBones.Head);
            }
            if(pool == null) return;

            try {
                foreach(TranscriptSession session in pool.GetSessions()) {
                    try {
                        session.Update();
                    } catch(Exception e) {
                        GameUtils.LogError(e.ToString());
                    }
                }
            } catch(System.InvalidOperationException) {
                // probably session has been added or removed, so ignore this

            } catch(Exception e) {
                GameUtils.Log("An exception has occurred in tick: " + e.ToString());
            }
        }
    }
}
