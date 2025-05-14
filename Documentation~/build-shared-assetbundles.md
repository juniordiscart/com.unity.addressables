# Shared AssetBundles

In addition to the bundles created from your `AddressableAssetGroups`, a build can produce specialized bundles called shared AssetBundles. These are the `unitybuiltinassets` AssetBundle and the `MonoScript` AssetBundle.

## Built in AssetBundle

Unity generates `unitybuiltinassets` with assets required by the player and any built-in shaders used by assets included in the build. All Addressable assets that reference a built-in shader, such as the Standard Shader, do so by referencing this specialized shader AssetBundle.

You can change the naming method of the built-in shader bundle with the __Built In Bundle Naming Prefix__ option in [Addressables Build settings](xref:addressables-asset-settings).

## MonoScript AssetBundle

To enable or disable the `MonoScript` AssetBundle change the __MonoScript Bundle Naming Prefix__ option in [Addressables Build settings](xref:addressables-asset-settings). The `MonoScript` bundle has naming options listed here, which are typically used in multi-project situations. It is used to build `MonoScript` behaviors into AssetBundles that can be referenced as a dependency.

Shared AssetBundles derive their build options from the default `AddressableAssetGroup`. By default this group is named **Default Local Group (Default)** and uses local build and load paths. In this case the shared bundles cannot be updated as part of a Content Update, and can only be changed in a new player build.

The Check for Content Update Restrictions tool fails to detect the changes to the bundle because it is only generated during the content build. Therefore, if you plan on making content changes to the shared bundles in the future, set the default group to use remote build and load paths and set its [Update Restriction](xref:addressables-content-update-builds) to **Can Change Post Release**.
