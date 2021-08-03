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

using System.Threading;

#if USE_SHORT
using BUFFER_TYPE = System.Int16;
#else
using BUFFER_TYPE = System.Single;
#endif

namespace VRCLiveCaptionsMod.LiveCaptions.TranscriptData {
    /// <summary>
    /// Audio buffer that contains additional useful utilities
    /// </summary>
    class AudioBuffer {
        public const int buffer_size = 16000;
        public BUFFER_TYPE[] buffer = new BUFFER_TYPE[buffer_size];
        public int buffer_head = 0;

        public Mutex readWriteMutex = new Mutex();

        public bool beingTranscribed = false;
        public bool queued = false;

        public float lastFillTime = 0.0f;


        public void StartTranscribing() {
            readWriteMutex.WaitOne();
            beingTranscribed = true;
        }

        public void StopTranscribing() {
            readWriteMutex.ReleaseMutex();
            beingTranscribed = false;
            buffer_head = 0;

            queued = false;
        }

        public bool ShouldBeQueued() {
            return buffer_head > (buffer_size - 1000);
        }
    }
}
