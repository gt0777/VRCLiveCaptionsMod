using System;
using UnityEngine;
using VRCLiveCaptionsMod.LiveCaptions.GameSpecific;

namespace VRCLiveCaptionsMod {
    /// <summary>
    /// Contains some useful utilities
    /// </summary>
    public static class Utils {
        /// <summary>
        /// Gets the current game time in seconds
        /// </summary>
        /// <returns>Time in seconds</returns>
        public static float GetTime() {
            return Time.time;
        }

        // Taken from VRChatUtilityKit
        /// <summary>
        /// Safely invokes the given delegate with the given args.
        /// </summary>
        /// <param name="delegate">The given delegate</param>
        /// <param name="args">The params of the delegate</param>
        public static void DelegateSafeInvoke(this Delegate @delegate, params object[] args) {
            if(@delegate == null)
                return;

            foreach(Delegate @delegates in @delegate.GetInvocationList()) {
                try {
                    @delegates.DynamicInvoke(args);
                } catch(Exception ex) {
                    GameUtils.LogError("Error while invoking delegate:\n" + ex.ToString());
                }
            }
        }
    }
}
