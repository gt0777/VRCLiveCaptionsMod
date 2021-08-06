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
using System.Collections.Generic;
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
        private SessionPool general_pool = new SessionPool();
        private SessionPool low_priority_pool = new SessionPool();
        private Dictionary<string, SessionPool> high_priority_pools = new Dictionary<string, SessionPool>();
        
        public TranscribeWorker() {
            GameUtils.GetProvider().AllAudioSourcesRemoved += CleanAndRestart;
            GameUtils.GetProvider().AudioEmitted += rawAudio;

            Settings.DisableChanging += (bool to) => {
                if(to) CleanAndRestart();
            };

            general_pool.EnsureTaskIsStopped();
        }

        private bool halted = false;

        private void CleanAndRestart() {
            halted = true;

            low_priority_pool.EnsureTaskIsStopped();
            low_priority_pool.DeleteAllSessions();

            foreach(SessionPool pool in high_priority_pools.Values) {
                pool.EnsureTaskIsStopped();
                pool.DeleteAllSessions(false);
            }

            general_pool.DeleteAllSessions(true);

            high_priority_pools.Clear();

            low_priority_pool.AllowTaskToRunAgain();

            halted = false;
        }

        /// <summary>
        /// Position of local player's head
        /// </summary>
        private Vector3 localPosition = Vector3.zero;

        private float lastEatLog = 0.0f;

        private void rawAudio(IAudioSource src, float[] samples, int len, int samplerate) {
            if(Settings.Disabled || halted) return;
            
            float maxDistMultiplier = 1.0f;
            if(!AudioSourceOverrides.IsWhitelisted(src.GetUID())) {
                if(!Settings.AutoTranscribeWhenInRange) return;
            } else {
                maxDistMultiplier = 2.0f;
            }
            
            Vector3 remotePlayerPos = src.GetPosition();
            if((remotePlayerPos - localPosition).sqrMagnitude > (Settings.transcribe_range * Settings.transcribe_range * maxDistMultiplier * maxDistMultiplier)) return;


            try {
                TranscriptSession session = general_pool.GetOrCreateSession(src);

                SessionPool pool;
                if(session.whitelisted) {
                    if(!high_priority_pools.ContainsKey(src.GetUID())) {
                        high_priority_pools[src.GetUID()] = new SessionPool();
                        high_priority_pools[src.GetUID()].InsertSession(session);
                        GameUtils.LogDebug("Create new high-priority session for " + src.GetFriendlyName());
                    }
                    if(low_priority_pool.ContainsSession(session)) {
                        low_priority_pool.EnsureTaskIsStopped();
                        low_priority_pool.DeleteSession(session, false);
                        GameUtils.LogDebug("Remove from low-priority pool " + src.GetFriendlyName());

                        low_priority_pool.AllowTaskToRunAgain();
                    }

                    pool = high_priority_pools[src.GetUID()];
                } else {
                    if(!low_priority_pool.ContainsSession(session)) {
                        low_priority_pool.InsertSession(session);
                        GameUtils.LogDebug("Insert to low-priority pool " + src.GetFriendlyName());
                    }
                    if(high_priority_pools.ContainsKey(src.GetUID())) {
                        high_priority_pools[src.GetUID()].EnsureTaskIsStopped();
                        high_priority_pools[src.GetUID()].DeleteSession(session, false);
                        high_priority_pools.Remove(src.GetUID());
                        GameUtils.LogDebug("Delete high-priority pool " + src.GetFriendlyName());
                    }

                    pool = low_priority_pool;
                }
                
                int eaten = session.EatSamples(samples, len);
                if(eaten < samples.Length) {
                    if((Utils.GetTime() - lastEatLog) > 0.5f) {
                        // TODO: UI Indicator
                        GameUtils.LogWarn(src.GetFriendlyName() + ": Buffer full! Ate only " + eaten.ToString());
                        lastEatLog = Utils.GetTime();
                    }
                }
            } catch(Exception e) {
                GameUtils.LogError(e.ToString());
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

            low_priority_pool.Tick();

            foreach(string key in high_priority_pools.Keys) {
                SessionPool pool = high_priority_pools[key];

                pool.Tick();

                if(pool.GetSessions().Count == 0) {
                    GameUtils.LogDebug("No count. Delete sessionPool " + key);
                    high_priority_pools.Remove(key);
                    break; // can't continue loop because the collection was modified
                }
            }
        }
    }
}
