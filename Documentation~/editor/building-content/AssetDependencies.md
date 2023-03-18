---
uid: addressables-asset-dependencies
---

# How Addressables interact with other project assets

When you include a scene in your Project Build Settings and build a player, Unity includes that scene and any assets used in the scene in your game or application's built-in data. Similarly, Unity includes any assets found in your project's Resources folders in a separate, built-in collection of assets. (The difference is that assets in a scene are only loaded as part of a scene, whereas assets in Resources can be loaded independently.) 

Addressable assets can either be built into your game or application as an additional set of "local" assets, or kept external to the game build as "remote" assets hosted on a server and downloaded when they are needed. You can update remote assets independently from the application itself (although remote assets cannot include code, so you can only change assets and serialized data).

![](../../images/addr_interact_baloons.png)

*How project assets are exported to a player build*

However, if you use the same asset in more than one of these categories, then Unity makes copies of the asset when building rather than sharing a single instance. For example, if you used a Material in a built-in scene and also used it in a Prefab located in a Resources folder, you would end up with two copies of that Material in your build -- even if the Material asset itself is not located in Resources. If you then marked that same Material as Addressable, you would end up with three copies. (Files in the project StreamingAssets folder can never be referenced by assets outside that folder.)

> [!NOTE]
> Before building a player, you must make a content build of your Addressable assets. During the player build, Unity copies your local Addressables to the StreamingAssets folder so that they are included in the build along with any assets you placed in StreamingAssets. (These assets are removed at the end of the build process.) It is your responsibility to upload the remote Addressables files produced by the content build to your hosting service. See [Builds] for more information.

When you use Addressables in a project, Unity recommends that you move your scenes and any data in Resources folders into Addressable groups and manage them as Addressables.

The Build Settings scene list must contain at least one scene. You can create a minimal scene that initializes your application or game. 

A small amount of data in Resources folders typically doesn't cause performance issues. If you use 3rd party packages that place assets there, you don't need to move them unless they cause problems. (Addressable assets cannot be stored in Resources folders.) 

## Referencing Subobjects
Unity partially determines what to include in a content build based on how your assets and scripts reference each other. Subobject references make the process more complicated.

If an `AssetReference` points to a subobject of an Asset that is Addressable, Unity builds the entire object into the `AssetBundle` at build time.  If the `AssetReference` points to an Addressable object (such as a `GameObject`, `ScriptableObject`, or `Scene`) which directly references a subobject, Unity only builds the subobject into the `AssetBundle` as an implicit dependency.

## Asset and AssetBundle dependencies

When you add an asset to an Addressables group, that asset is packed into an AssetBundle when you make a [content build]. In this case the asset is explicitly included in the bundle, or in other words it is an **explicit** asset. 

If an asset references other assets, then the referenced assets are dependencies of the original asset. This is known as an asset dependency. If the asset is packed into Assetbundle A and the referenced assets are packed into AssetBundle B, then bundle B is a dependency of bundle A. This is known as an AssetBundle dependency. See the [AssetBundle dependencies manual page] for more information.

Asset dependencies are treated depending on whether or not they are also Addressable. Dependencies that are Addressable are packed into AssetBundles according to the settings of the group they are in -- this could be the same bundle as the referencing asset or a different bundle. A dependency that is not Addressable is included in the bundle of its referencing asset. The referenced asset is implicitly included in the bundle, or in other words it is an **implicit** asset.

> [!TIP]
> Use the [Bundle Layout Preview] Analyze rule to view explicit and implicit assets that will be included in AssetBundles based on the contents of Addressables groups. This is useful when previewing assets before making an content build.
> Use the [Build Layout Report] tool to display more detailed information about AssetBundles produced by a content build.

If more than one Addressable references the same implicit asset, then copies of the implicit asset are included in each bundle containing a referencing Addressable.

![](../../images/addr_interact_shared.png)

*Non-Addressable assets are copied to each bundle with a referencing Addressable*

A subtle consequence that can occur when an implicit asset is included in more than one bundle, is that multiple instances of that asset can be instantiated at runtime rather than the single instance your game logic expects. If you change the instance state at runtime, only the object from the same bundle can see the change since all the other assets now have their own individual instance rather than sharing the common one. 

To eliminate this duplication, you can make the implicit asset an Addressable asset and include it in one of the existing bundles or add it to a different bundle. The bundle the asset is added to is loaded whenever you load one of the Addressables that reference it. If the Addressables are packed into a different AssetBundle than the referenced asset, then the bundle containing the referenced asset is an AssetBundle dependency.

Be aware that the dependent bundle must be loaded when you load ANY asset in the current bundle, not just the asset containing the reference. Although none of the assets in this other AssetBundle are loaded, loading a bundle has its own runtime cost. See [Memory implications of loading AssetBundle dependencies] for more information. 

> [!TIP]
> Use the [Check Duplicate Bundle Dependencies] Analyze rule to identify and resolve unwanted asset duplication resulting from your project content organization.

[AssetBundle dependencies manual page]: xref:AssetBundles-Dependencies
[Builds]: xref:addressables-builds
[Bundle Layout Preview]: xref:addressables-analyze-tool#unfixable-rules
[Build Layout Report]: xref:addressables-build-layout-report
[Check Duplicate Bundle Dependencies]: xref:addressables-analyze-tool#fixable-rules
[content build]: xref:addressables-builds
[Include In Build]: https://docs.unity3d.com/Manual/SpriteAtlasDistribution.html#Dontinclbuild
[Graphics Settings]: xref:class-GraphicsSettings
[Memory implications of loading AssetBundle dependencies]: xref:addressables-memory-management#memory-implications-of-loading-assetbundle-dependencies
[Mixed Lights]: xref:LightMode-Mixed
[Play Mode Script]: xref:addressables-groups-window#play-mode-scripts
[Sprite Packer Mode]: https://docs.unity3d.com/Manual/SpritePackerModes.html
[strips shaders variants]: xref:shader-variant-stripping
[Quality Settings]: xref:class-QualitySettings