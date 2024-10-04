---
uid: addressables-profile-variables
---

# Profile variables overview

Profile variables are generic key/value combinations that you can use to change Addressables configurations for different development situations.

There are two types of profile variables: 

* **Standard**: Standalone key/value pairs
* **Path pairs**: Uses a special naming convention to connect sets of variables together.

Path pairs are typically used to change between different build and load paths for different development situations. For example, you might use path pairs to change the build and load paths for your Addressable content for various platforms. 

## Add a new standard variable

You can add two kinds of variables to your profiles: 

* **Variable**: A basic variable, which defines a single value
* **Build and Load Path Variable**: A path pair, which defines a set of two path values. One value is for the build path and one is for the load path

To add a new Profile variable, open the [Addressables Profiles window](addressables-profiles-window.md), open the **Create** menu and select either **Variable** or **Build Load Path Variable**. Assign the new variable a name and value, then select **Save**. Addressables then adds the new variable to all profiles. Right-click on the variable name to rename or delete the variable.

You can use basic variables as components of your path values (for example, **BuildTarget**) and you can use them in your own build scripts. Use path pair variables to set the **Build & Local Paths** setting of your [groups](Groups.md) and remote catalog. 


### Path Pairs

Path pairs define a matched set of `BuildPath` and `LoadPath` variables. When you create a path pair, you can use the pair name to assign the path setting of a group or remote catalog as a unit. 

To create a path pair, go to **Create** and select **Build Load Path Variables**. Assign the path pair a prefix name and assign path strings to the individual fields. 

![](images/profiles-pairs.png)<br/>*A new path pair*

The new path pair uses the **Custom** setting for the **Bundle Location** property with your initial values. You can change to a different **Bundle Location** if needed.

> [!TIP]
> You can convert two regular variables for the build and load paths into a path pair by renaming them in the Profile window. Set one to `VariableName.BuildPath` and the other to `VariableName.LoadPath`.

![Path pairs grouped by a common prefix and separated by a period.](images/profiles-with-pairs.png)<br/>
_The **Addressables Profiles** window showing two profiles with two path pairs._

### Default path values

The default values for the build and load paths are:

* Local build path: `[UnityEditor.EditorUserBuildSettings.activeBuildTarget]`
* Local load path: `[UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]`
* Remote build path: `ServerData/[BuildTarget]`
* Remote load path: <undefined>

Usually, you shouldn't need to change the local path values. The Unity build system expects the AssetBundles and other files to exist in the default location. If you change the local paths, you must copy the files from the build path to the load path before making your Player build. The load path must always be within the Unity `StreamingAssets` folder.

If you distribute content remotely, you must change the remote load path to reflect the URL at which you host your remote content. You can set the remote build path to any convenient location: the build system doesn't rely on the default value. 

## Profile variable syntax

All Profile variables are of type `string`. You can assign them a fixed path or value. You can also use two syntax designations to derive all or part of a variable's value from static properties or other variables:

* __Brackets [ ]__:  Addressables evaluates entries surrounded by brackets at build time. The entries can be other profile variables such as `BuildTarget`, or code variables such as `UnityEditor.EditorUserBuildSettings.activeBuildTarget`. During a build, as Addressables processes your groups, it evaluates the strings inside brackets and writes the result into the catalog.
* __Braces { }__: Addressables evaluates entries surrounded by braces at runtime. You can use code variables of runtime classes, such as `{UnityEngine.AddressableAssets.Addressables.RuntimePath}`.

You can use static fields and properties inside either the brackets or braces. The names must be fully qualified and the types must be valid in context. For example, classes in the `UnityEditor` namespace can't be used at runtime.

The code variables used in the default Profile variable settings include:

* `[UnityEditor.EditorUserBuildSettings.activeBuildTarget]`
* `[UnityEngine.AddressableAssets.Addressables.BuildPath]`
* `[UnityEngine.AddressableAssets.Addressables.RuntimePath]`

For example, a load path of `{MyNamespace.MyClass.MyURL}/content/[BuildTarget]` is set on a group that creates an AssetBundle called `trees.bundle`. During the build, the catalog registers the load path for that bundle as `{MyNamespace.MyClass.MyURL}/content/Android/trees.bundle`, evaluates `[BuildTarget]` as `Android`, and adds the AssetBundle name to the path. At runtime as the Addressables system processes the catalog it evaluates `{MyNamespace.MyClass.MyURL}` to produce the final load path, `http://example.com/content/Android/trees.bundle`. 

> [!NOTE]
> Referencing a runtime variable in a Profile string doesn't prevent Unity from stripping that variable from your application's runtime libraries during the build optimization phase if nothing else in your code references the same variable.