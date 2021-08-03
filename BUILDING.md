# Building

This file is for building the DLL, so you can skip this section if you already have it.

Building this requires Visual Studio with SDK and targeting pack for .NET Framework 4.7.2

Install VRChat and MelonLoader 0.4.3, and wait for it to complete the first-time launch.

Having completed first-time launch, clone this repository RECURSIVELY:

`git clone --recursive https://github.com/gt0777/VRCLiveCaptionsMod.git`


If you forgot to clone it recursively, initialize the submodules by doing the following:

`git submodule update --init --recursive`


After this, open the Visual Studio project and add the missing references:
 * MelonLoader is located at: `C:\Program Files (x86)\Steam\steamapps\common\VRChat\MelonLoader\MelonLoader.dll`
 * Other assemblies are located in this folder: `C:\Program Files (x86)\Steam\steamapps\common\VRChat\MelonLoader\Managed`

Once you've added all of the missing references, switch to Release configuration and x64 platform and build.

The build should succeed and you should now have `VRCLiveCaptionsMod.dll` in your x64 release binaries folder.
