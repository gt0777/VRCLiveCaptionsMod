﻿// VRCLiveCaptionsMod - a mod for providing voice chat live captions
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
using System.Threading.Tasks;
using VRCLiveCaptionsMod.LiveCaptions.Abstract;
using VRCLiveCaptionsMod.LiveCaptions.GameSpecific;

namespace VRCLiveCaptionsMod.LiveCaptions {
    class SessionPool {
        private Dictionary<string, TranscriptSession> sessions = new Dictionary<string, TranscriptSession>();

        public int samplerate { get; private set; }

        public SessionPool(int samplerate = 48000) {
            this.samplerate = samplerate;

            GameUtils.GetProvider().AudioSourceRemoved += OnPlayerLeft;
        }

        private void OnPlayerLeft(IAudioSource ply) {
            DeleteSession(ply);
        }

        public void InitializeSession(IAudioSource src) {
            TranscriptSession rec = new TranscriptSession(src, this.samplerate);

            sessions[src.GetUID()] = rec;
        }

        public void DeleteAllSessions(bool dispose = true) {
            if(dispose) {
                foreach(TranscriptSession session in sessions.Values) {
                    try {
                        session.FullDispose();
                    } catch(Exception e) {
                        GameUtils.LogError("DELETESSSIONS: " + e.ToString());
                    }
                }
            }

            sessions.Clear();
        }

        private void DeleteByValue(TranscriptSession session, bool dispose) {
            foreach(string key in sessions.Keys) {
                if(sessions[key] == session) {
                    if(dispose) {
                        session.FullDispose();
                    }
                    sessions.Remove(key);
                    return;
                }
            }
        }

        public void DeleteSession(TranscriptSession session, bool dispose = true) {
            if((session.audioSource == null) || (!DeleteSession(session.audioSource.GetUID(), dispose))) {
                DeleteByValue(session, dispose);
                return;
            }
        }

        public void DeleteSession(IAudioSource src, bool dispose = true) {
            DeleteSession(src.GetUID(), dispose);
        }

        public bool DeleteSession(string uid, bool dispose = true) {
            if(!sessions.ContainsKey(uid)) return false;

            TranscriptSession rec = sessions[uid];

            if(dispose) {
                rec.FullDispose();
            }

            sessions.Remove(uid);

            return true;
        }

        public bool ContainsSession(TranscriptSession session) {
            if(session.audioSource == null) {
                GameUtils.LogError("Cannot check a session with null audiosource!");
                return false;
            }
            return sessions.ContainsKey(session.audioSource.GetUID());
        }

        public TranscriptSession GetOrCreateSession(IAudioSource src) {
            if(!sessions.ContainsKey(src.GetUID())) {
#if DEBUG
                var watch = System.Diagnostics.Stopwatch.StartNew();
#endif
                InitializeSession(src);
#if DEBUG
                GameUtils.Log("Time taken to init session: " + (watch.ElapsedMilliseconds).ToString());
#endif
            }
            TranscriptSession rec = sessions[src.GetUID()];

            if(rec.disposed) {
                DeleteSession(src, false);
                return GetOrCreateSession(src);
            }

            return rec;
        }

        public void InsertSession(TranscriptSession session) {
            if(session.audioSource == null) {
                GameUtils.LogError("Cannot insert a session with null audiosource!");
                return;
            }
            if(!sessions.ContainsKey(session.audioSource.GetUID())) {
                sessions[session.audioSource.GetUID()] = session;
            }
        }

        public Dictionary<string, TranscriptSession>.ValueCollection GetSessions() {
            return sessions.Values;
        }

        private bool LockedFromStarting = false;
        private Mutex RunBusyMutex = new Mutex();
        public bool Running = false;

        /// <summary>
        /// The background task that calls for inference.
        /// </summary>
        private void Run() {
            if(LockedFromStarting) return;

            RunBusyMutex.WaitOne();
            try {
                Running = true;

                float time_now = Utils.GetTime();

                try {
                    foreach(TranscriptSession session in GetSessions()) {
                        if(session == null) continue;
                        if(LockedFromStarting) return;

                        if(session.disposed) {
                            GameUtils.LogDebug("Disposed session " + session.audioSource.GetFriendlyName() + ", removing...");
                            DeleteByValue(session, false);
                            break;
                        } else if((time_now - session.last_activity) > 96.0) {
                            // The player is no longer speaking, remove their session if they're not
                            // whitelisted
                            if(!session.whitelisted || ((time_now - session.last_activity) > 600.0)) {
                                GameUtils.LogDebug(session.audioSource.GetFriendlyName() + " Player is no longer speaking, remove");
                                DeleteSession(session);
                                break; // collection was modified, enumeration may not resume
                            }
                        } else if((time_now - session.last_activity) > 0.3f) {
                            session.FlushCurrentAudio();
                            session.RunInference();
                            session.CommitSayingIfTooOld();
                        } else {
                            session.RunInference();
                        }
                    }
                } catch(Exception e) {
                    // we don't really care about this error
                    if(!e.Message.Contains("Collection was modified; enumeration"))
                        GameUtils.LogError("In Run(): " + e.ToString());
                }
            } finally {
                RunBusyMutex.ReleaseMutex();
                Running = false;
            }
        }

        /// <summary>
        /// The foreground tick method that should be called once every frame.
        /// This updates all of the subtitle UIs.
        /// </summary>
        public void Tick() {
            if(!Running && !LockedFromStarting) {
                Task.Run(() => Run());
            }

            try {
                foreach(TranscriptSession session in GetSessions()) {
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

        /// <summary>
        /// Ensures the background task is stopped
        /// </summary>
        public void EnsureTaskIsStopped() {
            LockedFromStarting = true;
            while(Running) {
                RunBusyMutex.WaitOne();
                RunBusyMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Allows the background task to run again,
        /// following a call to EnsureTaskIsStopped
        /// </summary>
        public void AllowTaskToRunAgain() {
            LockedFromStarting = false;
        }
    }
}
