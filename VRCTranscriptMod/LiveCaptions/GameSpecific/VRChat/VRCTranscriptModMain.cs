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

#if INTEGRATED_VRCUK
        VRChatUtilityKit.VRChatUtilityKitMod vrcUtility = new VRChatUtilityKit.VRChatUtilityKitMod();
#endif 

        public override void OnApplicationStart() {
            Instance = this;
            
            VRCUtils.OnUiManagerInit += OnUiManagerInit;

#if INTEGRATED_VRCUK
            vrcUtility.OnApplicationStart();
#endif
        }


        TranscribeWorker worker;

        internal void OnUiManagerInit() {
            MelonLogger.Msg("UIMan Init");
            
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
#if INTEGRATED_VRCUK
            vrcUtility.OnUpdate();
#endif

            if(worker != null)
                worker.Tick();

            if((Time.time - lastUpdate) < 1.0f) return;
            lastUpdate = Time.time;
        }
        
    }

}