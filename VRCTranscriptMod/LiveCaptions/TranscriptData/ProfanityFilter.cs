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
        
        private static string FilterWithWordList(string input, Func<string, bool> IsBadWord) {
            string output = "";

            string[] lines = input.Split('\n');
            foreach(string line in lines) {
                string[] words = line.Split(' ');
                foreach(string word in words) {
                    output = output + (IsBadWord(word.ToLower()) ? BadWordReplacement : word) + " ";
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
                        TranscriptData.profanities.Profanities.IsWordBad);
                    goto case FilterLevel.SLURS;

                case FilterLevel.SLURS:
                    output = FilterWithWordList(output,
                        TranscriptData.profanities.Slurs.IsWordBad);
                    goto case FilterLevel.NONE;

                case FilterLevel.NONE:
                default:
                    break;
            }

            return output;
        }
    }
}
