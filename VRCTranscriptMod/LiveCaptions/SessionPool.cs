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
using VRCLiveCaptionsMod.LiveCaptions.Abstract;
using VRCLiveCaptionsMod.LiveCaptions.GameSpecific;

namespace VRCLiveCaptionsMod.LiveCaptions {
    class SessionPool {
        private Dictionary<string, TranscriptSession> sessions = new Dictionary<string, TranscriptSession>();

        public int samplerate { get; private set; }

        public SessionPool(int samplerate) {
            this.samplerate = samplerate;

            GameUtils.GetProvider().AudioSourceRemoved += OnPlayerLeft;
            AudioSourceOverrides.OnRemovedFromWhitelist += (uid) => DeleteSession(uid);
        }

        private void OnPlayerLeft(IAudioSource ply) {
            DeleteSession(ply);
        }

        public void InitializeSession(IAudioSource src) {
            TranscriptSession rec = new TranscriptSession(src, this.samplerate);

            sessions[src.GetUID()] = rec;
        }

        public void DeleteAllSessions() {
            foreach(TranscriptSession session in sessions.Values) {
                try {
                    session.FullDispose();
                }catch(Exception e) {
                    GameUtils.LogError("DELETESSSIONS: " + e.ToString());
                }
            }

            sessions.Clear();
        }

        private void DeleteByValue(TranscriptSession session) {
            foreach(string key in sessions.Keys) {
                if(sessions[key] == session) {
                    session.FullDispose();
                    sessions.Remove(key);
                    return;
                }
            }
        }

        public void DeleteSession(TranscriptSession session) {
            if((session.audioSource == null) || (!DeleteSession(session.audioSource.GetUID()))) {
                DeleteByValue(session);
                return;
            }
        }

        public void DeleteSession(IAudioSource src) {
            DeleteSession(src.GetUID());
        }

        public bool DeleteSession(string uid) {
            if(!sessions.ContainsKey(uid)) return false;

            TranscriptSession rec = sessions[uid];

            rec.FullDispose();
            sessions.Remove(uid);

            return true;
        }


        public TranscriptSession GetOrCreateSession(IAudioSource src) {
            if(!sessions.ContainsKey(src.GetUID())) {
                InitializeSession(src);
            }
            TranscriptSession rec = sessions[src.GetUID()];

            return rec;
        }

        public Dictionary<string, TranscriptSession>.ValueCollection GetSessions() {
            return sessions.Values;
        }
    }
}
