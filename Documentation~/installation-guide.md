# Install Addressables

To install the Addressables package, open the Package Manager window (**Window** > **Package Management** > **Package Manager**) and perform one of the following options:

* [Install it from the registry](xref:um-upm-ui-install)
* [Add the package by its name](xref:um-upm-ui-quick) (`com.unity.addressables`)

## Set up the Addressables system

The Addressables system needs a folder for its settings, which you can automatically create when you open the Addressables Groups window for the first time. To create a folder for the Addressables settings in your project, perform the following steps:

1. Open the [Addressables Groups window](GroupsWindow.md) (**Window** > **Asset Management** > **Addressables** > **Groups**).
1. Select **Create Addressables Settings**, or drag an asset into the window.

When you run the __Create Addressables Settings__ command, the Addressables system creates a folder called, `AddressableAssetsData`, in which it stores settings files and assets it uses to keep track of your Addressables setup. Add the files in this folder to your source control system.

Addressables can create additional files when you change your Addressables configuration. For more information, refer to [Addressable Asset Settings reference](AddressableAssetSettings.md).

![The Addressables Groups window, and the Project window after selecting **Create Addressables Settings**. The Project window contains an `AddressableAssetsData` folder in the `Assets` folder.](images/install-settings.png)<br/>*The Addressables Groups window, and the Project window after selecting **Create Addressables Settings**. The Project window contains an `AddressableAssetsData` folder in the `Assets` folder.*

## Additional resources

* [Convert existing projects to Addressables](convert-existing-projects.md)
* [Create and organize Addressable assets introduction](organize-addressable-assets.md)
* [Addressables Groups window reference](GroupsWindow.md)
