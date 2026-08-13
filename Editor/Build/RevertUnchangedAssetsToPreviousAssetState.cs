using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using static UnityEditor.AddressableAssets.Build.ContentUpdateScript;

/// <summary>
/// RevertUnchangedAssetsToPreviousAssetState uses the asset state from the previous build to determine if any assets
/// need to use their previous settings or use the newly build data.
/// </summary>
public class RevertUnchangedAssetsToPreviousAssetState
{
    const string k_AssetBundleProviderId = "UnityEngine.ResourceManagement.ResourceProviders.AssetBundleProvider";

    internal struct AssetEntryRevertOperation
    {
        public CachedAssetState PreviousAssetState;
        public AddressableAssetEntry AssetEntry;
        public ContentCatalogDataEntry BundleCatalogEntry;
        public string CurrentBuildPath;
        public string PreviousBuildPath;
    }

    /// <summary>
    /// Reverts asset entries to their previous state if not modified by the new build.
    /// </summary>
    /// <param name="aaBuildContext">The new build data.</param>
    /// <param name="updateContext">The cached build data.</param>
    /// <returns>Returns the success ReturnCode if the content update succeeds.</returns>
    public static ReturnCode Run(IAddressableAssetsBuildContext aaBuildContext, ContentUpdateContext updateContext)
    {
        var aaContext = aaBuildContext as AddressableAssetsBuildContext;
        var groups = aaContext.Settings.groups.Where(group => group != null && group.HasSchema<BundledAssetGroupSchema>());
        if (updateContext.ContentState.cachedBundles == null)
            UnityEngine.Debug.LogWarning(
                $"ContentUpdateContext does not contain previous asset bundle info, remote static bundles that are updated will not be cacheable.  If this is needed, rebuild the shipped application state with the current version of addressables to update the addressables_content_state.bin file.  The updated addressables_content_state.bin file can be used to create the content update.");

        foreach (var assetGroup in groups)
        {
            List<AssetEntryRevertOperation> operations = DetermineRequiredAssetEntryUpdates(assetGroup, updateContext);
            if (operations != null && operations.Count > 0)
                ApplyAssetEntryUpdates(operations, updateContext);
        }

        var defaultContentUpdateSchema = aaContext.Settings.DefaultGroup.GetSchema<ContentUpdateGroupSchema>();
        if (defaultContentUpdateSchema == null)
        {
            Debug.LogWarning($"Default group {aaContext.Settings.DefaultGroup.Name} does not contain a {nameof(ContentUpdateGroupSchema)}, so we're unable to determine if " +
                             $"the built in shader bundle and monoscript bundles need to be reverting to their previous paths.  We will not revert the paths for these bundles in the catalog, " +
                             $"if that is not the desired behavior, please add a {nameof(ContentUpdateGroupSchema)} to the Default group and set the Prevent Updates toggle to the correct setting.");
        }
        else if (defaultContentUpdateSchema.StaticContent)
        {
            // cannot detect individual shader usage, so just assume that the shaders haven't changed, and just indeterminisn.
            if (!RevertBundleByNameContains(BuildScriptBase.BuiltInBundleBaseName, updateContext, aaContext))
                return ReturnCode.Error;
            // Scripts could have been added and fail, or removed and load fine, not enough information to know
            if (!RevertBundleByNameContains("_monoscripts", updateContext, aaContext))
                return ReturnCode.Error;
        }

        return ReturnCode.Success;
    }

    internal static bool RevertBundleByNameContains(string containingString, ContentUpdateContext updateContext, AddressableAssetsBuildContext aaContext)
    {
        bool isBuiltinBundleCase = string.Equals(containingString, BuildScriptBase.BuiltInBundleBaseName, StringComparison.Ordinal);

        CachedBundleState previousBundleCache = null;
        foreach (CachedBundleState cachedBundle in updateContext.ContentState.cachedBundles)
        {
            var options = cachedBundle.data as AssetBundleRequestOptions;
            if (options != null && AddressableAssetUtility.StringContains(options.BundleName, containingString, StringComparison.Ordinal))
            {
                previousBundleCache = cachedBundle;
                break;
            }
        }

        ContentCatalogDataEntry currentLocation = null;
        foreach (ContentCatalogDataEntry catalogEntry in aaContext.locations)
        {
            if (catalogEntry.Provider == k_AssetBundleProviderId)
            {
                var options = catalogEntry.Data as AssetBundleRequestOptions;
                if (options != null && AddressableAssetUtility.StringContains(options.BundleName, containingString, StringComparison.Ordinal))
                {
                    currentLocation = catalogEntry;
                    break;
                }
            }
        }

        if (previousBundleCache == null && currentLocation == null)
            return true; // bundle were not used in either build

        if (currentLocation == null)
            return true; // bundle not in update build but was in original is ok

        if (previousBundleCache == null && !isBuiltinBundleCase)
        {
            UnityEngine.Debug.LogError($"Matching cached update state for {currentLocation.InternalId} failed. Content not found in original build.");
            return false; // bundle was in update build, but not original. For monoscript builds, this is an automatic fail.
        }

        var currentOptions = currentLocation.Data as AssetBundleRequestOptions;
        var prevOptions = previousBundleCache != null ? previousBundleCache.data as AssetBundleRequestOptions : null;

        // Special case: builtin bundle only. If a remote group relies on its content changing, create a duplicate for remote and rewire only updated remote entries to it.
        if (isBuiltinBundleCase && currentOptions != null && (prevOptions == null || !string.Equals(currentOptions.Hash, prevOptions.Hash, StringComparison.Ordinal)))
        {
            Debug.LogWarning("A change was made in a remote group that required the unitybuiltinassets bundle to be rebuilt. " +
                   "Because the unitybuiltinassets bundle was built as part of a group set to \"Prevent Updates\", " +
                  $"Addressables will attempt to create a modified duplicate of the unitybuiltinassets bundle and place it into the first remote group in your project. " +
                  $"To avoid this behavior, consider modifying your shared bundle settings to point to a group that is not set to Prevent Updates.");
            bool success = CreateRemoteBuiltinDuplicateAndRewire(updateContext, aaContext, currentLocation, currentOptions, previousBundleCache);
            if (!success)
                return false;
        }

        if (prevOptions == null)
        {
            // There is a case where the unitybuiltinassets bundle can be set to a static location but not built during the first build (for example, if a user is using URP assets).
            // If, in a subsequent build, the user then adds an asset that relies on unitybuiltinassets  a remote group, we will still need to create a remote version of the unitybuiltinassets bundle.
            // Because in this case, there will be no previousBundleCache to revert to (since the bundle was not built in the original build), we can return here.
            return true;
        }

        currentLocation.InternalId = previousBundleCache.bundleFileId;
        currentOptions.Crc = prevOptions.Crc;
        currentOptions.Hash = prevOptions.Hash;
        currentOptions.BundleSize = prevOptions.BundleSize;
        currentOptions.BundleName = prevOptions.BundleName;

        return true;
    }

    internal static List<AssetEntryRevertOperation> DetermineRequiredAssetEntryUpdates(AddressableAssetGroup group, ContentUpdateScript.ContentUpdateContext contentUpdateContext)
    {
        if (!group.HasSchema<BundledAssetGroupSchema>())
            return new List<AssetEntryRevertOperation>();

        bool groupIsStaticContentGroup = group.HasSchema<ContentUpdateGroupSchema>() && group.GetSchema<ContentUpdateGroupSchema>().StaticContent;
        List<AssetEntryRevertOperation> operations = new List<AssetEntryRevertOperation>();

        List<AddressableAssetEntry> allEntries = new List<AddressableAssetEntry>();
        group.GatherAllAssets(allEntries, true, true, false);
        foreach (AddressableAssetEntry entry in allEntries)
        {
            if (entry.IsFolder)
                continue;
            GUID guid = new GUID(entry.guid);
            if (!contentUpdateContext.WriteData.AssetToFiles.ContainsKey(guid))
                continue;

            string file = contentUpdateContext.WriteData.AssetToFiles[guid][0];
            string fullInternalBundleName = contentUpdateContext.WriteData.FileToBundle[file];
            string finalBundleWritePath = contentUpdateContext.BundleToInternalBundleIdMap[fullInternalBundleName];

            //Ensure we can get the catalog entry for the bundle we're looking to replace
            if (!contentUpdateContext.IdToCatalogDataEntryMap.TryGetValue(finalBundleWritePath, out ContentCatalogDataEntry catalogBundleEntry))
                continue;

            //If new entries are added post initial build this will ensure that those new entries have their bundleFileId for SaveContentState
            entry.BundleFileId = catalogBundleEntry.InternalId;

            //If we have no cached state no reason to proceed.  This is new to the build.
            if (!contentUpdateContext.GuidToPreviousAssetStateMap.TryGetValue(entry.guid, out CachedAssetState previousAssetState))
                continue;

            //If the parent group is different we don't want to revert it to its previous state
            if (entry.parentGroup.Guid != previousAssetState.groupGuid)
                continue;

            var hashChanged = AssetDatabase.GetAssetDependencyHash(entry.AssetPath) != previousAssetState.asset.hash;
            //If the asset hash has changed and the group is not a static content update group we don't want to revert it to its previous state
            if (hashChanged && !groupIsStaticContentGroup)
                continue;

            //If the previous asset state has the same bundle file id as the current build we don't want to revert it to its previous state
            if (!hashChanged && catalogBundleEntry.InternalId == previousAssetState.bundleFileId)
                continue;

            var schema = group.GetSchema<BundledAssetGroupSchema>();
            string loadPath = schema.LoadPath.GetValue(group.Settings);
            string buildPath = schema.BuildPath.GetValue(group.Settings);

            //Need to check and make sure our cached version exists
            if (string.IsNullOrEmpty(previousAssetState.bundleFileId))
            {
                //Logging this as an error because a CachedAssetState without a set bundleFileId is indicative of a significant issue with the build script.
                Addressables.LogError($"CachedAssetState found for {entry.AssetPath} but the bundleFileId was never set on the previous build.");
                continue;
            }

            string previousBundlePath = BundleIdToBuildPath(previousAssetState.bundleFileId, loadPath, buildPath);
            if (!File.Exists(previousBundlePath) || hashChanged && groupIsStaticContentGroup)
            {
                //Logging this as a warning because users may choose to delete their bundles on disk which will trigger this state.
                Addressables.LogWarning($"CachedAssetState found for {entry.AssetPath} but the previous bundle at {previousBundlePath} cannot be found. " +
                                        $"This will not affect loading the bundle in previously built players, but loading the missing bundle in Play Mode using the play mode script " +
                                        $"\"Use Existing Build (requires built groups)\" will fail. This most often occurs because you are running a content update on a build where you " +
                                        $"made changes to a group marked with \"Prevent Updates\"");
            }

            string builtBundlePath = BundleIdToBuildPath(contentUpdateContext.BundleToInternalBundleIdMap[fullInternalBundleName], loadPath, buildPath);

            AssetEntryRevertOperation operation = new AssetEntryRevertOperation()
            {
                BundleCatalogEntry = catalogBundleEntry,
                AssetEntry = entry,
                CurrentBuildPath = builtBundlePath,
                PreviousAssetState = previousAssetState,
                PreviousBuildPath = previousBundlePath
            };

            operations.Add(operation);
        }

        return operations;
    }

    internal static string BundleIdToBuildPath(string bundleId, string rootLoadPath, string rootBuildPath)
    {
        if (bundleId == null)
            return null;
        bool replaceBackSlashes = rootLoadPath.Contains('/') && !ResourceManagerConfig.ShouldPathUseWebRequest(rootLoadPath);
        string path = replaceBackSlashes ? bundleId.Replace('\\', '/') : bundleId;
        return path.Replace(rootLoadPath, rootBuildPath);
    }

    /// <summary>
    /// When the builtin bundle content has changed during a content update, creates a duplicate of the new builtin bundle
    /// in the first remote group's path and rewires only updated remote asset locations to depend on it.
    /// </summary>
    ///
    private static bool CreateRemoteBuiltinDuplicateAndRewire(ContentUpdateContext updateContext, AddressableAssetsBuildContext aaContext,
        ContentCatalogDataEntry builtInLocation, AssetBundleRequestOptions currentOptions, CachedBundleState previousBundleCache)
    {
        AddressableAssetSettings settings = aaContext.Settings;

        if (!TryGetBuiltinSourcePath(settings, builtInLocation, out string sourcePath))
            return false;

        if (!TryGetFirstRemoteGroupPaths(settings, out string remoteBuildPath, out string remoteLoadPath))
        {
            Debug.LogWarning("Cannot create remote builtin duplicate: no remote group found (no group with a URL load path). Skipping duplicate, failing build.");
            return false;
        }

        string remoteBuiltinFileName = Path.GetFileNameWithoutExtension(currentOptions.BundleName) + "_remote.bundle";
        string remoteDestPath = Path.Combine(remoteBuildPath, remoteBuiltinFileName);
        if (!CopyBuiltinToRemotePath(sourcePath, remoteDestPath))
            return false;

        updateContext.Registry.AddFile(remoteDestPath);

        string remotePrimaryKey = remoteBuiltinFileName;
        string remoteInternalId = CreateBuiltinBundleInternalId(remoteLoadPath, remoteBuiltinFileName);
        ContentCatalogDataEntry remoteEntry = CreateRemoteBuiltinCatalogEntry(remoteInternalId, remotePrimaryKey, currentOptions);
        aaContext.locations.Add(remoteEntry);
        updateContext.IdToCatalogDataEntryMap[remoteInternalId] = remoteEntry;
        string builtInPrimaryKey = GetPrimaryKey(builtInLocation) as string;

        RewireAssetDependenciesToRemoteBuiltin(aaContext.locations, builtInPrimaryKey, remotePrimaryKey);
        return true;
    }

    static bool TryGetBuiltinSourcePath(AddressableAssetSettings settings, ContentCatalogDataEntry builtinLocation, out string path)
    {
        path = null;
        AddressableAssetGroup defaultGroup = settings.GetSharedBundleGroup();
        BundledAssetGroupSchema schema = defaultGroup?.GetSchema<BundledAssetGroupSchema>();
        if (schema == null)
        {
            Debug.LogError("Cannot create remote builtin duplicate: default group has no BundledAssetGroupSchema.");
            return false;
        }
        string buildPath = schema.BuildPath.GetValue(settings);
        if (string.IsNullOrEmpty(buildPath))
        {
            Debug.LogError("Cannot create remote builtin duplicate: default group build path is empty.");
            return false;
        }
        string fileName = Path.GetFileName(builtinLocation.InternalId);
        path = Path.Combine(buildPath, fileName);
        if (!File.Exists(path))
        {
            Debug.LogError($"Cannot create remote builtin duplicate: builtin bundle not found at {path}.");
            return false;
        }
        return true;
    }

    static bool TryGetFirstRemoteGroupPaths(AddressableAssetSettings settings, out string buildPath, out string loadPath)
    {
        buildPath = null;
        loadPath = null;
        AddressableAssetGroup defaultGroup = settings.GetSharedBundleGroup();
        foreach (var group in settings.groups)
        {
            if (group == null || group.Guid == defaultGroup.Guid)
                continue;
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema == null)
                continue;
            string lp = schema.LoadPath.GetValue(settings);
            if (string.IsNullOrEmpty(lp) || !ResourceManagerConfig.ShouldPathUseWebRequest(lp))
                continue;
            string bp = schema.BuildPath.GetValue(settings);
            if (string.IsNullOrEmpty(bp))
            {
                continue;
            }
            buildPath = bp;
            loadPath = lp.Replace('\\', '/').TrimEnd('/');
            return true;
        }
        Debug.LogError("Cannot create remote builtin duplicate: could not locate a remote group to place remote builtin duplicate in.");
        return false;
    }

    static bool CopyBuiltinToRemotePath(string sourcePath, string destPath)
    {
        try
        {
            string dir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.Copy(sourcePath, destPath, true);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to copy builtin bundle to remote path: {ex.Message}");
            return false;
        }
    }

    static string CreateBuiltinBundleInternalId(string remoteLoadPath, string fileName)
    {
        string internalId = remoteLoadPath + "/" + fileName;
        if (!ResourceManagerConfig.ShouldPathUseWebRequest(remoteLoadPath))
            internalId = internalId.Replace('/', Path.DirectorySeparatorChar);
        return internalId;
    }

    static ContentCatalogDataEntry CreateRemoteBuiltinCatalogEntry(string internalId, string primaryKey, AssetBundleRequestOptions currentOptions)
    {
        var options = new AssetBundleRequestOptions(currentOptions);
        options.BundleName = Path.GetFileNameWithoutExtension(primaryKey);

        return new ContentCatalogDataEntry(
            typeof(IAssetBundleResource),
            internalId,
            k_AssetBundleProviderId,
            new List<object> { primaryKey },
            null,
            options);
    }

    static object GetPrimaryKey(ContentCatalogDataEntry entry)
    {
        return (entry.Keys != null && entry.Keys.Count > 0) ? entry.Keys[0] : null;
    }

    /// <summary>
    /// For each asset location that depends on both the builtin bundle and at least one updated-remote bundle,
    /// replace the builtin dependency key with the remote builtin key so they load the duplicate.
    /// </summary>
    static void RewireAssetDependenciesToRemoteBuiltin(List<ContentCatalogDataEntry> locations,
        string builtInPrimaryKey, string remoteBundlePrimaryKey)
    {
        for (int i = 0; i < locations.Count; i++)
        {
            ContentCatalogDataEntry loc = locations[i];
            if (loc.Dependencies == null || loc.Dependencies.Count == 0)
                continue;

            for (int j = 0; j < loc.Dependencies.Count; j++)
            {
                if (object.Equals(loc.Dependencies[j], builtInPrimaryKey))
                {
                    loc.Dependencies[j] = remoteBundlePrimaryKey;
                }
            }
        }
    }

    internal static void ApplyAssetEntryUpdates(List<AssetEntryRevertOperation> operations, ContentUpdateContext contentUpdateContext)
    {
        UnityEngine.Assertions.Assert.IsNotNull(contentUpdateContext.ContentState.cachedBundles, "CachedBundles is null, cachedBundles requires to apply update.");
        foreach (AssetEntryRevertOperation operation in operations)
        {
            //Check that we can replace the entry in the file registry
            if (contentUpdateContext.Registry.ReplaceBundleEntry(Path.GetFileNameWithoutExtension(operation.PreviousBuildPath), operation.PreviousAssetState.bundleFileId))
            {
                File.Delete(operation.CurrentBuildPath);

                //sync the internal ids of the catalog entry and asset entry to the cached state
                operation.BundleCatalogEntry.InternalId = operation.AssetEntry.BundleFileId = operation.PreviousAssetState.bundleFileId;
                var bundleState = contentUpdateContext.ContentState.cachedBundles.FirstOrDefault(s => s.bundleFileId == operation.PreviousAssetState.bundleFileId);
                UnityEngine.Assertions.Assert.IsNotNull(bundleState, "Could not find cached bundle state for " + operation.AssetEntry.BundleFileId);
                UnityEngine.Assertions.Assert.IsNotNull(bundleState.data, "Could not find cached bundle load data for " + operation.AssetEntry.BundleFileId);
                operation.BundleCatalogEntry.Data = bundleState.data;
            }
        }
    }
}
