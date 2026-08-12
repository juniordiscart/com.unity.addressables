# Create an update build

When you distribute content remotely, you can perform a differential update of the previous build to minimize the amount of data your users must download.

Once you have configured remote Addressables groups and have a previous build which contains remote content, you can perform a content update build. To create an update build:

1. Open the [Addressables Groups window](GroupsWindow.md) (menu: __Windows > Asset Management > Addressables > Groups__).
2. In the __Profile__ menu, select the desired [profile](profiles-create.md).
3. In the __Build__ menu, select __Update a Previous Build__.
4. By default, the Addressables system automatically uses the `addressables_content_state.bin` file in the `Assets/AddressableAssetsData/TargetPlatform` folder to create an update build.

To update existing clients, copy the updated remote content to your hosting service. An update build includes all local and remote content, and any Player builds you create after a content update build contain a complete set of Addressable assets.

Updating a previous build doesn't change the `addressables_content_state.bin` file. Use the same version of the file for future update builds, until you publish another full build created from the __New Build__ menu.

## Changes to scripts that require rebuilding Addressables

Unity references classes in Addressables content using a MonoScript object. This object defines a class using the Assembly name, [Namespace](https://docs.unity3d.com/Manual/Namespaces.html), and either the class name or the referenced class.

When loading content at runtime, Unity uses the MonoScript to load and create an instance of the runtime class from the player assemblies.

Changes to MonoScripts need to be consistent between the Player and the built Addressables content. You must rebuild both the Player content and Addressables content to load the classes correctly.

The following actions can result in changes to the MonoScript data:

- Moving the script file to a location that comes under another [assembly definition file](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html)
- Changing the name of the assembly definition file containing the class
- Adding or changing the class namespace
- Changing the class name

You can enable the **MonoScript Bundle Naming Prefix** option in the [Addressables settings](AddressableAssetSettings.md#build) to build an AssetBundle that contains only MonoScript objects, separate to your serialized data.

If there are no changes to the serialized class data, then you only need to update the MonoScript AssetBundle.

## Check for content update restrictions

You can use __Check for Content Update Restrictions__ command to prepare groups for a content update build. The tool examines the `addressables_content_state.bin` file and group settings. To run the command:

1. Open the __Addressables Groups__ window in the Unity Editor (__Window__ > __Asset Management__ > __Addressables__ > __Groups__).
2. In the __Tools__ menu, select __Check for Content Update Restrictions__.
3. If a group has the [__Prevent Updates__](ContentPackingAndLoadingSchema.md#content-update-restriction) setting enabled in the previous build, the tool gives you the option to move any changed assets to a new remote group. You can change the names of any new remote groups the tool created, but moving assets to different groups can have unintended consequences.

When you create the update build, the new catalog maps the changed assets to their new remote AssetBundles, while still mapping the unchanged assets to their original AssetBundles. Checking for content update restrictions doesn't check groups with __Prevent Updates__ disabled.

Unity uses the hash returned by [`AssetDatabase.GetAssetDependencyHash`](xref:UnityEditor.AssetDatabase.GetAssetDependencyHash(System.String)) to determine if an asset has changed. This Editor API has limitations and might not accurately reflect AssetBundle changes that are calculated at build time. For example it computes the hash of the content of `.cs` files. This means that performing whitespace changes in a `.cs` file results in a different hash, but the AssetBundle containing the file is unchanged. For more information, refer to [Changes to scripts that require rebuilding Addressables](#changes-to-scripts-that-require-rebuilding-addressables).


>[!IMPORTANT]
> Before you run __Check for Content Update Restrictions__, make a branch with your version control system. The tool rearranges asset groups in a way suited for updating content. Branching ensures that next time you ship a full player build, you can return to your preferred content arrangement.

## Additional resources

* [Addressables Groups window](GroupsWindow.md)
* [Create a script to check for content updates](content-update-builds-check.md)
* [Content update dependencies](content-update-examples.md)