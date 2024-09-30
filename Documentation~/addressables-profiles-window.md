# Addressables Profiles window reference

To manage the profiles in your project, use the [Addressables Profiles window](addressables-profiles-window.md) (menu: __Window > Asset Management > Addressables > Profiles__).

![](images/addressables-profiles-window.png)<br/>*The __Addressables Profiles__ window displaying the default profile.*

Right-click a profile name to set it as the active profile, rename the profile, or delete it.

The following default profile variables are available:

|**Profile**|**Description**|
|---|---|
| __Local__| Set the variables for local content. You can choose from:<br/><br/>- **Built-In**<br/>- **Cloud Content Delivery**<br/>- **Custom**|
|__Local.BuildPath__<br/><br/>Can only edit if you select **Custom** as the variable| Define where to build the files containing assets you want to install locally with your application. By default, this path is inside your Project Library folder.|
|__Local.LoadPath__<br/><br/>Can only edit if you select **Custom** as the variable| Define where to load assets installed locally with your application. By default, this path is in the StreamingAssets folder. Addressables automatically includes local content built to the default location in StreamingAssets when you build a Player, but not from other locations.|
|__Remote__| Set the variables for remote content. You can choose from:<br/><br/>- **Built-In**<br/>- **Cloud Content Delivery**<br/>- **Custom**|  
|__Remote.BuildPath__<br/><br/>Can only edit if you select **Custom** as the variable| Define where to build the files containing assets you plan to distribute remotely.|
|__Remote.LoadPath__<br/><br/>Can only edit if you select **Custom** as the variable| Define the URL from which to download remote content and catalogs.|
|__BuildTarget__| Set the name of the build target, such as Android or StandaloneWindows64.|

> [!IMPORTANT]
> Usually, you shouldn't need to change the local build or load paths from their default values. If you do, you must manually copy the local build artifacts from your custom build location to the project's [StreamingAssets](xref:SpecialFolders) folder before making a Player build. Changing these paths also precludes building your Addressables as part of the Player build. 

Refer to [Builds](Builds.md) for more information about how Addressables uses profiles during content builds.

