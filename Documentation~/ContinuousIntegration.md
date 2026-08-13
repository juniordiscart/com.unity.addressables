---
uid: addressables-ci
---

# Build with continuous integration

You can integrate Addressables into your continuous integration (CI) to perform content builds across different environments and team members. When integrating Addressables in CI, consider the following:

* [Content builder configuration](#configure-custom-content-builders): Set up the correct builder for the CI process.
* [Cache management](#clean-the-addressables-content-builder-cache): Clear cached data to prevent build inconsistencies.
* [Pipeline optimization](#clean-the-scriptable-build-pipeline-cache): Manage the Scriptable Build Pipeline cache for clean builds.

## Configure custom content builders

Addressables uses content builders to process and package your project's assets. The system defaults to `BuildScriptPackedMode` when you call [`AddressableAssetSettings.BuildPlayerContent`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent). This method automatically uses the [`ActivePlayerDataBuilder`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ActivePlayerDataBuilder) setting and executes that builder's `BuildDataImplementation`.

### Setting up custom builders for continuous integration

If you have implemented a custom [`IDataBuilder`](xref:UnityEditor.AddressableAssets.Build.IDataBuilder) for CI builds, you need to specify which builder to use:

1. Set the [`ActivePlayerDataBuilderIndex`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.ActivePlayerDataBuilderIndex) property on the [`AddressableAssetSettings`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings) instance.
2. Access the settings through [`AddressableAssetSettingsDefaultObject.Settings`](xref:UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings).
3. Use the index that corresponds to thr custom builder's position in the [`DataBuilders`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.DataBuilders) list.

The following code example demonstrates how to configure a custom builder:

[!code-cs[sample](../Tests/Editor/DocExampleCode/CustomDataBuilder.cs#doc_SetCustomBuilder)]

## Clean the Addressables content builder cache

Cache cleaning prevents CI builds from using outdated files from previous builds, which can cause inconsistent or incorrect build results.

Each [`IDataBuilder`](xref:UnityEditor.AddressableAssets.Build.IDataBuilder) implementation includes a [`ClearCachedData`](xref:UnityEditor.AddressableAssets.Build.IDataBuilder.ClearCachedData) method that removes files created by that specific builder. For the default `BuildScriptPackedMode`, this includes:

- Content catalog files.
- Serialized settings files.
- Built AssetBundles.
- Generated link.xml files.

Call `IDataBuilder.ClearCachedData` as part of your CI process to ensure clean builds that don't rely on artifacts from previous runs.

## Clean the scriptable build pipeline cache

The Scriptable Build Pipeline (SBP) creates a build cache in the `Library/BuildCache` folder to optimize subsequent builds. This cache contains:

- `.info` files with build metadata.
- Hash maps for tracking asset changes.
- Type database information.

While this cache speeds up development builds by reusing unchanged data, it can cause issues in CI environments where you need completely clean builds.

Call [`BuildCache.PurgeCache(false)`](xref:UnityEditor.Build.Pipeline.Utilities.BuildCache.PurgeCache*) in your build scripts to clear the SBP cache. The `false` parameter skips the confirmation dialog.

## Platform considerations

When building for multiple platforms in CI, restart Unity for each platform. This ensures that Unity completes script compilation for each target platform before executing build methods via `-executeMethod`.

Platform switches can trigger domain reloads and script recompilation. If you don't wait for these processes to complete, your build methods might execute with the wrong platform settings or incomplete compilation.

For more information, refer to the Unity User Manual documentation on [Command line arguments](https://docs.unity3d.com/Manual/CommandLineArguments.html).

## Additional resources

* [Command line arguments](https://docs.unity3d.com/Manual/CommandLineArguments.html)
* [Build output](build-output.md)
* [`IDataBuilder` API reference](xref:UnityEditor.AddressableAssets.Build.IDataBuilder)