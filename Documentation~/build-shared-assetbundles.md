# Shared AssetBundles

A build can produce specialized AssetBundles called shared AssetBundles. These are the `unitybuiltinassets` AssetBundle and the `MonoScript` AssetBundle.

## Built-in AssetBundles

Unity generates `unitybuiltinassets` with assets required by the Player and any built-in shaders used by assets included in the build. All Addressable assets that reference a built-in shader, such as the Standard Shader, do so by referencing this specialized shader AssetBundle.

You can change the naming method of the built-in shader AssetBundle with the __Built In Bundle Naming Prefix__ option in [Addressables Build settings](AddressableAssetSettings.md#build).

## MonoScript AssetBundle

To enable or disable the `MonoScript` AssetBundle change the __MonoScript Bundle Naming Prefix__ option in [Addressables Build settings](AddressableAssetSettings.md#build). The `MonoScript` AssetBundle has naming options which are typically used in multi-project situations to build `MonoScript` behaviors into AssetBundles that can be referenced as a dependency.

Shared AssetBundles derive their build options from the default [group](groups-intro.md). By default this group is named **Default Local Group (Default)** and uses local build and load paths. In this case the shared AssetBundles can't be updated as part of a [content update](builds-update-build.md), and can only be changed in a new Player build.

The Check for Content Update Restrictions tool fails to detect the changes to the AssetBundle because it's only generated during the content build. If you plan on making content changes to the shared AssetBundles, set the default group to use remote build and load paths and enable [Prevent Updates](ContentPackingAndLoadingSchema.md#content-update-restriction).

## Additional resources

* [Group Inspector settings reference](ContentPackingAndLoadingSchema.md)
* [Addressables Asset Settings reference](AddressableAssetSettings.md#build)