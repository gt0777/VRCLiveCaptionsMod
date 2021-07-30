using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VRCTranscriptMod {
    class Utils {
        public static string GetUID(VRCPlayer player) {
            return player.prop_String_2;
        }

        public static string GetUID(VRC.Player player) {
            return GetUID(player.prop_VRCPlayer_0);
        }

        public static float GetTime() {
            return Time.time;
        }
    }
}
