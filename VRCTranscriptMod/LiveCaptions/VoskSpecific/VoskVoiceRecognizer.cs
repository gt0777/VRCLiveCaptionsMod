﻿// VRCLiveCaptionsMod - a mod for providing voice chat live captions
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
using System.IO.Compression;
using System.Linq;
using System.Net;
using Vosk;
using VRCLiveCaptionsMod.LiveCaptions.GameSpecific;

namespace VRCLiveCaptionsMod.LiveCaptions.VoskSpecific {
    static class VoskUtil {
        public static string extractTextFromResult(string result) {
            try {
                string line_with_result = result.Split('\n').First(line => line.TrimStart().Replace(" ", "").StartsWith("\"partial\":") || line.TrimStart().Replace(" ", "").StartsWith("\"text\":"));

                string text = line_with_result.Split('"')[3];

                return text;
            } catch(System.InvalidOperationException) {
                GameUtils.LogError("INVALID RESULT GIVEN: " + result);
                return "ERROR";
            }
        }

        private static void DownloadModel(string path, string url, string folder_name) {
            string tempPath = Path.GetTempPath();
            string tgtPath = tempPath + folder_name + ".zip";
            string tgtPathExtract = tempPath + folder_name + "_ext";

            WebClient client = new WebClient();
            client.DownloadFileCompleted += (sender, e) => {
                try {
                    ZipFile.ExtractToDirectory(tgtPath, tgtPathExtract);
                    File.Delete(tgtPath);

                    string[] dirName = Directory.GetDirectories(tgtPathExtract);
                    if(dirName.Length == 0) {
                        GameUtils.LogError("Couldn't find the directory!");
                    }

                    Directory.Move(dirName[0], path + folder_name);
                    GameUtils.Log("Installed " + folder_name);
                } catch(Exception err) {
                    GameUtils.LogError("Extraction failed!! " + err.ToString());
                } finally {
                    Directory.Delete(tgtPathExtract);
                }
            };

            GameUtils.Log("Downloading " + url + " to " + tgtPath);
            client.DownloadFileAsync(new Uri(url), tgtPath);
        }

        public static void EnsureModels(string path) {
            string[] subdirs = Directory.GetDirectories(path);

            bool found_englishLight = false;
            foreach(string subdir in subdirs) {
                if(subdir.Equals("english-light")) found_englishLight = true;
            }

            if(!found_englishLight) {
                // Download
                DownloadModel(path, "http://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip", "english-light");
            }
        }
    }

    class VoskVoiceRecognizer : IVoiceRecognizer {
        private VoskRecognizer _vosk;
        private Model _model;
        private int _sample_rate;

        private string _text = "";


        private void ensure_state() {
            if(_vosk == null) throw new System.InvalidOperationException("The Init(int) method was not called..");
        }

        public VoskVoiceRecognizer(Model model) {
            _model = model;
        }

        public void Flush() {
            ensure_state();
            _text = VoskUtil.extractTextFromResult(_vosk.FinalResult());
        }

        public string GetText() {
            ensure_state();
            return _text;
        }

        public void Init(int sample_rate) {
            _sample_rate = sample_rate;
            _vosk = new VoskRecognizer(_model, _sample_rate);
        }

        public bool Recognize(float[] samples, int len) {
            ensure_state();

            bool result = _vosk.AcceptWaveform(samples, len);
            if(result) {
                _text = VoskUtil.extractTextFromResult(_vosk.Result());
            } else {
                _text = VoskUtil.extractTextFromResult(_vosk.PartialResult());
            }

            return result;
        }

        public bool Recognize(short[] samples, int len) {
            ensure_state();

            bool result = _vosk.AcceptWaveform(samples, len);
            if(result) {
                _text = VoskUtil.extractTextFromResult(_vosk.Result());
            } else {
                _text = VoskUtil.extractTextFromResult(_vosk.PartialResult());
            }

            return result;
        }
    }
}
