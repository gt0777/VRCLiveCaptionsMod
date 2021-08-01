# VRC Live Captions Mod

This mod adds live captions to VRChat voice chat. This is an accessibility tool, intended to help those who are hard of hearing or deaf, but it may also be useful for other people who simply prefer seeing subtitles.

## Credits

This mod was made possible by:
 * [MelonLoader](https://melonwiki.xyz), for providing a mod API
 * [VRChatUtilityKit](https://github.com/loukylor/VRC-Mods), for providing a bunch of useful VRChat utilities when creating a mod
 * [Vosk](https://alphacephei.com/vosk/), the main speech recognition toolkit that works in real-time and provides small models
 * [badwords](https://github.com/web-mech/badwords), a list of English bad words

## Building

This section is for building the DLL, so you can skip this section if you already have it.

Building this requires Visual Studio with SDK and targeting pack for .NET Framework 4.7.2

Install VRChat and MelonLoader, and wait for it to complete the first-time launch.

After this, open the Visual Studio project and add the missing references:
 * MelonLoader is located at: `C:\Program Files (x86)\Steam\steamapps\common\VRChat\MelonLoader\MelonLoader.dll`
 * VRChatUtilityKit must be downloaded from https://github.com/loukylor/VRC-Mods/releases/download/VRCUK-1.0.3/VRChatUtilityKit.dll
 * Other assemblies are located in this folder: `C:\Program Files (x86)\Steam\steamapps\common\VRChat\MelonLoader\Managed`

Once you've added all of the missing references, switch to Release configuration and x64 platform and build.

The build should succeed and you should now have `VRCTranscriptMod.dll` in your x64 release binaries folder.

## Installation

Ensure MelonLoader is installed and drag `VRCLiveCaptionsMod.dll` to your VRChat Mods folder: `C:\Program Files (x86)\Steam\steamapps\common\VRChat\Mods`

VRChatUtilityKit is required for this mod to function. Download it from https://github.com/loukylor/VRC-Mods/releases/download/VRCUK-1.0.3/VRChatUtilityKit.dll and add it to your Mods folder.

You will need additional libraries for the mod to work. Download libvosk from https://github.com/alphacep/vosk-api/releases/download/v0.3.30/vosk-win64-0.3.30.zip and drag the 4 DLL files into the VRChat folder: `C:\Program Files (x86)\Steam\steamapps\common\VRChat`

The game should successfully launch now, but live captions will not yet function as you need to install a Vosk voice recognition model.

## Vosk model installation

Create a folder `C:\models`

You can find a list of various models at https://alphacephei.com/vosk/models

It's recommended to use the small/lightweight models (for example, vosk-model-small-en-us-0.15) due to the high number of people and latency requirements.

Once you download a model, extract it to `C:\models` with a friendly name. The folder structure must look something like this (in this example, I have 2 models, one called english-large and another called english-light):

```
C:\
└───models
    ├───english-large
    │   ├───am
    │   ├───conf
    │   ├───graph
    │   │   └───phones
    │   ├───ivector
    │   ├───rescore
    │   └───rnnlm
    └───english-light
        ├───am
        ├───conf
        ├───graph
        │   └───phones
        └───ivector
```

Your installed models will be listed in the VRChat quick menu under the Live Captions tab. You may click on one to switch to it. Currently, the loading indicator is broken so you'll need to repeatedly click on the Live Captions tab button to update the status text to figure out when it has loaded (it'll say "Using model modelname")


## Usage

Use the quick menu and click on someone to whitelist them for live captioning specifically.

Otherwise, you can enable the Range option in the quick menu Live Captions tab to enable live captioning for everyone within a 6 meter radius of you. If you whitelist someone, the range extends to about 12 meters. 

The Range option is not recommended with large models.

The live caption accuracy may be worse at first when a new player has joined or when you've just enabled captioning, but it should get better over time as the person speaks more. The session gets reset after about 2 minutes of no activity (no speaking or out of range) to save on memory.

Most of the models have been trained on speech from places like audiobooks, so it will work best if the other person speaks clearly like they're an audiobook narrator and avoids making other kinds of noises (such as laughing, squeaking, playing music, etc)

The current models don't have any detection for laughing, applause, or music, so you may get a bunch of nonsense words in such cases.

VRChat may consume a significantly higher amount of memory with this mod enabled. You may need to close down other applications such as browsers to avoid hitting the pagefile, but this all depends on your system specifications. Try monitoring your memory usage when you start using this mod.