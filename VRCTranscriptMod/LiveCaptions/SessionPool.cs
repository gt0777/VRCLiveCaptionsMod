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
