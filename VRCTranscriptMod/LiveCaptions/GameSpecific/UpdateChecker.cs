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

using Newtonsoft.Json.Linq;
using SemVer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace VRCLiveCaptionsMod.LiveCaptions.GameSpecific {
    class UpdateChecker {
        static string version_s = "0.1.2";
        static SemanticVersion version = SemanticVersion.Parse(version_s);

        private static int CompareVersions(string First, string Second) {
            SemanticVersion a = SemanticVersion.Parse(First);
            SemanticVersion b = SemanticVersion.Parse(Second);

            return a.CompareTo(b);
        }

        public static string Check() {
            const string tags_url = "https://api.github.com/repos/gt0777/VRCLiveCaptionsMod/tags";
            const string error_txt = "Failed to check for updates.";

            try {
                HttpWebRequest getTags;
                getTags = WebRequest.CreateHttp(tags_url);

                getTags.UserAgent = "VRCLiveCaptionsMod Update checker";

                WebResponse resp = getTags.GetResponse();

                StreamReader objReader = new StreamReader(resp.GetResponseStream());

                string sLine = objReader.ReadToEnd();

                var decoded = JArray.Parse(sLine);

                if(decoded.Type != JTokenType.Array) throw new Exception("Didn't receive an array");

                List<SemanticVersion> versions = new List<SemanticVersion>();
                foreach(JObject obj in decoded.Children()) {
                    try {
                        string name = obj.GetValue("name").ToString();

                        versions.Add(SemanticVersion.Parse(name.Replace("v", "")));
                    }catch(Exception) {
                        GameUtils.LogWarn("Received invalid version");
                    }
                }

                versions.Sort();

                SemanticVersion latest = versions.Last();

                if(latest > version) {
                    return "Your version (" + version_s + ") is out of date! The latest is " + latest.ToString() + ". Please visit https://github.com/gt0777/VRCLiveCaptionsMod";
                }

                return "Your version (" + version_s + ") is up-to-date.";


            } catch(Exception e) {
                GameUtils.LogError("Update checking failed: " + e.ToString());
                return error_txt;
            }
        }
    }
}
