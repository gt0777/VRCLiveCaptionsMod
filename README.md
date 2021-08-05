***

<h1 align="center">
VRC Live Captions Mod
</h1>

<p align="center">
accessibility mod for VRChat to display live captions when people speak
</p>

***

## Info

This is an accessibility tool, intended to help those who are hard of hearing or deaf, but it may also be useful for other people who simply prefer seeing subtitles.

![Demonstration screenshot](https://i.imgur.com/4euvS07.png)

## Privacy

This mod uses the open-source Vosk and its offline models to process speech to text. This is done completely locally on your PC, meaning your conversations stay private and don't get logged (by this mod at least. I have no idea what VRChat servers or the proprietary Oculus Lipsync library does with peoples' voice)

## Credits

This mod was made possible by:
 * [MelonLoader](https://melonwiki.xyz), for providing a mod API
 * [VRChatUtilityKit](https://github.com/loukylor/VRC-Mods), for providing a bunch of useful VRChat utilities when creating a mod
 * [Vosk](https://alphacephei.com/vosk/), the main speech recognition toolkit that works in real-time and provides small models
 * [badwords](https://github.com/web-mech/badwords), a list of English bad words

## Installation


Ensure MelonLoader 0.4.3 (or higher) is installed.

Download the latest GitHub release .dll file from [here](https://github.com/gt0777/VRCLiveCaptionsMod/releases), you may need to expand the Assets section to find the dll.

Drag `VRCLiveCaptionsMod.dll` to your VRChat Mods folder: `C:\Program Files (x86)\Steam\steamapps\common\VRChat\Mods` (or similar, depending on your VRChat game location).

If you're unsure of your VRChat game location, rightclick VRChat in Steam, then navigate to Manage > Browse local files.

On first launch, the mod should automatically download some dependencies for you. Once you're in-game, you should see a new tab in your quickmenu:

![Screenshot of the Live Captions quickmenu tab](https://i.imgur.com/yc1AyzA.png)

## Vosk model installation

By default, the lightweight english-light model (vosk-model-small-en-us-0.15, Apache 2.0) will be automatically downloaded and installed into the Models directory. If this is fine and you don't need any additional languages or different English models, you can skip this section.

The Models directory `C:\Program Files (x86)\Steam\steamapps\common\VRChat\Models\` (or similar, depending on your VRChat game location) gets automatically created upon first launch.

You can also download additional languages or different models from [here](https://alphacephei.com/vosk/models). It's recommended to use the small/lightweight models due to the high number of people and latency requirements.

Simply extract the folder inside of the .zip to the Models directory, and optionally rename it to something more friendly. Make sure the folder structure inside is similar to that of english-light, and that the folder isn't nested in another folder. Invalid folder structures or invalid models will cause the game to immediately crash when you try to switch to it, so be warned.

Your installed models will be listed in the VRChat quick menu under the Live Captions tab. You may click on one to switch to it.


## Usage

You can enable the Range option in the quick menu Live Captions tab to enable live captioning for everyone within a 6 meter radius of you. This may be completely unusable with large models.

In the user details quickmenu, you should also see a new option for enabling high-priority captioning. You can enable this per-user and this will prioritize their captioning over others.

## Performance

There should be little to no performance hit when using lightweight models. However, you may suffer a ~300ms hiccup when a new player starts talking.

When using a lightweight model, the captions should be pretty much in sync with the voice. In some cases, it may even be ahead of the voice due to the weird way VRChat audio works.

## Accuracy

The live caption accuracy may be worse at first when a new player has joined or when you've just enabled captioning, but it should get better over time as the person speaks more. The session gets reset after about 2 minutes of no activity (no speaking or out of range) to save on memory, or 10 minutes if they're high-priority.

Most of the models have been trained on speech from places like audiobooks, so it will work best if the other person speaks clearly like they're an audiobook narrator and avoids making other kinds of noises (such as laughing, squeaking, playing music, etc). It works better with some voices and accents over others.

The current models don't have any detection for laughing, applause, music, or foreign languages, so you may get a bunch of nonsense words in such cases.

Sadly, it can often mistake words that sound similar (for example, "text to speech" can suddenly become "texas beach"), so don't overly rely on the captions! Especially if the live caption is vulgar or offensive - it may have just misunderstood what was being said!

## Issues

If your PC can't keep up with the number of players or using a heavy model, you will get messages like this in your MelonLoader console.

![Buffer overflow example](https://i.imgur.com/JLVTdvU.png)

In this case, the captions will likely be inaccurate or slow. You may need to turn off the Range option and manually prioritize people with whom you're talking.

If you're using a heavier model, you may want to switch to a more lightweight model.


## Community

Join the Discord server:

[Discord](https://discord.gg/FDGKxVFFB3)


## Licensing

This project is open-source under the GPLv3 license. However, if you're from the VRChat team and would like to implement this into VRChat itself but can't do so without a more permissive license, please get in touch with me at:

![Contact](https://i.imgur.com/LDo9sNf.png)