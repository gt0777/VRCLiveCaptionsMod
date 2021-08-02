# VRC Live Captions Mod

This mod adds live captions to VRChat voice chat. This is an accessibility tool, intended to help those who are hard of hearing or deaf, but it may also be useful for other people who simply prefer seeing subtitles.

![Demonstration screenshot](https://i.imgur.com/4euvS07.png)

## Credits

This mod was made possible by:
 * [MelonLoader](https://melonwiki.xyz), for providing a mod API
 * [VRChatUtilityKit](https://github.com/loukylor/VRC-Mods), for providing a bunch of useful VRChat utilities when creating a mod
 * [Vosk](https://alphacephei.com/vosk/), the main speech recognition toolkit that works in real-time and provides small models
 * [badwords](https://github.com/web-mech/badwords), a list of English bad words

## Building

This section is for building the DLL, so you can skip this section if you already have it.

Building this requires Visual Studio with SDK and targeting pack for .NET Framework 4.7.2

Install VRChat and MelonLoader 0.4.3, and wait for it to complete the first-time launch.

After this, open the Visual Studio project and add the missing references:
 * MelonLoader is located at: `C:\Program Files (x86)\Steam\steamapps\common\VRChat\MelonLoader\MelonLoader.dll`
 * Other assemblies are located in this folder: `C:\Program Files (x86)\Steam\steamapps\common\VRChat\MelonLoader\Managed`

Once you've added all of the missing references, switch to Release configuration and x64 platform and build.

The build should succeed and you should now have `VRCLiveCaptionsMod.dll` in your x64 release binaries folder.

## Installation

Ensure MelonLoader 0.4.3 (or higher) is installed and drag `VRCLiveCaptionsMod.dll` to your VRChat Mods folder: `C:\Program Files (x86)\Steam\steamapps\common\VRChat\Mods` (or similar, depending on your VRChat game location).

If you're unsure of your VRChat game location, rightclick VRChat in Steam, then navigate to Manage > Browse local files.

You will need additional libraries for the mod to work. Download [libvosk](https://github.com/alphacep/vosk-api/releases/download/v0.3.30/vosk-win64-0.3.30.zip) and drag the 4 DLL files into the VRChat folder: `C:\Program Files (x86)\Steam\steamapps\common\VRChat` (in the same directory as the .exe)

![Screenshot of where to put the files](https://i.imgur.com/JcoPz5x.png)

The game should successfully launch now, and the live captions tab should be accessible within the Quick Menu.
* If there is no Live Captions tab button, you probably didn't install the mod properly. Check the log file for more details.
* If the Live Captions tab button is there, but clicking on it doesn't do anything, then it's likely failing to load the additional libvosk libraries. Ensure they're located in the same folder as VRChat.exe as seen in the screenshot above.


![Screenshot of the Live Captions quickmenu tab](https://i.imgur.com/yc1AyzA.png)

## Vosk model installation

By default, the english-light model (vosk-model-small-en-us-0.15, Apache 2.0) will be automatically downloaded and installed into the Models directory. If this is fine and you don't need any additional languages or different English models, you can skip this section.

The Models directory `C:\Program Files (x86)\Steam\steamapps\common\VRChat\Models\` (or similar, depending on your VRChat game location) gets automatically created upon first launch.

You can also download additional languages or different models from [here](https://alphacephei.com/vosk/models). It's recommended to use the small/lightweight models due to the high number of people and latency requirements.

Simply extract the folder inside of the .zip to the Models directory, and optionally rename it to something more friendly. Make sure the folder structure inside is similar to that of english-light, and that the folder isn't nested in another folder. Invalid folder structures or invalid models will cause the game to immediately crash when you try to switch to it, so be warned.

Your installed models will be listed in the VRChat quick menu under the Live Captions tab. You may click on one to switch to it.


## Usage

Use the quick menu and click on someone to whitelist them for live captioning specifically.

Otherwise, you can enable the Range option in the quick menu Live Captions tab to enable live captioning for everyone within a 6 meter radius of you. If you whitelist someone, the range extends to about 12 meters. 

The Range option is not recommended with large models.

The live caption accuracy may be worse at first when a new player has joined or when you've just enabled captioning, but it should get better over time as the person speaks more. The session gets reset after about 2 minutes of no activity (no speaking or out of range) to save on memory.

Most of the models have been trained on speech from places like audiobooks, so it will work best if the other person speaks clearly like they're an audiobook narrator and avoids making other kinds of noises (such as laughing, squeaking, playing music, etc)

The current models don't have any detection for laughing, applause, or music, so you may get a bunch of nonsense words in such cases.

VRChat may consume a significantly higher amount of memory with this mod enabled. You may need to close down other applications such as browsers to avoid hitting the pagefile, but this all depends on your system specifications. Try monitoring your memory usage when you start using this mod.

## Licensing

This project is open-source under the GPLv3 license. However, if you're from the VRChat team and would like to implement this into VRChat itself but can't do so without a more permissive license, please get in touch with me at:

![Contact](https://i.imgur.com/LDo9sNf.png)