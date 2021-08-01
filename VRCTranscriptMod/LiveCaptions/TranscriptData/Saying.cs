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
