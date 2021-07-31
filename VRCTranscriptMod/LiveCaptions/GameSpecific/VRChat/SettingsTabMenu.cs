using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRChatUtilityKit.Ui;
using System.Runtime.InteropServices;
using VRCLiveCaptionsMod.LiveCaptions.TranscriptData;

namespace VRCLiveCaptionsMod.LiveCaptions.GameSpecific {
    // Taken from UnityExplorer
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

    // Taken from UnityExplorer
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

    class SettingsMenuContents {
        static ToggleButton killswitch;
        static ToggleButton range_transcribe;

        static ToggleButton filter_words;


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

            filter_words = new ToggleButton(submenu.gameObject, new Vector3(2, 0), "Swear filter", "Disabled", 
                
                (state) => {
                    Settings.ProfanityFilterLevel = state ? ProfanityFilter.FilterLevel.ALL : ProfanityFilter.FilterLevel.NONE;
                },
                "Enable this option if you want close players to be automatically transcribed, without needing to manually enable each player. This may consume a lot of memory.",
                "Players close to you will be automatically transcribed.",
                "rangetranscribetoggle",
                Settings.ProfanityFilterLevel == ProfanityFilter.FilterLevel.ALL);


            // TODO: increase/decrease size buttons

            model_info = new Label(submenu.gameObject, new Vector3(3, 0), "", "model_info");

            /*
            Settings.ModelChanged += (model) => {
                Update();
            };*/

            Settings.ModelName = "model";
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
            MemoryStream ms = new MemoryStream();
            resource.Save(ms, resource.RawFormat);

            Texture2D tex = new Texture2D(resource.Width, resource.Height);
            ImageConversion.LoadImage(tex, ms.ToArray());
            
            Sprite sprite = SpriteHelper.CreateSprite(tex, new Rect(0.0f, 0.0f, tex.width*1.0f, tex.height*1.0f), new Vector2(0.5f, 0.5f), 100.0f, 0, Vector4.zero);
            return sprite;
        }
    }
}
