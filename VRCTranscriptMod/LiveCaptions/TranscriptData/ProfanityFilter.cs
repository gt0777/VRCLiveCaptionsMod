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
