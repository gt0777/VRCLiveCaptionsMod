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
