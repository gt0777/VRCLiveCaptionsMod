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
using System.IO;
using System.Threading;
using Vosk;
using VRCLiveCaptionsMod.LiveCaptions.GameSpecific;
using VRCLiveCaptionsMod.LiveCaptions.TranscriptData;
using VRCLiveCaptionsMod.LiveCaptions.VoskSpecific;

namespace VRCLiveCaptionsMod.LiveCaptions {
    class Settings {
        public static string model_directory = GameUtils.GetPathForModels();
        public static float transcribe_range = 6.0f;

        public static Model Vosk_model { get; private set; }

        public static event Action<bool> DisableChanging;
        public static event Action<bool> TranscribeChanging;
        public static event Action ModelChanged;
        public static event Action<float> TextScaleChanging;

        private static bool _disabled = false;
        private static bool _autoTranscribeWhenInRange = false;
        private static string _modelName = "";
        private static float _textScale = 1.0f;

        public static bool Disabled {
            get => _disabled;
            set {
                if(_disabled != value) DisableChanging?.DelegateSafeInvoke(value);
                _disabled = value;
            }
        }

        public static bool AutoTranscribeWhenInRange {
            get => _autoTranscribeWhenInRange;
            set {
                if(_autoTranscribeWhenInRange != value) TranscribeChanging?.DelegateSafeInvoke(value);
                _autoTranscribeWhenInRange = value;
            }
        }

        private static Thread loader;
        public static bool Loading { get; private set; }
        public static string ModelName {
            get => _modelName;
            set {
                if(_modelName != value) {
                    if(loader == null || !loader.IsAlive) {
                        _modelName = value;
                        Loading = true;
                        loader = new Thread(loadNewModel);
                        loader.Start();
                    }
                }
            }
        }

        public static ProfanityFilter.FilterLevel ProfanityFilterLevel = ProfanityFilter.FilterLevel.ALL;


        private static void loadNewModel() {
            Vosk_model = null;
            ModelChanged?.DelegateSafeInvoke();

            try {
                if(Directory.Exists(model_directory + _modelName)) {
                    Vosk_model = new Model(model_directory + _modelName);
                } else {
                    // directory doesn't exist!
                }
            } catch(Exception e) {
                // failed
                Console.WriteLine("Failed to load model " + model_directory + _modelName + " : " + e.ToString());
                Vosk_model = null;
            } finally {
                Loading = false;
                ModelChanged?.DelegateSafeInvoke();
                VoiceRecognizerEvents.FireChangedEvent();
            }
        }

        public static float TextScale {
            get => _textScale;
            set {
                if(_textScale != value) TextScaleChanging?.DelegateSafeInvoke(value);
                _textScale = value;
            }
        }

        public static string GetModelPath() {
            return model_directory + ModelName;
        }

        public static bool ModelExists() {
            return Directory.Exists(GetModelPath());
        }



        public static void Init() {
            VoskUtil.EnsureModels(model_directory);
        }

    }
}
