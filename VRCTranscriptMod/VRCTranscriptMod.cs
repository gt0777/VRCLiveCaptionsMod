
using MelonLoader;
using UnityEngine;
using VRChatUtilityKit.Utilities;

using Vosk;

using VRChatUtilityKit.Ui;
using VRCTranscriptMod.VRCTranscribe;

namespace VRCTranscriptMod {
    public class VRCTranscriptMod : MelonMod {
        internal static VRCTranscriptMod Instance { get; private set; }

        Model model;

        public override void OnApplicationStart() {
            Instance = this;

            VRCUtils.Init();
            VRCUtils.OnUiManagerInit += OnUiManagerInit;

            UiManager.Init();

            // TODO: don't crash if model is missing
            model = new Model("C:\\model");
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

            worker = new TranscribeWorker(model);
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