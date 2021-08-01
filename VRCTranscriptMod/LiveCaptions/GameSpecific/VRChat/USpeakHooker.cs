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
using MelonLoader;


namespace VRCLiveCaptionsMod.LiveCaptions.GameSpecific.VRChat {
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
