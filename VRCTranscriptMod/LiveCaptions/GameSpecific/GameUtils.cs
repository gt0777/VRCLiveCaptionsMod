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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using VRC.Core;
using VRC.SDKBase;
using VRChatUtilityKit.Utilities;
using VRCLiveCaptionsMod.LiveCaptions.Abstract;
using VRCLiveCaptionsMod.LiveCaptions.GameSpecific.VRChat;
using VRCLiveCaptionsMod.LiveCaptions.VoskSpecific;

namespace VRCLiveCaptionsMod.LiveCaptions.GameSpecific {
    static class VRCPlayerUtils {
        public static string GetUID(VRCPlayer ply) {
            return ply.prop_String_2;
        }

        public static string GetDisplayName(VRCPlayer ply) {
            return ply.prop_String_0;
        }
    }

    class VRCPlayerAudioSource : IAudioSource {
        private VRCPlayer _ply;
        private Transform head_transform;

        public VRCPlayerAudioSource(VRCPlayer ply) {
            _ply = ply;
        }

        public string GetFriendlyName() {
            return VRCPlayerUtils.GetDisplayName(_ply);
        }

        public Vector3 GetPosition() {
            if(head_transform == null) {
                head_transform = _ply.gameObject.transform.Find("AnimationController/HeadAndHandIK/HeadEffector");
            }
            return head_transform.position;
        }

        public string GetUID() {
            return VRCPlayerUtils.GetUID(_ply);
        }

        public bool IsImportant() {
            return GameUtils.GetProvider().IsUidImportant(GetUID());
        }
    }

    class VRChatGameProvider : IGameProvider {
        public event Action<IAudioSource, float[], int, int> AudioEmitted;
        public event Action<IAudioSource> AudioSourceAdded;
        public event Action<IAudioSource> AudioSourceRemoved;
        public event Action AllAudioSourcesRemoved;

        public VRChatGameProvider() {
            string folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\";
            VoskUtil.EnsureDependencies(folder);

            USpeakHooker.Init();
            USpeakHooker.OnRawAudio += OnRawAudioReceivedFromUSpeak;

            NetworkEvents.OnRoomLeft += () => AllAudioSourcesRemoved?.DelegateSafeInvoke();

            NetworkEvents.OnPlayerJoined += (ply) => {
                AudioSourceAdded?.DelegateSafeInvoke(
                    PlayerToAudioSource(ply.gameObject.GetComponent<VRCPlayer>())
                );
            };

            NetworkEvents.OnPlayerLeft += (ply) => {
                IAudioSource src = PlayerToAudioSource(ply.gameObject.GetComponent<VRCPlayer>());
                AudioSourceRemoved?.DelegateSafeInvoke(src);

                plyToAudioSource.Remove(src.GetUID());
            };

            TranscriptPlayerUi.Init();

            Settings.Init();
            SettingsTabMenu.Init();
        }

        private Dictionary<string, VRCPlayerAudioSource> plyToAudioSource = new Dictionary<string, VRCPlayerAudioSource>();
        private IAudioSource PlayerToAudioSource(VRCPlayer ply) {
            string uid = VRCPlayerUtils.GetUID(ply);
            if(!plyToAudioSource.ContainsKey(uid) || plyToAudioSource[uid] == null) {
                plyToAudioSource[uid] = new VRCPlayerAudioSource(ply);
            }

            return plyToAudioSource[uid];
        }

        private void OnRawAudioReceivedFromUSpeak(VRCPlayer ply, float[] samples, int sample_rate) {
            AudioEmitted?.DelegateSafeInvoke(PlayerToAudioSource(ply), samples, samples.Length, sample_rate);
        }

        public Vector3 GetLocalHeadPosition() {
            if(Networking.LocalPlayer == null) return Vector3.zero;

            return Networking.LocalPlayer.GetBonePosition(HumanBodyBones.Head);
        }

        public bool IsUidImportant(string uid) {
            return APIUser.IsFriendsWith(uid);
        }
    };

    public static class GameUtils {
        private static VRChatGameProvider provider;
        public static void Init() {
            provider = new VRChatGameProvider();
        }

        public static IGameProvider GetProvider() {
            return provider;
        }

        public static void Log(string s) {
            MelonLogger.Msg(s);
        }

        public static void LogWarn(string s) {
            MelonLogger.Warning(s);
        }

        public static void LogError(string s) {
            MelonLogger.Error(s);
        }

        public static void LogDebug(string s) {
#if DEBUG
            Log(s);
#endif
        }
        
        public static IVoiceRecognizer GetVoiceRecognizer() {
            if(Settings.Vosk_model == null) return null;

            return new VoskVoiceRecognizer(Settings.Vosk_model);
        }

        public static Transform GetSubtitleUiParent() {
            return GameObject.Find("UserInterface").transform;
        }

        public static string GetPathForModels() {
            string folder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Models\";

            if(!Directory.Exists(folder)) {
                Directory.CreateDirectory(folder);
            }

            return folder;
        }
    }
}
