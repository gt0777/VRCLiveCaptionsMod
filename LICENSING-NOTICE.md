# VRC-Mods [LGPL v3]
VRC-Mods repository: https://github.com/loukylor/VRC-Mods
VRC-Mods license: LGPL v3 
VRC-Mods is Copyright (c) 2021 loukylor.

## Files
The following files/folders contain code taken from the VRC-Mods repository, and are hence subject to the VRC-Mods license:
* `LiveCaptions/GameSpecific/VRChat/Components`
* `LiveCaptions/GameSpecific/VRChat/Ui`
* `LiveCaptions/GameSpecific/VRChat/Utilities`
* `LiveCaptions/Utils.cs`

## Changes
The following modifications were made:
* The DelegateSafeInvoke method was taken out of `LiveCaptions/GameSpecific/VRChat/Utilities/Extensions.cs`, and put into `LiveCaptions/Utils.cs` for easier access.
 
# UnityExplorer [GPL v3]
UnityExplorer repository: https://github.com/sinai-dev/UnityExplorer
UnityExplorer license: GPL v3
UnityExplorer is Copyright (c) 2021 sinai-dev

## Files
The following file contains code taken from the UnityExplorer repository, and is hence subject to the UnityExplorer license:
* `LiveCaptions/GameSpecific/VRChat/SpriteHelper.cs`

## Changes
The following changes were made:
* Methods for creating a sprite were copied out of `src/Core/Runtime/Il2Cpp/Il2CppTextureUtil.cs` into `LiveCaptions/GameSpecific/VRChat/SpriteHelper.cs` for easier access.


# Vosk-API [Apache-2.0]
Vosk-API repository: https://github.com/alphacep/vosk-api
Vosk-API license: Apache-2.0 License
Vosk-API is Copyright (c) 2021 Alpha Cephei Inc

## Files
All files under the `LiveCaptions/VoskSpecific/Vosk` folder have been taken from the Vosk-API repository and are hence subject to its license.


# badwords [MIT]
badwords repository: https://github.com/MauriceButler/badwords
badwords license: MIT
badwords is Copyright (c) 2013 Maurice Butler

## Files
The following file contains code from the badwords repository: `LiveCaptions/TranscriptData/profanities/Profanities.cs`

# VRC Live Captions Mod
## Files
All of the following listed files are subject to the VRC Live Captions license:

```
├───LiveCaptions
│   │   SessionPool.cs
│   │   Settings.cs
│   │   SubtitleUi.cs
│   │   TranscribeWorker.cs
│   │   TranscriptPlayerOverrides.cs
│   │   TranscriptSession.cs
│   │   TranscriptSessionDebugger.cs
│   │
│   ├───Abstract
│   │       IGameProvider.cs
│   │       IVoiceRecognizer.cs
│   │
│   ├───assets
│   │       livecaptionicon.png
│   │
│   ├───GameSpecific
│   │   │   GameUtils.cs
│   │   │
│   │   └───VRChat
│   │       │   SettingsTabMenu.cs
│   │       │   TranscriptPlayerUi.cs
│   │       │   USpeakHooker.cs
│   │       │   VRCTranscriptMod.cs
│   │       │
│   │
│   ├───TranscriptData
│   │   │   AudioBuffer.cs
│   │   │   ProfanityFilter.cs
│   │   │   Saying.cs
│   │   │   TextGenerator.cs
│   │   │
│   │   └───profanities
│   │           Slurs.cs
│   │
│   └───VoskSpecific
│       │   VoskVoiceRecognizer.cs
```