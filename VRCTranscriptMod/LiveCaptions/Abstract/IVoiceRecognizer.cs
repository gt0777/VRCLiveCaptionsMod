using System;

namespace VRCLiveCaptionsMod {
    public class VoiceRecognizerEvents {
        /// <summary>
        /// When fired, all current voice recognizers should be disposed
        /// and a new one should be retrieved.
        /// </summary>
        public static event Action VoiceRecognizerChanged;

        /// <summary>
        /// Fires the VoiceRecognizerChanged event. This should only be used
        /// when voice recognizers should be changed
        /// </summary>
        public static void FireChangedEvent() {
            VoiceRecognizerChanged?.DelegateSafeInvoke();
        }
    };

    /// <summary>
    ///  An abstract voice recognizer interface
    /// </summary>
    public interface IVoiceRecognizer {

        /// <summary>
        /// Initializes the voice recognizer. This is required
        /// prior to making calls for recognition.
        /// </summary>
        /// <param name="sample_rate">The sample rate (example: 44100, 48000, 22050)</param>
        void Init(int sample_rate);


        /// <summary>
        /// Performs voice recognition on the given parameters.
        /// </summary>
        /// <param name="samples">Samples in float array form</param>
        /// <param name="len">Length of the array to read</param>
        /// <returns>Whether or not the recognition has finished, and the string from get_text() is final.</returns>
        bool Recognize(float[] samples, int len);

        /// <summary>
        /// Performs voice recognition on the given parameters.
        /// </summary>
        /// <param name="samples">Samples in PCM-16 signed short array form</param>
        /// <param name="len">Length of the array to read</param>
        /// <returns>Whether or not the recognition has finished, and the string from get_text() is final.</returns>
        bool Recognize(short[] samples, int len);

        /// <summary>
        /// Flushes the recognition and makes the subsequent get_text call return a final result.
        /// </summary>
        void Flush();


        /// <summary>
        /// Returns the recognized text.
        /// </summary>
        /// <returns>Text in string form</returns>
        string GetText();
    }
}
