﻿using MelonLoader;
using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;
using VRChatUtilityKit.Utilities;
using VRCLiveCaptionsMod.LiveCaptions.Abstract;
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
    }

    class VRChatGameProvider : IGameProvider {
        public event Action<IAudioSource, float[], int, int> AudioEmitted;
        public event Action<IAudioSource> AudioSourceAdded;
        public event Action<IAudioSource> AudioSourceRemoved;
        public event Action AllAudioSourcesRemoved;

        public VRChatGameProvider() {
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
        
        public static IVoiceRecognizer GetVoiceRecognizer() {
            if(Settings.Vosk_model == null) return null;

            return new VoskVoiceRecognizer(Settings.Vosk_model);
        }

        public static Transform GetSubtitleUiParent() {
            return GameObject.Find("UserInterface").transform;
        }
    }
}