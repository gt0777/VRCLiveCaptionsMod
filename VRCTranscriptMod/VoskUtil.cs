using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRCTranscriptMod {
    class VoskUtil {
        public static string extractTextFromResult(string result) {
            string line_with_result = result.Split('\n').First(line => line.TrimStart().Replace(" ", "").StartsWith("\"partial\":") || line.TrimStart().Replace(" ", "").StartsWith("\"text\":"));

            string text = line_with_result.Split('"')[3];

            return text;
        }
    }
}
