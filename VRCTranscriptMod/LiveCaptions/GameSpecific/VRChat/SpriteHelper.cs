using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VRCLiveCaptionsMod.LiveCaptions.GameSpecific.VRChat {
    // Taken from UnityExplorer
    public static class ICallHelper {
        private static readonly Dictionary<string, Delegate> iCallCache = new Dictionary<string, Delegate>();

        public static T GetICall<T>(string iCallName) where T : Delegate {
            if(iCallCache.ContainsKey(iCallName))
                return (T)iCallCache[iCallName];

            IntPtr ptr = il2cpp_resolve_icall(iCallName);

            if(ptr == IntPtr.Zero) {
                throw new MissingMethodException($"Could not resolve internal call by name '{iCallName}'!");
            }

            Delegate iCall = Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
            iCallCache.Add(iCallName, iCall);

            return (T)iCall;
        }

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr il2cpp_resolve_icall([MarshalAs(UnmanagedType.LPStr)] string name);
    }

    // Taken from UnityExplorer
    class SpriteHelper {
        internal delegate IntPtr d_CreateSprite(IntPtr texture, ref Rect rect, ref Vector2 pivot, float pixelsPerUnit,
    uint extrude, int meshType, ref Vector4 border, bool generateFallbackPhysicsShape);

        public static Sprite CreateSprite(Texture texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, Vector4 border) {
            var iCall = ICallHelper.GetICall<d_CreateSprite>("UnityEngine.Sprite::CreateSprite_Injected");

            var ptr = iCall.Invoke(texture.Pointer, ref rect, ref pivot, pixelsPerUnit, extrude, 1, ref border, false);

            if(ptr == IntPtr.Zero)
                return null;
            else
                return new Sprite(ptr);
        }
    }

}
