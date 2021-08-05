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

// #define DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRCLiveCaptionsMod.LiveCaptions {
    /// <summary>
    /// For debugging TranscriptSession
    /// </summary>
    class TranscriptSessionDebugger {
        string name;

#if DEBUG
        FileStream fileStream;
#endif

        public TranscriptSessionDebugger(string name) {
            this.name = name;
#if DEBUG
            fileStream = new FileStream("C:\\debug\\" + name, FileMode.Append, FileAccess.Write);
#endif
        }

        public void onSubmitSamples(short[] samples, int len) {
#if DEBUG
            byte[] bytesData = new byte[len * 2];
            
            for(int i=0; i<len; i++) {
                byte[] byteArr = BitConverter.GetBytes(samples[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            fileStream.Write(bytesData, 0, bytesData.Length);
#endif
        }

        public void createMarker(bool alwaysMin = false) {
#if DEBUG
            short[] fakeData = new short[4];
            fakeData[0] = System.Int16.MinValue;
            fakeData[2] = System.Int16.MinValue;

            fakeData[1] = alwaysMin ? fakeData[0] : System.Int16.MaxValue;
            fakeData[3] = alwaysMin ? fakeData[0] : System.Int16.MaxValue;

            onSubmitSamples(fakeData, 4);
#endif
        }

        public void cleanup() {
#if DEBUG
            fileStream.Close();
#endif
        }
    }
}
