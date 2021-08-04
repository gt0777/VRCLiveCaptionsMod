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
        private SessionPool general_pool = new SessionPool();
        private SessionPool[] pools = new SessionPool[2];
        
        public TranscribeWorker() {
            GameUtils.GetProvider().AllAudioSourcesRemoved += () => CleanAndRestart();
            GameUtils.GetProvider().AudioEmitted += rawAudio;

            Settings.DisableChanging += (bool to) => {
                if(to) CleanAndRestart();
            };

            pools[0] = new SessionPool();
            pools[1] = new SessionPool();

            general_pool.LockedFromStarting = true;
        }
        
        private void CleanAndRestart() {
            foreach(SessionPool pool in pools) {
                pool.LockedFromStarting = true;
                pool.EnsureThreadIsStopped();
                pool.DeleteAllSessions(false);
            }

            general_pool.DeleteAllSessions(true);

            foreach(SessionPool pool in pools) {
                pool.LockedFromStarting = false;
            }
        }

        /// <summary>
        /// Position of local player's head
        /// </summary>
        private Vector3 localPosition = Vector3.zero;

        private float lastEatLog = 0.0f;

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
            

            try {
                TranscriptSession session = general_pool.GetOrCreateSession(src);

                if(session.whitelisted && !pools[1].ContainsSession(session)) {
                    if(pools[0].ContainsSession(session)) pools[0].DeleteSession(session, false);
                    pools[1].InsertSession(session);
                } else if(!session.whitelisted && !pools[0].ContainsSession(session)) {
                    if(pools[1].ContainsSession(session)) pools[1].DeleteSession(session, false);
                    pools[0].InsertSession(session);
                }

                pools[session.whitelisted ? 1 : 0].EnsureThreadIsRunning();
                
                int eaten = session.EatSamples(samples, len);
                if(eaten < samples.Length) {
                    if((Utils.GetTime() - lastEatLog) > 0.5f) {
                        GameUtils.LogWarn("Buffer full! Ate only " + eaten.ToString());
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
            
            foreach(SessionPool pool in pools) {
                pool.Tick();
            }
        }
    }
}
