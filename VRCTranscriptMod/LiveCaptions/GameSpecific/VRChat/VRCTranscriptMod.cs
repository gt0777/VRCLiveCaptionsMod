
using MelonLoader;
using UnityEngine;
using VRChatUtilityKit.Utilities;

using VRChatUtilityKit.Ui;
using VRCLiveCaptionsMod.LiveCaptions;
using VRCLiveCaptionsMod.LiveCaptions.VoskSpecific;
using VRCLiveCaptionsMod.LiveCaptions.GameSpecific;

namespace VRCLiveCaptionsMod {
    public class VRCLiveCaptionsModMain : MelonMod {
        internal static VRCLiveCaptionsModMain Instance { get; private set; }

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
            
            AudioSourceOverrides.Init();
            SubtitleUi.Init();

            GameUtils.Init();

            worker = new TranscribeWorker();

        }

        public override void OnApplicationQuit() {

        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName) {

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