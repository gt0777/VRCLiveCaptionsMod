using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRChatUtilityKit.Ui;


namespace VRCLiveCaptionsMod.LiveCaptions.GameSpecific {
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
