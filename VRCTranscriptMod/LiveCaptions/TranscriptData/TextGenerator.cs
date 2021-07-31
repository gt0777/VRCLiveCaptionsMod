using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRCLiveCaptionsMod.LiveCaptions.TranscriptData {
    class TextGenerator {
        public string FullText { get; private set; } = "";

        const int maxLines = 2;
        private float lastTextUpdate = 0.0f;

        public void UpdateText(List<Saying> past_sayings, Saying active_saying) {
            if(Utils.GetTime() - lastTextUpdate < 0.03) return;
            lastTextUpdate = Utils.GetTime();

            // Merge past and active to make codepath simpler
            Saying[] allSayings;

            allSayings = new Saying[past_sayings.Count +
                ((active_saying != null) ? 1 : 0)];
            for(int i = 0; i < past_sayings.Count; i++) {
                allSayings[i] = past_sayings[i];
            }

            if(active_saying != null)
                allSayings[past_sayings.Count] = active_saying;


            // Concat allSayings
            string full = "";
            for(int i = 0; i < allSayings.Length; i++) {
                float prevAge = 0.0f;
                if(i > 0) {
                    prevAge = allSayings[i - 1].timeEnd;
                }

                float diff = Math.Abs(allSayings[i].timeStart - prevAge);
                if(diff > TranscriptSession.sepAge) {
                    full = full + '\n';
                } else {
                    full = full + ' ';
                }
                full = full + allSayings[i].fullTxt;
            }

            if(full.Length > 2) {
                // Insert line breaks when a line is too long
                string tmp = "";
                int chars_in_current_line = 0;

                for(int i = 0; i < full.Length; i++) {
                    char c = full[i];

                    tmp = tmp + c;
                    if(c == '\n') {
                        chars_in_current_line = 0;
                        continue;
                    }

                    chars_in_current_line++;

                    if(chars_in_current_line >= 48 && c == ' ') {
                        tmp = tmp + '\n';
                        chars_in_current_line = 0;
                    }
                }

                // Grab only the last ${maxLines} lines
                string[] lines = tmp.Split('\n');

                full = "";
                for(int i = 0; i < lines.Length; i++) {
                    if(i >= (lines.Length - maxLines)) {
                        full = full + lines[i] + '\n';
                    }
                }
            }

            FullText = full;
        }
    }
}
