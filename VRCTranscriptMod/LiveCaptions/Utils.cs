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
