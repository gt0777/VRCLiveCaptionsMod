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

/*
 *
 * 
 * 
 * 
 * 
 *
 * 
 * 
 * !!! WARNING !!!
 * 
 * This file contains offensive words.
 * 
 * This is used for the profanity filter to remove them
 * from transcribed text, if filtering is enabled.
 * 
 * Please don't continue if you're not prepared to see
 * racist slurs, profanities and other bad words.
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 */












using System.Collections.Generic;

namespace VRCLiveCaptionsMod.LiveCaptions.TranscriptData.profanities {
    public static class Slurs {
        public static readonly string[] words = {
            "nigger",
            "nigga",
            "nig",
            "coon",
            "chink",
            "kkk",
            "triple k",
            "wigger",
            "white boy",
            "whitey",
            "nazi",
            "hitler",
            "fag",
            "faggot",
            "whore",
            "retard",
            "retarded",
            "slut",
            "sluttish"
        };

        private static Dictionary<string, bool> BadWordDict = null;
        private static void Init() {
            BadWordDict = new Dictionary<string, bool>();
            foreach(string word in words) {
                BadWordDict[word.ToLower()] = true;
                BadWordDict[word.ToLower() + "s"] = true;
                BadWordDict[word.ToLower() + "es"] = true;
                BadWordDict[word.ToLower() + "ing"] = true;
            }
        }

        public static bool IsWordBad(string word) {
            if(BadWordDict == null) Init();

            return BadWordDict.ContainsKey(word);
        }
    }
}
