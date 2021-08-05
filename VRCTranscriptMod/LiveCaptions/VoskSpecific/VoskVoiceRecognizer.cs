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
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using Vosk;
using VRCLiveCaptionsMod.LiveCaptions.GameSpecific;

namespace VRCLiveCaptionsMod.LiveCaptions.VoskSpecific {
    

    class DepFile {
        public string filename;
        public string checksum;
        public DepFile(string a, string b) {
            filename = a;
            checksum = b;
        }
    }

    static class VoskUtil {
        public static string extractTextFromResult(string result) {
            try {
                string spaceless = result.Replace(" ", "");
                if(spaceless.Equals("{\"text\":\"\"}")) return "";
                if(spaceless.Equals("{\"partial\":\"\"}")) return "";

                string line_with_result = result.Split('\n').First(line => line.TrimStart().Replace(" ", "").StartsWith("\"partial\":") || line.TrimStart().Replace(" ", "").StartsWith("\"text\":"));

                string text = line_with_result.Split('"')[3];

                return text;
            } catch(System.InvalidOperationException) {
                GameUtils.LogError("INVALID RESULT GIVEN: " + result);
                return "";
            } catch(Exception) {
                GameUtils.LogError("Failed to extract: " + result);
                return "";
            }
        }

        public static string CheckSum(string filePath) {
            using(SHA512 sha512 = SHA512Managed.Create()) {
                using(FileStream fileStream = File.OpenRead(filePath))
                    return BitConverter.ToString(sha512.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
            }
        }

        // from https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs) {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if(!dir.Exists) {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach(FileInfo file in files) {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if(copySubDirs) {
                foreach(DirectoryInfo subdir in dirs) {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }



        private static void DownloadModel(string path, string url, string folder_name) {
            string tempPath = Path.GetTempPath();
            string tgtPath = tempPath + folder_name + ".zip";
            string tgtPathExtract = tempPath + folder_name + "_ext";

            WebClient client = new WebClient();
            client.DownloadFileCompleted += (sender, e) => {
                try {
                    if(Directory.Exists(tgtPathExtract)) {
                        Directory.Delete(tgtPathExtract, true);
                    }

                    ZipFile.ExtractToDirectory(tgtPath, tgtPathExtract);
                    File.Delete(tgtPath);

                    string[] dirName = Directory.GetDirectories(tgtPathExtract);
                    if(dirName.Length == 0) {
                        GameUtils.LogError("Couldn't find the directory!");
                    }

                    DirectoryCopy(dirName[0], path + folder_name, true);
                    GameUtils.Log("Installed " + folder_name);
                } catch(Exception err) {
                    GameUtils.LogError("Extraction failed!! " + err.ToString());
                } finally {
                    try {
                        if (Directory.Exists(tgtPathExtract)) {
                            Directory.Delete(tgtPathExtract, true);
                        }
                    } catch(Exception err) {
                        GameUtils.LogError("Cleanup failed!! " + err.ToString());
                    }
                }
            };

            GameUtils.Log("Downloading " + url + " to " + tgtPath);
            client.DownloadFileAsync(new Uri(url), tgtPath);
        }

        public static void EnsureModels(string path) {
            string[] subdirs = Directory.GetDirectories(path);

            bool found_englishLight = false;
            foreach(string subdir in subdirs) {
                if(subdir.Contains("english-light")) found_englishLight = true;
            }

            if(!found_englishLight) {
                // Download
                DownloadModel(path, "http://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip", "english-light");
            }
        }

        static DepFile[] depFiles { get; } = {
            new DepFile(
                "libvosk.dll",
                "b7dd41023908bb5b393f306533d55a00344a6256ba412805d2293f229a6517baa67c928a5b5f536cad143852743ebd531be1c3f5ac8f617d42db1ffac8531231"
            ),

            new DepFile(
                "libwinpthread-1.dll",
                "987f55c17d7befd0c0ac9c31aa6999f5233d94056315def8c1492f7c21e26bf66c0db213ade915760cfa96211e00a152f4a9442f7ca83c6b60114a8c7ee18797"
            ),

            new DepFile(
                "libstdc++-6.dll",
                "188765f726cb6e68e05d43fd8479041c9189500028783970ed14a019605a2c483718356f78c545960232a8d769f1a3c971c92bbebf35593cf5cb967d602c63c8"
            ),

            new DepFile(
                "libgcc_s_seh-1.dll",
                "df58fbaa943c6ddb8b495607664957dee89a710b12cf5392f61693f5601e2aaa0773e9222633f88392c3494263ae4b735fc954e6a3ade9fa3629b90529359b68"
            )
        };
        public static void EnsureDependencies(string path) {
            bool dependenciesExist = true;
            foreach(DepFile file in depFiles) {
                if(!File.Exists(path + file.filename)) dependenciesExist = false;
            }

            if(dependenciesExist) {
                GameUtils.Log("Dependencies exist, returning...");
                return;
            }

            GameUtils.Log("Some dependencies are missing! Need to download them...");

            string tempPath = Path.GetTempPath();
            string voskDepPath = tempPath + "voskdeps.zip";

            if(File.Exists(voskDepPath)) {
                File.Delete(voskDepPath);
            }

            GameUtils.Log("Downloading to " + voskDepPath);
            using(WebClient client = new WebClient()) {
                client.DownloadFile("https://github.com/alphacep/vosk-api/releases/download/v0.3.30/vosk-win64-0.3.30.zip", voskDepPath);
            }

            string extractPath = tempPath + "voskdeps_ext";

            if(Directory.Exists(extractPath)) {
                Directory.Delete(extractPath, true);
            }

            ZipFile.ExtractToDirectory(voskDepPath, extractPath);
            File.Delete(voskDepPath);

            string folderWithContents = extractPath + @"\vosk-win64-0.3.30\";

            GameUtils.Log("Extracted to " + extractPath);

            foreach(DepFile file in depFiles) {
                if(CheckSum(folderWithContents + file.filename).ToLower() != file.checksum.ToLower()) {
                    GameUtils.LogError("Downloaded file " + file.filename + " hash does not match! Retrying...");
                    EnsureDependencies(path);
                    return;
                }
            }

            // at this point, we've verified the hash of each file, so it should be 
            // safe to copy them over


            foreach(DepFile file in depFiles) {
                if(File.Exists(path + file.filename)) {
                    File.Delete(path + file.filename);
                }

                GameUtils.Log("Copy " + path + file.filename);
                File.Copy(folderWithContents + file.filename, path + file.filename);
            }

            GameUtils.Log("Successfully installed dependencies!");
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
