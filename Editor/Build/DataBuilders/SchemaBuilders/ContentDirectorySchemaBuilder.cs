#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using Unity.Loading;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.U2D;

namespace UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders
{
    /// <summary>
    /// The builder used for Addressable Groups which have the Content Directory schema.
    /// </summary>
    public partial class ContentDirectorySchemaBuilder : ISchemaBuilder
    {
        string m_CatalogBuildPath;
        string m_BuildReportDirectory;

        internal string RootAssetBuildPath
        {
            get => "Library/BuildInstructions/ContentDirectoryRootAssets";
        }

        /// <summary>
        /// Schema builder name
        /// </summary>
        public string Name => "Content Directories";

        /// <summary>
        /// Holds build-time entry data for catalog generation.
        /// </summary>
        struct BuildEntryData
        {
            public string key;
            public string guid;
            public List<string> labels;
            public Type type;
            public bool isScene;
            public int id; // AssetId for regular assets, SceneId for scenes
            public bool includeAddressKey; // false if this entry's own address should not become a catalog key
        }

        Dictionary<string, List<BuildEntryData>> m_CatalogIdToEntriesMap = new Dictionary<string, List<BuildEntryData>>();
        Dictionary<string, string> m_CatalogIdToBuildPathMap = new Dictionary<string, string>();
        Dictionary<string, string> m_CatalogIdToLoadPathMap = new Dictionary<string, string>();
        Dictionary<string, AddressableAssetGroup> m_CatalogIdToFirstGroupMap = new Dictionary<string, AddressableAssetGroup>();
        Dictionary<string, List<string>> m_CatalogIdToGroupGuidsMap = new Dictionary<string, List<string>>();
        List<string> m_ContentDirectoryFilePaths = new List<string>();
        Dictionary<string, int> m_keyToAssetIdMap = new Dictionary<string, int>();

        Dictionary<string, int> m_keyToSceneIdMap = new Dictionary<string, int>();

        // Tracks sub-asset IDs and element types per parent-key pattern (e.g. "multi_sprite_texture[").
        // Populated by EvaluateIsSubObject; consumed by AddAssetCatalogEntry to emit per-type list
        // locations, mirroring standard Addressables' multi-type expansion.
        class SubAssetInfo
        {
            public List<int> Ids = new List<int>();
            public HashSet<Type> ElementTypes = new HashSet<Type>();
        }

        Dictionary<string, SubAssetInfo> m_parentKeyToSubAssetInfo = new Dictionary<string, SubAssetInfo>();

        AddressableRootAsset m_GlobalRootAsset;
        string m_GlobalRootAssetPath;

        private IBuildLogger m_Logger;
        private FileRegistry m_FileRegistry;
        private BuildTarget m_BuildTarget;

        static bool IsEditorTypeOrNull(Type convertedType)
        {
            return convertedType == null || convertedType == typeof(DefaultAsset);
        }

        static void LogEditorTypeStrippedWarning(Type type, string identifier, bool isAssetPath)
        {
            if (type != null)
            {
                string locationDesc = isAssetPath ? "internal id" : "key";
                Debug.LogWarningFormat("Type {0} is in editor assembly {1}. Asset location with {2} {3} will be stripped and not included in the build.",
                    type.Name, type.Assembly.FullName, locationDesc, identifier);
            }
            else
            {
                string locationDesc = isAssetPath ? "path" : "key";
                Debug.LogWarningFormat("Asset with {0} {1} has a null type and will be stripped from the build.", locationDesc, identifier);
            }
        }

        /// <summary>
        /// Builds content directories for the specified catalogs and processes the associated assets.
        /// </summary>
        /// <param name="aaContext">The context for Addressable Assets, used to manage build-related data and state.</param>
        /// <param name="cachedState">A list of cached asset states to carry over into the build process.</param>
        /// <param name="addrResult">The result object to store information about the content directory build process.</param>
        /// <exception cref="Exception">Thrown if the content directory build for any catalog fails.</exception>
        public void Build(AddressableAssetsBuildContext aaContext, AddressablesPlayerBuildResult addrResult)
        {
            if (m_CatalogIdToEntriesMap.Count == 0)
                return;

            if (aaContext.Settings.DisableWriteTypeTree && aaContext.Settings.StripUnityVersion)
                throw new InvalidOperationException(
                    "Cannot build Content Directory groups when both DisableWriteTypeTree and " +
                    "StripUnityVersionFromBundleBuild are enabled. Content Directories require version metadata when " +
                    "type trees are stripped. Please either enable writing type trees, or uncheck Strip Unity Version From Build in the AddressableAssetSettingsObject.");

            if (aaContext.Settings.ExtractTypeTreeData)
                Debug.LogWarning(
                    "AddressableAssetSettings.ExtractTypeTreeData is enabled but Content Directory groups " +
                    "do not support TypeTree extraction in this Unity version. The setting will affect " +
                    "AssetBundle groups only. Content Directories will default to type trees being enabled.");


            BuildTarget editorActiveBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (m_BuildTarget != editorActiveBuildTarget)
            {
                string message =
                    $"Content Directory build requires the Addressables build target ({m_BuildTarget}) to match the Editor active build target ({editorActiveBuildTarget}). Switch the active platform in File > Build Settings, or run the Addressables build for the active platform.";
                m_Logger.AddEntry(LogLevel.Error, message);
                throw new Exception(message);
            }

            // webGL needs a list of all content directory filepaths so it can populate its preload manifest
            // on all other platforms, this list is not used.
            aaContext.ContainsContentDirectoryData = true;


            // Flush the in-memory AddressableRootAsset to disk before building.
            // ProcessGroupSchema populates m_GlobalRootAsset (marking it dirty) but
            // BuildPipeline.BuildContentDirectory reads from the on-disk path; without
            // this save the content directory would embed the empty asset created in Init.
            using (m_Logger.ScopedStep(LogLevel.Verbose, $"Save Root Asset"))
            {
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(m_GlobalRootAssetPath));
            }

            // Use single global AddressableRootAsset for all catalogs
            var globalRootAssetPaths = new string[] { m_GlobalRootAssetPath };

            foreach (var catalogId in m_CatalogIdToEntriesMap.Keys)
            {
                var aaSettings = aaContext.Settings;
                using (m_Logger.ScopedStep(LogLevel.Info, $"Building content directory {catalogId}"))
                {
                    bool hasContentDirectoryGroup = m_CatalogIdToFirstGroupMap.TryGetValue(catalogId, out var firstGroup);
                    var catalogName = catalogId;
                    var buildParams = new BuildContentDirectoryParameters();
                    buildParams.rootAssetPaths = globalRootAssetPaths;
                    buildParams.outputPath = m_CatalogIdToBuildPathMap[catalogId];
                    if (!Directory.Exists(buildParams.outputPath))
                        Directory.CreateDirectory(buildParams.outputPath);
                    if (aaContext.Settings.ArchiveContentDirectories)
                        buildParams.options |= BuildContentOptions.SkipExportToOutputPath;
                    if (aaSettings.DisableWriteTypeTree)
                        buildParams.options |= UnityEditor.BuildContentOptions.DisableWriteTypeTree
                                               | UnityEditor.BuildContentOptions.SerializeUnityVersion;
                    else if (!aaSettings.StripUnityVersion)
                        buildParams.options |= UnityEditor.BuildContentOptions.SerializeUnityVersion;

                    BuildReport report;
                    using (m_Logger.ScopedStep(LogLevel.Info, "BuildPipeline.BuildContentDirectory"))
                    {
                        report = BuildPipeline.BuildContentDirectory(buildParams);
                    }

                    using (m_Logger.ScopedStep(LogLevel.Info, "Post-Build"))
                    {

                        if (BuildHistory.TryGetBuildReportDirectory(report.summary.buildSessionGuid, out string buildReportDirectory))
                        {
                            m_BuildReportDirectory = buildReportDirectory;
                        }

                        if (BuildHistory.TryGetFilePath(report.summary.buildSessionGuid, "BuildContentTEP.json", out string tepPath))
                        {
                            if (m_Logger is ILogTEP tepLogger)
                                tepLogger.ImportExternalTEP(tepPath);
                        }

                        if (report.summary.result != BuildResult.Succeeded)
                        {
                            m_Logger.AddEntry(LogLevel.Error, report.SummarizeErrors());
                            throw new Exception($"Content Directory build for catalog {catalogName} failed with status {report.summary.result}");
                        }

                        var contentDirectoryResultInfo = new AddressablesPlayerBuildResult.ContentDirectoryBuildResult
                        {
                            BuildReportDirectory = m_BuildReportDirectory,
                            CatalogName = catalogName,
                            ContentDirectoryPath = buildParams.outputPath,
                            Hash = report.summary.buildManifestHash.ToString(),
                            BuildSessionGUID = report.summary.buildSessionGuid,
                            GroupGuids = m_CatalogIdToGroupGuidsMap.TryGetValue(catalogId, out var guids) ? guids : new List<string>(),
                        };

                        addrResult.ContentDirectoryBuildResults.Add(contentDirectoryResultInfo);

                        if (hasContentDirectoryGroup)
                        {
                            string outputPath = buildParams.outputPath;
                            if (aaContext.Settings.ArchiveContentDirectories)
                            {
                                var contentLayout = LoadContentLayout();
                                using (m_Logger.ScopedStep(LogLevel.Info, "Archiving Content Directories"))
                                {
                                    var createdFiles = ContentDirectoryArchiver.ArchiveFromUDS(contentLayout, outputPath,
                                        (long)(aaContext.Settings.TargetArchiveSizeInMB * 1024 * 1024), m_Logger);
                                    foreach (var file in createdFiles)
                                        m_FileRegistry.AddFile(file);

                                    if (m_BuildTarget == BuildTarget.WebGL)
                                        m_ContentDirectoryFilePaths.AddRange(createdFiles);
                                }
                            }
                            else if (m_BuildTarget == BuildTarget.WebGL)
                            {
                                var contentLayout = LoadContentLayout();
                                PopulateContentDirectoryFilePathsFromLayout(outputPath, contentLayout);
                            }
                        }
                    }
                }
            }

            if (m_BuildTarget == BuildTarget.WebGL)
            {
                using (m_Logger.ScopedStep(LogLevel.Info, $"Building WebGL file manifest"))
                {
                    WebGLContentDirectoryManifest.WriteManifest(m_ContentDirectoryFilePaths);
                }
            }
        }

        ContentLayout LoadContentLayout()
        {
            using (m_Logger.ScopedStep(LogLevel.Info, "Loading Content Layout"))
            {
                return ContentLayout.Load(m_BuildReportDirectory);
            }
        }

        void PopulateContentDirectoryFilePathsFromLayout(string outputPath, ContentLayout contentLayout)
        {
            foreach (var artifact in contentLayout.BinaryArtifacts)
                m_ContentDirectoryFilePaths.Add(Path.Combine(outputPath, artifact.ContentHash + GetExtension(artifact)));
            m_ContentDirectoryFilePaths.Add(Path.Combine(outputPath, ContentDirectoryArchiver.kBuildManifestHashFileName));
        }

        string GetExtension(ContentLayout.BinaryArtifact artifact)
        {
            if (artifact.Category == ContentDirectoryFileCategory.ContentFile)
                return ".cf";
            if (artifact.Category == ContentDirectoryFileCategory.Manifest)
                return ".json";
            return String.Empty;
        }

        /// <summary>
        /// Determines whether the specified schema can be built as a <see cref="ContentDirectoryGroupSchema"/>.
        /// </summary>
        /// <param name="schema">The schema to evaluate.</param>
        /// <returns><see langword="true"/> if the specified schema is a <see cref="ContentDirectoryGroupSchema"/>; otherwise,
        /// <see langword="false"/>.</returns>
        public bool CanBuildSchema(AddressableAssetGroupSchema schema)
        {
            return schema is ContentDirectoryGroupSchema;
        }

        /// <inheritdoc/>
        /// <summary>
        /// Collects provider types from the first ContentDirectoryGroupSchema instance found.
        /// Since AddressableAssetSettings updates all groups to have the same provider types,
        /// we only need to check the first schema we find.
        /// </summary>
        private void CollectContentDirectoryProviderTypes(AddressableAssetSettings settings, HashSet<Type> providerTypes)
        {
            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;

                var schema = group.GetSchema<ContentDirectoryGroupSchema>();
                if (schema != null && schema.IsEnabled)
                {
                    if (schema.GroupAssetEntryProviderType.Value != null)
                        providerTypes.Add(schema.GroupAssetEntryProviderType.Value);
                    break; // Short-circuit after finding first schema since all groups have the same values
                }
            }
        }

        /// <summary>
        /// Generates catalog locations for each distinct catalog id collected from content-directory
        /// group schemas. Returns a dictionary mapping catalog id to the list of
        /// <see cref="ContentCatalogDataEntry"/> locations; the build script writes the actual
        /// catalog files.
        /// </summary>
        /// <param name="aaContext">The build context containing information about the Addressables assets and providers.</param>
        /// <param name="addrResult">The result of the Addressables player build, including content directory build results.</param>
        /// <returns>A dictionary of catalog id to catalog locations generated by the schema.</returns>
        public Dictionary<string, List<ContentCatalogDataEntry>> GenerateCatalogLocations(AddressableAssetsBuildContext aaContext, AddressablesPlayerBuildResult addrResult)
        {
            CollectContentDirectoryProviderTypes(aaContext.Settings, aaContext.providerTypes);
            aaContext.providerTypes.Add(typeof(AtlasSpriteProvider));
            aaContext.providerTypes.Add(typeof(NativeContentAssetListProvider));

            var result = new Dictionary<string, List<ContentCatalogDataEntry>>();
            foreach (var catalogId in m_CatalogIdToEntriesMap.Keys)
                result[catalogId] = GenerateCatalogEntries(catalogId, aaContext);

            return result;
        }

        List<ContentCatalogDataEntry> GenerateCatalogEntries(string catalogId,
            AddressableAssetsBuildContext aaContext)
        {
            List<ContentCatalogDataEntry> catalogEntries = new List<ContentCatalogDataEntry>();
            using (m_Logger.ScopedStep(LogLevel.Info, $"Generating catalog for {catalogId}"))
            {
                // The Content Directory load path is embedded in each entry's
                // ContentDirectoryAssetData (see AddAssetCatalogEntry) and the asset/scene
                // providers mount the directory from it at runtime. No standalone
                // ContentDirectory location is emitted.
                var loadPath = m_CatalogIdToLoadPathMap[catalogId];
                var entries = m_CatalogIdToEntriesMap[catalogId];

                using (m_Logger.ScopedStep(LogLevel.Info, $"Generating {entries.Count} entries for {catalogId}"))
                {
                    foreach (var entry in entries)
                    {
                        AddSpriteAtlasEntries(entry, catalogEntries);
                        AddAssetCatalogEntry(entry, loadPath, catalogEntries);
                    }
                }
            }

            return catalogEntries;
        }

        /// <summary>
        /// Generates a content update for Addressable assets based on the provided context.
        /// </summary>
        /// <param name="aaContext">The build context containing information about the Addressable assets and their dependencies.</param>
        /// <param name="cachedState">A list of cached asset states representing the previous build's asset information.</param>
        /// <param name="addrResult">The result object that will store the outcome of the Addressables player build process.</param>
        public void GenerateContentUpdate(AddressableAssetsBuildContext aaContext, AddressablesPlayerBuildResult addrResult)
        {
        }

        /// <summary>
        /// Relocates the metadata hash file to a known location for player build type stripping purposes
        /// </summary>
        /// <param name="aaContext">The build context containing information about the Addressable assets and their dependencies.</param>
        /// <param name="contentCatalog">Content catalog data of the build.</param>
        public void GenerateTypeStrippingInfo(AddressableAssetsBuildContext aaContext, ContentCatalogData contentCatalog)
        {
            if (!Directory.Exists(m_BuildReportDirectory))
                return;

            string scriptsOnlyCacheMetadataPath = Path.Combine(m_BuildReportDirectory, "ScriptsOnlyCache.yaml");
            string platformSpecificCachePath = aaContext.Settings.GetContentStateBuildPath();
            string scriptsOnlyCacheBuildPath = Path.Combine(platformSpecificCachePath, "ScriptsOnlyCache.yaml");
            if (File.Exists(scriptsOnlyCacheBuildPath))
                File.Delete(scriptsOnlyCacheBuildPath);
            string directory = Path.GetDirectoryName(scriptsOnlyCacheBuildPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            FileUtil.CopyFileOrDirectory(scriptsOnlyCacheMetadataPath, scriptsOnlyCacheBuildPath);
            m_FileRegistry.AddFile(scriptsOnlyCacheBuildPath);
        }

        /// <summary>
        /// Initializes the build context and prepares internal data structures for processing.
        /// </summary>
        /// <param name="aaContext">The <see cref="AddressableAssetsBuildContext"/> containing the build context information.</param>
        /// <param name="builderInput">The input parameters for the Addressables data builder.</param>
        /// <param name="buildContext">The Scriptable Build Pipeline build context.</param>
        /// <param name="dataBuilder">The <see cref="IDataBuilder"/> responsible for managing the data building process.</param>
        public void Init(AddressableAssetsBuildContext aaContext,
            AddressablesDataBuilderInput builderInput,
            BuildContext buildContext,
            IDataBuilder dataBuilder)
        {
            m_Logger = builderInput.Logger;
            m_FileRegistry = builderInput.Registry;
            m_BuildTarget = builderInput.Target;

            m_CatalogIdToEntriesMap.Clear();
            m_CatalogIdToBuildPathMap.Clear();
            m_CatalogIdToLoadPathMap.Clear();
            m_CatalogIdToFirstGroupMap.Clear();
            m_CatalogIdToGroupGuidsMap.Clear();
            m_SpriteAtlasKeyToSprites.Clear();
            m_ContentDirectoryFilePaths.Clear();
            m_keyToAssetIdMap.Clear();
            m_keyToSceneIdMap.Clear();
            m_parentKeyToSubAssetInfo.Clear();
            m_GlobalRootAsset = null;
            m_GlobalRootAssetPath = null;
            WebGLContentDirectoryManifest.ClearManifest();

            CreateContentDirectoryBuildFolder();
            CreateGlobalAddressableRootAsset();
        }

        void CreateGlobalAddressableRootAsset()
        {
            using (m_Logger.ScopedStep(LogLevel.Verbose, "CreateGlobalAddressableRootAsset"))
            {
                m_GlobalRootAsset = ScriptableObject.CreateInstance<AddressableRootAsset>();
                m_GlobalRootAssetPath = $"{RootAssetBuildPath}/AddressableRootAsset.asset";
                AssetDatabase.CreateAsset(m_GlobalRootAsset, m_GlobalRootAssetPath);
            }
        }

        struct SpriteChildEntryData
        {
            public string address;
            public List<string> labels; // folder key, when the sprite is a folder child
            public bool includeAddressKey; // false if the sprite's own address should not become a catalog key
        }

        Dictionary<string, List<SpriteChildEntryData>> m_SpriteAtlasKeyToSprites = new Dictionary<string, List<SpriteChildEntryData>>();

        /// <summary>
        /// Processes the specified group schema.
        /// </summary>
        /// <param name="schema">The schema to process. Must be of type <see cref="ContentDirectoryGroupSchema"/>. Use <c>schema.Group</c> to access the owning group.</param>
        /// <param name="aaContext">The build context containing settings and other relevant data for the Addressable system.</param>
        /// <returns>An empty string if no error was encountered, otherwise it returns the error.</returns>
        public string ProcessGroupSchema(AddressableAssetsBuildContext aaContext, AddressableAssetGroupSchema schema)
        {
            if (schema is not ContentDirectoryGroupSchema contentDirectoryGroupSchema || !contentDirectoryGroupSchema.Group.IncludeInBuild || !contentDirectoryGroupSchema.IsEnabled)
                return "";

            string catalogId = schema.CatalogId;
            using (m_Logger.ScopedStep(LogLevel.Verbose, "ContentDirectorySchema.ProcessGroupSchema",
                       ("Catalog", catalogId)))
            {

                if (!m_CatalogIdToEntriesMap.ContainsKey(catalogId))
                {
                    m_CatalogIdToEntriesMap[catalogId] = new List<BuildEntryData>();
                    m_CatalogIdToGroupGuidsMap[catalogId] = new List<string>();
                }

                // Track first group for PostProcessDirectory and all group GUIDs for build results
                if (!m_CatalogIdToFirstGroupMap.ContainsKey(catalogId))
                    m_CatalogIdToFirstGroupMap[catalogId] = schema.Group;
                m_CatalogIdToGroupGuidsMap[catalogId].Add(schema.Group.Guid);

                // Gather all assets including subassets and sprites for sprite atlas
                var allEntries = new List<AddressableAssetEntry>();
                using (m_Logger.ScopedStep(LogLevel.Verbose, "GatherAllAssets", true))
                {
                    foreach (var entry in schema.Group.entries)
                    {
                        using (m_Logger.ScopedStep(LogLevel.Verbose, "GatherAssets",
                                   ("Address", entry.address),
                                   ("Path", entry.AssetPath),
                                   ("Guid", entry.guid)))
                        {
                            entry.GatherAllAssets(allEntries, includeSelf: true, recurseAll: true, includeSubObjects: true);
                        }
                    }
                }

                foreach (var entry in allEntries)
                {
                    var targetAsset = entry.TargetAsset;
                    if (targetAsset == null)
                    {
                        Debug.LogWarning($"Skipping entry with null TargetAsset: {entry.address}");
                        continue;
                    }
                    using (m_Logger.ScopedStep(LogLevel.Verbose, "GatherSpriteAtlases",
                               ("Address", entry.address),
                               ("Path", entry.AssetPath),
                               ("Guid", entry.guid)))
                    {
                        if (entry.ParentEntry != null && entry.ParentEntry.TargetAsset.GetType() == typeof(SpriteAtlas))
                        {
                            var parentKey = entry.ParentEntry.address;

                            var spriteLabels = new List<string>();
                            if (contentDirectoryGroupSchema.IncludeLabelsInCatalog)
                                spriteLabels.AddRange(entry.labels);
                            string spriteFolderKey = contentDirectoryGroupSchema.IncludeFolderKeysInCatalog ? entry.ParentFolderAddress : null;
                            bool isSpriteFolderChild = !string.IsNullOrEmpty(spriteFolderKey) && spriteFolderKey != entry.address;
                            if (isSpriteFolderChild)
                                spriteLabels.Add(spriteFolderKey);

                            var spriteChildData = new SpriteChildEntryData
                            {
                                address = entry.address,
                                labels = spriteLabels,
                                includeAddressKey = contentDirectoryGroupSchema.IncludeAddressesForFolderChildren || !isSpriteFolderChild
                            };

                            if (m_SpriteAtlasKeyToSprites.ContainsKey(parentKey))
                                m_SpriteAtlasKeyToSprites[parentKey].Add(spriteChildData);
                            else
                                m_SpriteAtlasKeyToSprites.Add(parentKey, new List<SpriteChildEntryData> { spriteChildData });
                            continue;
                        }
                    }

                    string entryGuid = entry.guid;
                    Type convertedType;
                    using (m_Logger.ScopedStep(LogLevel.Verbose, "GatherSubassets",
                               ("Address", entry.address),
                               ("Path", entry.AssetPath),
                               ("Guid", entry.guid)))
                    {
                        // For subassets, use parent's GUID since subassets don't have their own GUID
                        if (string.IsNullOrEmpty(entryGuid) && entry.IsSubAsset && entry.ParentEntry != null)
                            entryGuid = entry.ParentEntry.guid;

                        // For subassets, use the actual subasset type (e.g., Sprite) rather than MainAssetType
                        // (e.g., Texture2D). This ensures type filtering works correctly when loading by GUID-based keys.
                        Type assetType = (entry.IsSubAsset && entry.TargetAsset != null)
                            ? entry.TargetAsset.GetType()
                            : entry.MainAssetType;
                        convertedType = AddressableAssetUtility.MapEditorTypeToRuntimeType(assetType, false);
                        if (IsEditorTypeOrNull(convertedType))
                        {
                            LogEditorTypeStrippedWarning(entry.MainAssetType, entry.AssetPath, isAssetPath: true);
                            continue;
                        }
                    }

                    // Add to AddressableRootAsset and track the ID
                    int id;
                    bool isScene = entry.IsScene;
                    using (m_Logger.ScopedStep(LogLevel.Verbose, "CreateLoadable",
                               ("IsScene", isScene.ToString()),
                               ("Address", entry.address),
                               ("Path", entry.AssetPath),
                               ("Guid", entry.guid)))
                    {

                        if (isScene)
                        {
                            var sceneId = LoadableSceneIdEditorUtility.CreateLoadableSceneId(entry.AssetPath);
                            id = m_GlobalRootAsset.AddScene(sceneId);
                            m_keyToSceneIdMap[entry.address] = id;
                        }
                        else
                        {
                            var loadableObjId = LoadableObjectIdEditorUtility.CreateLoadableObjectId(targetAsset.GetEntityId());
                            id = m_GlobalRootAsset.AddAsset(loadableObjId);
                            m_keyToAssetIdMap[entry.address] = id;
                        }
                    }

                    // Build labels list
                    var labels = new List<string>();
                    if (contentDirectoryGroupSchema.IncludeLabelsInCatalog)
                        labels.AddRange(entry.labels);
                    string folderKey = contentDirectoryGroupSchema.IncludeFolderKeysInCatalog ? entry.ParentFolderAddress : null;
                    bool isFolderChild = !string.IsNullOrEmpty(folderKey) && folderKey != entry.address;
                    if (isFolderChild)
                        labels.Add(folderKey);
                    if (entry.MainAsset != entry.TargetAsset && entry.MainAssetType != typeof(SpriteAtlas))
                    {
                        string guidBasedKey = $"{entryGuid}[{targetAsset.name}]";
                        labels.Add(guidBasedKey);
                    }

                    // Store entry data for catalog generation
                    var buildEntry = new BuildEntryData
                    {
                        key = entry.address,
                        guid = entryGuid,
                        labels = labels,
                        type = convertedType,
                        isScene = isScene,
                        id = id,
                        includeAddressKey = contentDirectoryGroupSchema.IncludeAddressesForFolderChildren || !isFolderChild
                    };
                    m_CatalogIdToEntriesMap[catalogId].Add(buildEntry);

                    EvaluateIsSubObject(buildEntry);
                }

                EditorUtility.SetDirty(m_GlobalRootAsset);

                if (!m_CatalogIdToBuildPathMap.ContainsKey(catalogId))
                    m_CatalogIdToBuildPathMap[catalogId] = contentDirectoryGroupSchema.BuildPath.GetValue(aaContext.Settings);
                m_CatalogIdToLoadPathMap[catalogId] = contentDirectoryGroupSchema.LoadPath.GetValue(aaContext.Settings);
            }

            return string.Empty;
        }

        void EvaluateIsSubObject(BuildEntryData buildEntry)
        {
            if (buildEntry.isScene)
                return;

            using (m_Logger.ScopedStep(LogLevel.Verbose, "EvaluateIsSubObject",
                       ("Id", buildEntry.id.ToString()),
                        ("Guid", buildEntry.guid)))
            {
                int bracketIdx = buildEntry.key.IndexOf('[');
                if (bracketIdx > 0)
                {
                    string parentKey = buildEntry.key.Substring(0, bracketIdx + 1);
                    if (!m_parentKeyToSubAssetInfo.TryGetValue(parentKey, out var info))
                    {
                        info = new SubAssetInfo();
                        m_parentKeyToSubAssetInfo[parentKey] = info;
                    }

                    info.Ids.Add(buildEntry.id);
                    info.ElementTypes.Add(buildEntry.type);
                }
            }
        }

        void CreateContentDirectoryBuildFolder()
        {
            using (m_Logger.ScopedStep(LogLevel.Verbose, "CreateContentDirectoryBuildFolder"))
            {

                if (Directory.Exists(RootAssetBuildPath))
                    Directory.Delete(RootAssetBuildPath, true);
                Directory.CreateDirectory(RootAssetBuildPath);
                AssetDatabase.ImportAsset(RootAssetBuildPath, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        string GetParentKeyPattern(string key)
        {
            int idx = key.IndexOf('[');
            return idx >= 0 ? key.Substring(0, idx + 1) : key + "[";
        }

        void AddSpriteAtlasEntries(BuildEntryData entry, List<ContentCatalogDataEntry> catalogEntries)
        {
            if (entry.type != typeof(SpriteAtlas))
                return;

            if (!m_SpriteAtlasKeyToSprites.TryGetValue(entry.key, out var spritesEntries))
                return;

            foreach (var spriteEntry in spritesEntries)
            {
                var keys = new List<string>();
                if (spriteEntry.includeAddressKey)
                    keys.Add(spriteEntry.address);
                keys.AddRange(spriteEntry.labels);

                ContentCatalogDataEntry spriteAddressableEntry = new ContentCatalogDataEntry(
                    typeof(Sprite),
                    spriteEntry.address,
                    typeof(AtlasSpriteProvider).FullName,
                    keys,
                    new List<string>() { entry.key } // Dependency on parent atlas
                );
                catalogEntries.Add(spriteAddressableEntry);
            }
        }

        /// <summary>
        /// When a non-sub-asset entry (e.g. a multi-sprite Texture2D) has precomputed
        /// sub-asset IDs whose element type differs from the parent type, emits an extra
        /// catalog location typed as the sub-asset element type (e.g. Sprite). This
        /// mirrors the standard Addressables multi-type expansion and allows
        /// <c>LoadAssetAsync&lt;IList&lt;Sprite&gt;&gt;(bareKey)</c> to resolve through the catalog
        /// type filter so <see cref="NativeContentAssetEntryProvider.HandleListRequest"/>
        /// can execute.
        /// </summary>
        /// <summary>
        /// For each sub-asset element type that differs from the parent entry's type, emits an
        /// additional catalog location at the same bare key so that
        /// <c>LoadAssetAsync&lt;IList&lt;T&gt;&gt;(bareKey)</c> resolves through the catalog type filter
        /// and <see cref="NativeContentAssetEntryProvider.HandleListRequest"/> can execute.
        /// Mirrors standard Addressables' multi-type expansion (one catalog entry per serialized type).
        /// </summary>
        void AddSubAssetListCatalogEntry(BuildEntryData entry, SubAssetInfo subAssetInfo, string loadPath,
            List<string> keys, ContentDirectoryAssetData assetData, List<ContentCatalogDataEntry> catalogEntries)
        {
            if (subAssetInfo == null)
                return;

            foreach (var subAssetElementType in subAssetInfo.ElementTypes)
            {
                if (subAssetElementType == entry.type)
                    continue;

                var listEntry = new ContentCatalogDataEntry(
                    subAssetElementType,
                    entry.key,
                    typeof(NativeContentAssetListProvider).FullName,
                    keys,
                    null,
                    assetData
                );

                listEntry.Keys.AddRange(entry.labels);
                catalogEntries.Add(listEntry);
            }
        }

        void AddAssetCatalogEntry(BuildEntryData entry, string loadPath, List<ContentCatalogDataEntry> catalogEntries)
        {
            string providerTypeName = typeof(NativeContentAssetEntryProvider).FullName;

            int[] subAssetIds = null;
            SubAssetInfo subAssetInfo = null;
            List<string> keys = new List<string>();
            if (entry.includeAddressKey)
                keys.Add(entry.key);

            if (!ResourceManagerConfig.ExtractKeyAndSubKey(entry.key, out string mainKey, out string subKey))
            {
                keys.Add(entry.guid);

                if (!entry.isScene)
                {
                    string parentPattern = GetParentKeyPattern(entry.key);
                    m_parentKeyToSubAssetInfo.TryGetValue(parentPattern, out subAssetInfo);
                    if (subAssetInfo != null)
                        subAssetIds = subAssetInfo.Ids.ToArray();
                }
            }

            var assetData = new ContentDirectoryAssetData
            {
                AssetId = entry.isScene ? ContentDirectoryAssetData.kInvalidId : entry.id,
                SceneId = entry.isScene ? entry.id : ContentDirectoryAssetData.kInvalidId,
                SubAssetIds = subAssetIds,
                LoadPath = loadPath
            };

            var addressableEntry = new ContentCatalogDataEntry(
                entry.type,
                entry.key,
                providerTypeName,
                keys,
                null,
                assetData
            );

            addressableEntry.Keys.AddRange(entry.labels);
            catalogEntries.Add(addressableEntry);

            AddSubAssetListCatalogEntry(entry, subAssetInfo, loadPath, keys, assetData, catalogEntries);
        }
    }
}
#endif
