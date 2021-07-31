using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;


namespace VRCLiveCaptionsMod.LiveCaptions.GameSpecific {
    class USpeakHooker {
        public static event Action<VRCPlayer, float[], int> OnRawAudio;
        
        public static bool Init() {
            MethodInfo decompressedAudioReceiver = null;

            string debug_info = "";

            foreach(MethodInfo info in typeof(USpeaker).GetMethods().Where(
                mi => mi.GetParameters().Length == 4
            )) {
                debug_info += (String.Format("Method: {1} {0}({2} params)",
                        info.Name,
                        info.ReturnType.ToString(),
                        info.GetParameters().Length.ToString()
                    ));

                int ints = 0;
                int floats = 0;
                foreach(ParameterInfo inf in info.GetParameters()) {
                    debug_info += (String.Format(" - Param {0}", inf.ParameterType.ToString())) + "\n";
                    if(inf.ParameterType.ToString().Contains("Single")) floats++;
                    if(inf.ParameterType.ToString().Contains("Int32")) ints++;
                }

                if(ints == 2 && floats == 2 && !info.Name.Contains("PDM")) {
                    decompressedAudioReceiver = info;
                }
            }

            if(decompressedAudioReceiver == null) {
                MelonLogger.Error("Couldn't find decompressedAudioReceiver!");
                MelonLogger.Error(debug_info);
                return false;
            }

            VRCLiveCaptionsModMain.Instance.HarmonyInstance.Patch(decompressedAudioReceiver, postfix: 
                new HarmonyMethod(typeof(USpeakHooker).GetMethod(nameof(onDecompressedAudio), BindingFlags.NonPublic | BindingFlags.Static)));

            return true;
        }

        private static void onDecompressedAudio(USpeaker __instance, UnhollowerBaseLib.Il2CppStructArray<float> param_1, float param_2, int param_3, int param_4) {
            VRCPlayer tgt_ply = __instance.field_Private_VRCPlayer_0;
            if(tgt_ply == null) return;

            int sample_rate = 48000; //default vrchat
            if(__instance.field_Private_AudioClip_0 != null)
                sample_rate = __instance.field_Private_AudioClip_0.frequency;

            OnRawAudio(tgt_ply, param_1, sample_rate);
        }

    }
}
