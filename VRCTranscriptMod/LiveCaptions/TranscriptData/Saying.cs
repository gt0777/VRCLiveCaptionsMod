using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRCLiveCaptionsMod.LiveCaptions.TranscriptData {
    /// <summary>
    /// A result from the voice recognizer.
    /// </summary>
    class Saying {
        public string fullTxt { get; private set; }

        public float timeStart { get; private set; }
        public float timeEnd { get; private set; }

        public bool final { get; private set; }

        public Saying() {
            timeStart = Utils.GetTime();
            timeEnd = timeStart;
            fullTxt = "";
        }

        public void Update(string to, bool final = false) {
            fullTxt = to;
            timeEnd = Utils.GetTime();
            this.final = final;
        }

        // TODO: per-word time?
    }
}
