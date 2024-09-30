# Create an update build

When you distribute content remotely, you can perform a differential update of the previously published build to minimize the amount of data your users must download (compared to a full build). 

Once you have configured your remote groups properly and have a previous build which contains remote content, you can perform a content update build by:

1. Open the [Groups window](GroupsWindow.md) (menu: __Windows > Asset Management > Addressables > Groups__).
2. Select the desired profile from the __Profile__ menu on the toolbar.
3. Select the __Update a Previous Build__ from the __Build__ menu.
4. Locate the `addressables_content_state.bin` file produced by the build you are updating. (The default location is in your `Assets/AddressableAssetsData/TargetPlatform` folder.)
5. Click __Open__ to start the update build.

To update existing clients, copy the updated remote content to your hosting service (after appropriate testing). (An update build does include all of your local and remote content -- any player builds you create after a content update build will contain a complete set of Addressable assets.) 

Updating a previous build does not change the `addressables_content_state.bin` file. Use the same version of the file for future update builds (until you publish another full build created from the __New Build__ menu). 

See [Content Update Builds](ContentUpdateWorkflow.md) for information on how and when to use content update builds.

## Minimize changes to bundles

Content bundles can be large, and having to update the whole bundle for small changes can result in a large amount of data being updated for a small change to the MonoScript.

Enabling the **MonoScript Bundle Naming Prefix** option in the [Addressables settings](xref:addressables-asset-settings) will build an AssetBundle that contains the MonoScript objects, separate to your serialized data.
If there are no changes to the serialized class data then only the MonoScript bundle will have changed and other bundles will not need to be updated.

## Changes to scripts that require rebuilding Addressables

Unity references classes in Addressables content using a MonoScript object. This object defines a class using the Assembly name, [Namespace](https://docs.unity3d.com/Manual/Namespaces.html), and either the class name or the referenced class.

When loading content at runtime, Unity uses the MonoScript to load and create an instance of the runtime class from the player assemblies.

Changes to MonoScripts need to be consistent between the Player and the built Addressables content. You must rebuild both the Player content and Addressables content to load the classes correctly.

The following actions can result in changes to the MonoScript data:
- Moving the script file to a location that comes under another [assembly definition file](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html)
- Changing the name of the assembly definition file containing the class
- Adding or Changing the class namespace
- Changing the class name
