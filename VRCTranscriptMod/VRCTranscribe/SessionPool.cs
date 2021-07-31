using MelonLoader;
using System;
using System.Collections.Generic;
using Vosk;
using VRChatUtilityKit.Utilities;

namespace VRCTranscriptMod.VRCTranscribe {
    class SessionPool {
        private Dictionary<string, TranscriptSession> sessions = new Dictionary<string, TranscriptSession>();

        public float samplerate { get; private set; }

        public SessionPool(float samplerate) {
            this.samplerate = samplerate;

            NetworkEvents.OnPlayerLeft += OnPlayerLeft;
            TranscriptPlayerOverrides.OnRemovedFromWhitelist += DeleteSession;
        }

        private void OnPlayerLeft(VRC.Player ply) {
            DeleteSession(Utils.GetUID(ply));
        }

        public void InitializeSession(string uid) {
            TranscriptSession rec = new TranscriptSession(this.samplerate);

            sessions[uid] = rec;
        }

        public void DeleteSession(string uid) {
            if(!sessions.ContainsKey(uid)) return;
            TranscriptSession rec = sessions[uid];

            rec.FullDispose();
            sessions.Remove(uid);
        }

        public void DeleteAllSessions() {
            foreach(TranscriptSession session in sessions.Values) {
                try {
                    session.FullDispose();
                }catch(Exception e) {
                    MelonLogger.Error("DELETESSSIONS: " + e.ToString());
                }
            }

            sessions.Clear();
        }

        public void DeleteByValue(TranscriptSession session) {
            foreach(string key in sessions.Keys) {
                if(sessions[key] == session) {
                    session.FullDispose();
                    sessions.Remove(key);
                    return;
                }
            }
        }

        public TranscriptSession GetOrCreateSession(string uid) {
            if(!sessions.ContainsKey(uid)) {
                InitializeSession(uid);
            }
            TranscriptSession rec = sessions[uid];

            return rec;
        }

        public Dictionary<string, TranscriptSession>.ValueCollection GetSessions() {
            return sessions.Values;
        }
    }
}
