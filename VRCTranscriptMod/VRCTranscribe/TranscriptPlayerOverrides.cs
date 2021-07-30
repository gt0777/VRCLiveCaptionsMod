using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRChatUtilityKit.Utilities;

namespace VRCTranscriptMod.VRCTranscribe {
    class TranscriptPlayerOverrides {
        private static Dictionary<string, bool> overrides;

        public static event Action<string> OnRemovedFromWhitelist;
        public static event Action<string> OnAddedToWhitelist;
        
        public static void Init() {
            overrides = new Dictionary<string, bool>();
            // TODO: Load/save from file
        }

        public static void SetOverride(string uid, bool is_whitelisted) {
            if(uid == null) MelonLogger.Msg("UID IS NULL! WTF?");
            if(overrides == null) MelonLogger.Msg("overrides IS NULL! WTF?");

            overrides[uid] = is_whitelisted;

            if(is_whitelisted) OnAddedToWhitelist?.DelegateSafeInvoke(uid);
            else OnRemovedFromWhitelist?.DelegateSafeInvoke(uid);

            MelonLogger.Msg(uid + " is now " + is_whitelisted.ToString());
            

            // TODO: save config
        }

        public static bool IsWhitelisted(string uid) { // TODO: more general case, defaults?
            return overrides.ContainsKey(uid) && (overrides[uid] == true);
        }
    }
}
