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
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRChatUtilityKit.Ui;


namespace VRCLiveCaptionsMod.LiveCaptions.GameSpecific.VRChat {
    class TranscriptPlayerUi {
        private const string uipath = "UserInterface/QuickMenu/UserInteractMenu";
        private const string name = "Enable Live Transcript";
        private const string tooltip = "Toggles live transcription of the user's voice. This may use a lot of memory.";

        private static string currently_active_uid = "";
        private static void onQuickMenuOpen(VRC.Core.APIUser param_1) {
            currently_active_uid = param_1.id;
            transcriptToggle.State = AudioSourceOverrides.IsWhitelisted(currently_active_uid);
        }

        private static ToggleButton transcriptToggle;

        public static void Init() { 
            // Insert toggle button into the user interact menu
            GameObject userInteractMenu = GameObject.Find(uipath);

            transcriptToggle = new ToggleButton(
                userInteractMenu, new Vector3(-1, 2),
                name, "Disabled",
                new Action<bool>((state) => {
                    AudioSourceOverrides.SetOverride(currently_active_uid, state);
                }),
                tooltip, tooltip, "TranscriptToggle", false, true
            );

            transcriptToggle.gameObject.AddComponent<GraphicRaycaster>();
            transcriptToggle.gameObject.AddComponent<VRCSDK2.VRC_UiShape>();
            

            // Patch the method for when quickmenu is opened, to extract the targeted player's UID.
            foreach(MethodInfo method in typeof(MenuController).GetMethods().Where(mi => mi.Name.StartsWith("Method_Public_Void_APIUser_") && !mi.Name.Contains("_PDM_")))
                VRCLiveCaptionsModMain.Instance.HarmonyInstance.Patch(method, postfix: new HarmonyMethod(typeof(TranscriptPlayerUi).GetMethod(nameof(onQuickMenuOpen), BindingFlags.NonPublic | BindingFlags.Static)));
        }

    }
}
