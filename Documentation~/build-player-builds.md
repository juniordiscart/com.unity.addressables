# Build Addressable assets with a Player build

If you want to build your project's Addressable content as part of the Player build, use the __Build__ or __Build and Run__ options in the [Build Profiles](xref:um-build-settings) window to start a build.

To specify whether to include Addressable assets, enable the __Build Addressables on Player Build__ setting in the Project [Addressables Asset Settings reference](AddressableAssetSettings.md#build). You can choose the appropriate option for each project, or use the [global Preferences setting](addressables-preferences.md). When you set a project-level setting, it applies to all contributors who build the project. The Preferences setting applies to all Unity Projects that don't set a specific value.

Building Addressables content together with the Player can be convenient, but increases build time, especially on large projects. This is because Unity rebuilds the Addressables content even when you haven't modified any assets. If you don't change Addressables content between most builds, consider disabling this option, and build Addressables assets in a [content-only build](builds-full-build.md).

> [!NOTE]
> Building Addressables on Player Build requires Unity 2021.2+. In earlier versions of Unity, you must build Addressables content as a separate step.