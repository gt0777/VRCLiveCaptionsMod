
using MelonLoader;
using UnityEngine;
using VRChatUtilityKit.Utilities;

using Vosk;

using VRChatUtilityKit.Ui;
using VRCTranscriptMod.VRCTranscribe;

namespace VRCTranscriptMod {
    public class VRCTranscriptMod : MelonMod {
        internal static VRCTranscriptMod Instance { get; private set; }

        public override void OnApplicationStart() {
            Instance = this;

            VRCUtils.Init();
            VRCUtils.OnUiManagerInit += OnUiManagerInit;

            UiManager.Init();
            
        }


        TranscribeWorker worker;

        internal void OnUiManagerInit() {
            UiManager.UiInit();

            MelonLogger.Msg("UIMan Init");
            NetworkEvents.NetworkInit();

            USpeakHooker.Init();
            TranscriptPlayerOverrides.Init();
            TranscriptPlayerUi.Init();
            SubtitleUi.Init();
            SettingsTabMenu.Init();

            worker = new TranscribeWorker();

        }

        public override void OnApplicationQuit() {

        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName) {
            //if(worker != null) worker.CleanAndRestart();
        }

        private float lastUpdate = 0;
        public override void OnUpdate() {
            if(worker != null)
                worker.Tick();

            if((Time.time - lastUpdate) < 1.0f) return;
            lastUpdate = Time.time;
        }
        
    }

}