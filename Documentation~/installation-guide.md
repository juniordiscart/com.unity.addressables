# Install Addressables

To install the Addressables package in your project, use the [**Package Manager**](xref:Packages):

1. Open the Package Manager (menu: __Window > Package Manager__).
1. Set the package list to display packages from the __Unity Registry__.
1. Select the Addressables package in the list.
1. Click __Install__ (at the bottom, right-hand side of the Package Manager window).

To set up the Addressables system in your Project after installation, open the __Addressables Groups__ window and click __Create Addressables Settings__. 

![](images/install-settings.png)<br/>*The **Addressables Groups window** before initializing the Addressables system in a Project*

When you select __Create Addressables Settings__, the Addressables system creates a folder called `AddressableAssetsData` to store settings files and other assets the package uses to keep track of your Addressables setup. Add the files in this folder to your version control system. The Addressables package can create additional files as you change your Addressables configuration. For more information, refer to [Addressables Settings](AddressableAssetSettings.md).

> [!NOTE]
> For instructions on installing a specific version of Addressables or for general information about managing the packages in a Project, refer to [Packages](xref:PackagesList).
