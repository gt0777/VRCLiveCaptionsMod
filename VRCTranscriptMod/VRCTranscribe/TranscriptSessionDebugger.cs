// #define DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRCTranscriptMod.VRCTranscribe {
    class TranscriptSessionDebugger {
        string name;

#if DEBUG
        FileStream fileStream;
#endif

        public TranscriptSessionDebugger(string name) {
            this.name = name;
#if DEBUG
            fileStream = new FileStream("C:\\debug\\" + name, FileMode.CreateNew, FileAccess.Write);
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

        public void onDispose() {
#if DEBUG
            short[] fakeData = new short[4];
            fakeData[0] = System.Int16.MinValue;
            fakeData[2] = System.Int16.MinValue;

            fakeData[1] = System.Int16.MaxValue;
            fakeData[3] = System.Int16.MaxValue;

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
