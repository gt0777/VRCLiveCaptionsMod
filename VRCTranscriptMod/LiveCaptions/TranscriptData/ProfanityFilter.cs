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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRCLiveCaptionsMod.LiveCaptions.TranscriptData {
    static class ProfanityFilter {
        const string BadWordReplacement = "[_]";

        public enum FilterLevel {
            NONE,
            SLURS,
            ALL
        };

        private static bool WordIsBad(string word, string[] badwords) {
            string a = word.ToLower();
            foreach(string badword in badwords) {
                string b = badword.ToLower();

                // TODO: this sucks, and is quite inefficient
                if(a.Equals(b) || a.Equals(b + "s") || a.Equals(b + "es") || a.Equals(b + "ing")) {
                    return true;
                }
            }
            return false;
        }

        private static string FilterWithWordList(string input, string[] badwords) {
            string output = "";

            string[] lines = input.Split('\n');
            foreach(string line in lines) {
                string[] words = line.Split(' ');
                foreach(string word in words) {
                    output = output + (WordIsBad(word, badwords) ? BadWordReplacement : word) + " ";
                }
                output = output + '\n';
            }

            output = output.TrimEnd('\n', ' ');

            return output;
        }

        public static string FilterString(string input, FilterLevel level) {
            string output = input;
            switch(level) {
                case FilterLevel.ALL:
                    output = FilterWithWordList(output,
                        TranscriptData.profanities.Profanities.words);
                    goto case FilterLevel.SLURS;

                case FilterLevel.SLURS:
                    output = FilterWithWordList(output,
                        TranscriptData.profanities.Slurs.words);
                    goto case FilterLevel.NONE;

                case FilterLevel.NONE:
                default:
                    break;
            }

            return output;
        }
    }
}
