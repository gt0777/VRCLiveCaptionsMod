using MelonLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vosk;
using VRChatUtilityKit.Ui;
using UnhollowerBaseLib;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using VRChatUtilityKit.Utilities;
using System.Threading;

namespace VRCTranscriptMod.VRCTranscribe {
    public static class ICallHelper {
        private static readonly Dictionary<string, Delegate> iCallCache = new Dictionary<string, Delegate>();

        public static T GetICall<T>(string iCallName) where T : Delegate {
            if(iCallCache.ContainsKey(iCallName))
                return (T)iCallCache[iCallName];

            IntPtr ptr = il2cpp_resolve_icall(iCallName);

            if(ptr == IntPtr.Zero) {
                throw new MissingMethodException($"Could not resolve internal call by name '{iCallName}'!");
            }

            Delegate iCall = Marshal.GetDelegateForFunctionPointer(ptr, typeof(T));
            iCallCache.Add(iCallName, iCall);

            return (T)iCall;
        }

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr il2cpp_resolve_icall([MarshalAs(UnmanagedType.LPStr)] string name);
    }

    class SpriteHelper {
        internal delegate IntPtr d_CreateSprite(IntPtr texture, ref Rect rect, ref Vector2 pivot, float pixelsPerUnit,
    uint extrude, int meshType, ref Vector4 border, bool generateFallbackPhysicsShape);

        public static Sprite CreateSprite(Texture texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, Vector4 border) {
            var iCall = ICallHelper.GetICall<d_CreateSprite>("UnityEngine.Sprite::CreateSprite_Injected");

            var ptr = iCall.Invoke(texture.Pointer, ref rect, ref pivot, pixelsPerUnit, extrude, 1, ref border, false);

            if(ptr == IntPtr.Zero)
                return null;
            else
                return new Sprite(ptr);
        }
    }

    class Settings {
        public static string model_directory = "C:\\models\\";
        public static float transcribe_range = 6.0f;

        public static Model Vosk_model { get; private set; }

        public static event Action<bool> DisableChanging;
        public static event Action<bool> TranscribeChanging;
        public static event Action<Model> ModelChanged;
        public static event Action<float> TextScaleChanging;

        private static bool _disabled = false;
        private static bool _autoTranscribeWhenInRange = false;
        private static string _modelName = "example";
        private static float _textScale = 1.0f;

        public static bool Disabled {
            get => _disabled;
            set {
                if(_disabled != value) DisableChanging?.DelegateSafeInvoke(value);
                _disabled = value;
            }
        }

        public static bool AutoTranscribeWhenInRange {
            get => _autoTranscribeWhenInRange;
            set {
                if(_autoTranscribeWhenInRange != value) TranscribeChanging?.DelegateSafeInvoke(value);
                _autoTranscribeWhenInRange = value;
            }
        }

        private static Thread loader;
        public static bool Loading { get; private set; }
        public static string ModelName {
            get => _modelName;
            set {
                if(_modelName != value) {
                    if(loader == null || !loader.IsAlive) {
                        _modelName = value;
                        loader = new Thread(loadNewModel);
                        loader.Start();
                    }
                }
            }
        }

        private static void loadNewModel() {
            MelonLogger.Msg("Loading new model...");
            Loading = true;
            Vosk_model = null;

            try {
                if(Directory.Exists(model_directory + _modelName)) {
                    Vosk_model = new Model(model_directory + _modelName);
                } else {
                    MelonLogger.Warning("Directory doesn't exist " + model_directory + _modelName);
                }
            } catch(Exception e) {
                // failed
                MelonLogger.Warning("Failed to load model " + model_directory + _modelName + " : " + e.ToString());
                Vosk_model = null;
            } finally {
                ModelChanged?.DelegateSafeInvoke(Vosk_model);
                Loading = false;
            }
        }

        public static float TextScale {
            get => _textScale;
            set {
                if(_textScale != value) TextScaleChanging?.DelegateSafeInvoke(value);
                _textScale = value;
            }
        }

        public static string GetModelPath() {
            return model_directory + ModelName;
        }

        public static bool ModelExists() {
            return Directory.Exists(GetModelPath());
        }

    }

    class SettingsMenuContents {
        static ToggleButton killswitch;
        static ToggleButton range_transcribe;

        static Label model_info;

        static SubMenu submenu;
   

        
        public static void Init(SubMenu sub) {
            submenu = sub;

            killswitch = new ToggleButton(submenu.gameObject, new Vector3(0, 0), "Killswitch", "Off", KillChanged,
                "Killswitch is disabled. Live captions will continue to work.",
                "Killswitch is enabled. Live captioning has been terminated.",
                "killswitchtoggle",
                Settings.Disabled);

            range_transcribe = new ToggleButton(submenu.gameObject, new Vector3(1, 0), "Range auto-transcribe", "Disabled", RangeChanged,
                "Enable this option if you want close players to be automatically transcribed, without needing to manually enable each player. This may consume a lot of memory.",
                "Players close to you will be automatically transcribed.",
                "rangetranscribetoggle",
                Settings.AutoTranscribeWhenInRange);

            // TODO: increase/decrease size buttons

            model_info = new Label(submenu.gameObject, new Vector3(2, 0), "", "model_info");

            Settings.ModelChanged += (model) => {
                Update();
            };
        }

        private static void RangeChanged(bool to) {
            Settings.AutoTranscribeWhenInRange = to;
            Update();
        }

        private static void KillChanged(bool to) {
            Settings.Disabled = to;
            Update();
        }

        static List<GameObject> trash = new List<GameObject>();

        private static int PopulateModelButtons() {
            if(!Directory.Exists(Settings.model_directory)) return -1;
            string[] model_dirs = Directory.GetDirectories(Settings.model_directory);

            int currModel = 0;
            for(int y = 1; y < 3; y++) {
                for(int x = 0; x < 4; x++) {
                    if(currModel >= model_dirs.Length) break;
                    string currModelName = model_dirs[currModel].Replace(Settings.model_directory, "");
                    SingleButton button = new SingleButton(
                        submenu.gameObject,
                        new Vector3(x+1, y),
                        currModelName,
                        () => {
                            Settings.ModelName = currModelName;
                            Update();
                        },
                        "Set active model to " + model_dirs[currModel],
                        "modelselector"
                    );

                    GameObject.DontDestroyOnLoad(button.gameObject);

                    trash.Add(button.gameObject);
                    currModel++;
                }
            }

            return model_dirs.Length;
        }

        public static void Update() {
            foreach(GameObject garbage in trash) {
                GameObject.Destroy(garbage);
            }

            killswitch.State = Settings.Disabled;
            range_transcribe.State = Settings.AutoTranscribeWhenInRange;

            string modelInfoTxt = "";


            if(!Settings.ModelExists()) {
                modelInfoTxt = "Couldn't find model " + Settings.GetModelPath();
            } else {
                if(Settings.Vosk_model == null) {
                    if(Settings.Loading) {
                        modelInfoTxt = "Loading model " + Settings.GetModelPath();
                    } else {
                        modelInfoTxt = "Failed to load model " + Settings.GetModelPath();
                    }
                } else {
                    modelInfoTxt = "Using model " + Settings.ModelName;
                }
            }

            int models = PopulateModelButtons();

            if(models == -1) {
                modelInfoTxt = "Model directory " + Settings.model_directory + " doesn't exist!";
            }else if(models == 0) {
                modelInfoTxt = "No models found in " + Settings.model_directory + ", please add some.";
            }

            model_info.TextComponent.text = modelInfoTxt;
        }
    }

    class SettingsTabMenu {
        private static SubMenu menu;
        private static TabButton tabButton;
        

        public static void Init() {
            // Create menu and tab button
            menu = new SubMenu("UserInterface/QuickMenu", "VRCTranscribeOptions");
            
            Sprite liveCaptionSprite = GetSpriteFromResource(Properties.Resources.LiveCaptionIcon);
            tabButton = new TabButton(liveCaptionSprite, menu, () => {
                OnTabButtonClick();
            });

            tabButton.gameObject.GetComponent<UiTooltip>().field_Public_String_0 = "Live Transcribe settings";

            // Populate
            SettingsMenuContents.Init(menu);
        }

        public static void OnTabButtonClick() {
            // TODO: update state of everything
            SettingsMenuContents.Update();
            tabButton.OpenTabMenu();
        }


        public static Sprite GetSpriteFromResource(System.Drawing.Bitmap resource) {
            MelonLogger.Msg("Start get ssprite " + resource.ToString());
            MemoryStream ms = new MemoryStream();
            resource.Save(ms, resource.RawFormat);

            Texture2D tex = new Texture2D(resource.Width, resource.Height);
            ImageConversion.LoadImage(tex, ms.ToArray());

            MelonLogger.Msg("Call create");
            Sprite sprite = SpriteHelper.CreateSprite(tex, new Rect(0.0f, 0.0f, tex.width*1.0f, tex.height*1.0f), new Vector2(0.5f, 0.5f), 100.0f, 0, Vector4.zero);
            MelonLogger.Msg("End get ssprite");
            return sprite;
        }
    }
}
