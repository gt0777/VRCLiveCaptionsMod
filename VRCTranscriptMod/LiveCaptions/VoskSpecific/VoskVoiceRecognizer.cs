using System.Linq;
using Vosk;
using VRCLiveCaptionsMod.LiveCaptions.GameSpecific;

namespace VRCLiveCaptionsMod.LiveCaptions.VoskSpecific {
    static class VoskUtil {
        public static string extractTextFromResult(string result) {
            try {
                string line_with_result = result.Split('\n').First(line => line.TrimStart().Replace(" ", "").StartsWith("\"partial\":") || line.TrimStart().Replace(" ", "").StartsWith("\"text\":"));

                string text = line_with_result.Split('"')[3];

                return text;
            } catch(System.InvalidOperationException) {
                GameUtils.LogError("INVALID RESULT GIVEN: " + result);
                return "ERROR";
            }
        }
    }

    class VoskVoiceRecognizer : IVoiceRecognizer {
        private VoskRecognizer _vosk;
        private Model _model;
        private int _sample_rate;

        private string _text = "";


        private void ensure_state() {
            if(_vosk == null) throw new System.InvalidOperationException("The Init(int) method was not called..");
        }

        public VoskVoiceRecognizer(Model model) {
            _model = model;
        }

        public void Flush() {
            ensure_state();
            _text = VoskUtil.extractTextFromResult(_vosk.FinalResult());
        }

        public string GetText() {
            ensure_state();
            return _text;
        }

        public void Init(int sample_rate) {
            _sample_rate = sample_rate;
            _vosk = new VoskRecognizer(_model, _sample_rate);
        }

        public bool Recognize(float[] samples, int len) {
            ensure_state();

            bool result = _vosk.AcceptWaveform(samples, len);
            if(result) {
                _text = VoskUtil.extractTextFromResult(_vosk.Result());
            } else {
                _text = VoskUtil.extractTextFromResult(_vosk.PartialResult());
            }

            return result;
        }

        public bool Recognize(short[] samples, int len) {
            ensure_state();

            bool result = _vosk.AcceptWaveform(samples, len);
            if(result) {
                _text = VoskUtil.extractTextFromResult(_vosk.Result());
            } else {
                _text = VoskUtil.extractTextFromResult(_vosk.PartialResult());
            }

            return result;
        }
    }
}
