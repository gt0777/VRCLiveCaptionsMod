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
        public float lastFillTime = 0.0f;

        public void StartTranscribing() {
            readWriteMutex.WaitOne();
            beingTranscribed = true;
        }

        public void StopTranscribing() {
            readWriteMutex.ReleaseMutex();
            beingTranscribed = false;
            buffer_head = 0;
        }
    }
}
