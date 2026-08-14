#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.CatalogBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using Unity.Loading;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders
{
    /// <summary>
    /// Data used during the Content Directory build process.
    /// </summary>
    public class ContentDirectoryParameters : BuildParameters
    {
        /// <summary>
        /// The Editor's version of the build parameters.
        /// </summary>
        public BuildContentDirectoryParameters Parameters;

        /// <summary>
        /// Constructor for ContentDirectoryParameters.
        /// </summary>
        /// <param name="parameters">The Editor buidl content directory parameters object.</param>
        public ContentDirectoryParameters(BuildContentDirectoryParameters parameters)
        {
            Parameters = parameters;
            OutputFolder = parameters.outputPath;
        }

        /// <summary>
        /// Gets the parameters for building content directories used by the Editor build process.
        /// </summary>
        /// <returns>The Editor's build content directory parameters object stored by this wrapper which Scriptable Build Pipeline uses.</returns>
        public override BuildContentDirectoryParameters GetContentDirectoryParameters()
        {
            return Parameters;
        }
    }

    /// <summary>
    /// The builder used for Addressable Groups which have the Content Directory schema.
    /// </summary>
    public class ContentDirectorySchemaBuilder : ISchemaBuilder
    {
        private bool m_BuiltData = false;

        string m_CatalogBuildPath;
        string m_MetadataDirectoryPath;

        public const string kContentDirectoryPathVariable = "[ContentDirectoryPath]";
        internal string RootAssetBuildPath
        {
            get => "Library/BuildInstructions/ContentDirectoryRootAssets";
        }

        /// <summary>
        /// Schema builder name
        /// </summary>
        public string Name => "Content Directories";

        Dictionary<string, List<Object>> m_CatalogIdToRootAssetMap = new Dictionary<string, List<Object>>();
        Dictionary<string, List<string>> m_CatalogIdToRootAssetsPathMap = new Dictionary<string, List<string>>();
        Dictionary<Object, AddressableAssetGroup> m_RootAssetToGroupMap = new Dictionary<Object, AddressableAssetGroup>();
        Dictionary<Object, string> m_RootAssetToAssetPathMap = new Dictionary<Object, string>();
        Dictionary<string, string> m_CatalogIdToLoadPathMap = new Dictionary<string, string>();
        List<string> m_ContentDirectoryFilePaths = new List<string>();

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
        /// <param name="buildContext">The context for the build process, containing shared state and configuration.</param>
        /// <param name="builderInput">The input parameters for the Addressables data builder.</param>
        /// <param name="aaContext">The context for Addressable Assets, used to manage build-related data and state.</param>
        /// <param name="extractData">A task responsible for extracting data during the build process.</param>
        /// <param name="carryOverCachedState">A list of cached asset states to carry over into the build process.</param>
        /// <param name="addrResult">The result object to store information about the content directory build process.</param>
        /// <exception cref="Exception">Thrown if the content directory build for any catalog fails.</exception>
        public void Build(BuildContext buildContext, AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext, ExtractDataTask extractData, List<CachedAssetState> carryOverCachedState, AddressablesPlayerBuildResult addrResult)
        {
            if (m_CatalogIdToRootAssetMap.Count == 0)
                return;
            //This is just to make sure we don't call it multiple times if there's multiple catalogs
            //fwiw I don't love this and would like to change this later
            bool postProcessDirectoryCalled = false;

            // webGL needs a list of all content directory filepaths so it can populate its preload manifest
            // on all other platforms, this list is not used.

            aaContext.ContainsContentDirectoryData = true;
            foreach (var entry in m_CatalogIdToRootAssetMap)
            {
                var aaSettings = builderInput.AddressableSettings;
                using (builderInput.Logger.ScopedStep(LogLevel.Info, $"Building content directory {entry.Key}"))
                {
                    var sourceAssetGroups = new List<AddressableAssetGroup>(entry.Value.Count);
                    var rootAssetPaths = new string[entry.Value.Count];
                    for (int i = 0; i < entry.Value.Count; i++)
                    {
                        rootAssetPaths[i] = m_RootAssetToAssetPathMap[entry.Value[i]];
                        sourceAssetGroups.Add(m_RootAssetToGroupMap[entry.Value[i]]);
                    }

                    var catalogName = entry.Key;
                    // add logger scope here
                    var buildParams = new BuildContentDirectoryParameters();
                    buildParams.rootAssetPaths = rootAssetPaths;
                    buildParams.outputPath = aaSettings.buildSettings.contentDirectoryBuildPath;
                    buildParams.targetPlatform = builderInput.Target;

                    ContentDirectoryParameters cdParams = new ContentDirectoryParameters(buildParams);
                    cdParams.Target = builderInput.Target;
                    cdParams.Group = builderInput.TargetGroup;
                    IBuildResults results = new BundleBuildResults();

                    List<IBuildTask> tasks = new List<IBuildTask>();
                    tasks.Add(new BuildContentDirectoriesTask());
                    tasks.Add(extractData);

                    var buildStatus = ContentPipeline.BuildContentDirectories(buildContext, cdParams, out results, tasks, aaContext);
                    if (buildStatus < ReturnCode.Success || extractData.BuildReportContext == null || extractData.BuildReportContext.Report == null)
                        throw new Exception($"Content Directory build for catalog {catalogName} failed with status {buildStatus}");

                    var result = extractData.BuildReportContext.Report;

                    if (BuildHistory.TryGetMetadataPath(result.summary.buildSessionGuid, out string metadataLocation))
                    {
                        m_MetadataDirectoryPath = metadataLocation;
                    }

                    if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                    {
                        builderInput.Logger.AddEntry(LogLevel.Error, result.SummarizeErrors());
                        throw new Exception($"Content Directory build for catalog {catalogName} failed with status {result.summary.result}");
                    }

                    var contentDirectoryResultInfo = new AddressablesPlayerBuildResult.ContentDirectoryBuildResult
                    {
                        ContentDirectoryMetaDataPath = m_MetadataDirectoryPath,
                        CatalogName = catalogName,
                        ContentDirectoryPath = buildParams.outputPath,
                        Hash = result.summary.buildManifestHash.ToString(),
                        BuildSessionGUID = result.summary.buildSessionGuid,
                        GroupGuids = new List<string>(),
                    };

                    foreach (var rootAsset in buildParams.rootAssetPaths)
                    {
                        if (aaContext.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(rootAsset)) is AddressableAssetEntry assetEntry)
                        {
                            contentDirectoryResultInfo.GroupGuids.Add(assetEntry.parentGroup.Guid);
                        }
                        else if (AssetDatabase.LoadAssetAtPath<GroupRootAsset>(rootAsset) is GroupRootAsset addrCD)
                        {
                            contentDirectoryResultInfo.GroupGuids.Add(addrCD.Key);
                        }
                        else
                        {
                            Debug.LogError($"{rootAsset} is neither an {nameof(AddressableAssetEntry)} nor an {nameof(GroupRootAsset)}, but it has been passed into the build as a RootAsset. " +
                                $"The asset at this path needs to either be marked as Addressable or converted to a {nameof(GroupRootAsset)}");
                        }
                    }

                    addrResult.ContentDirectoryBuildResults.Add(contentDirectoryResultInfo);

                    if (!postProcessDirectoryCalled)
                    {
                        using (var progressTracker = new ProgressTracker())
                        {
                            if (entry.Value.Count > 0)
                            {
                                var rootAsset = entry.Value[0]; //This also only needs to be called once
                                var assetGroup = m_RootAssetToGroupMap[rootAsset];
                                using (builderInput.Logger.ScopedStep(LogLevel.Info, assetGroup.name))
                                {
                                    progressTracker.UpdateTask("Post Processing Content Directories");
                                    var outputPath = PostProcessDirectory(assetGroup, result, addrResult,
                                        builderInput.Registry, aaContext, builderInput.Target, builderInput.Logger);

                                    if (aaContext.Settings.ArchiveContentDirectories)
                                    {
                                        progressTracker.UpdateTask("Archiving Content Directories");
                                        ArchiveContentDirectories(outputPath, (long)(aaContext.Settings.TargetArchiveSizeInMB * 1024 * 1024), builderInput.Registry);
                                        if (builderInput.Target == BuildTarget.WebGL)
                                            m_ContentDirectoryFilePaths.AddRange(Directory.GetFiles(outputPath, "content*.archive"));
                                    }

                                    postProcessDirectoryCalled = true;
                                }
                            }
                        }
                    }

                }
            }

            if (builderInput.Target == BuildTarget.WebGL)
            {
                using (builderInput.Logger.ScopedStep(LogLevel.Info, $"Building WebGL file manifest"))
                {
                    WebGLContentDirectoryManifest.WriteManifest(m_ContentDirectoryFilePaths);
                }
            }

            m_BuiltData = true;
        }


        string PostProcessDirectory(AddressableAssetGroup assetGroup, BuildReport buildResult, AddressablesPlayerBuildResult addrResult,
            FileRegistry registry, AddressableAssetsBuildContext aaContext, BuildTarget target, IBuildLogger logger)
        {
            var schema = assetGroup.GetSchema<ContentDirectoryGroupSchema>();
            if (schema == null || !schema.IsEnabled)
                return null;

            var path = schema.BuildPath.GetValue(assetGroup.Settings);
            if (string.IsNullOrEmpty(path))
                return null;

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var outputPath = buildResult.summary.outputPath;
            if (string.IsNullOrEmpty(outputPath))
                throw new InvalidOperationException("Content Directory build has no output path in the build report summary.");
            if (!Directory.Exists(outputPath))
                throw new InvalidOperationException(
                    $"Content Directory build output path does not exist: {outputPath}");

            var outputFullPath = Path.GetFullPath(outputPath);
            var buildReportPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in buildResult.GetFiles())
            {
                buildReportPaths.Add(file.path);
                // for WebGL, also save paths to later put into preload manifest
                if (target == BuildTarget.WebGL)
                {
                    m_ContentDirectoryFilePaths.Add(Path.Combine(path, Path.GetFileName(file.path)));
                }
            }

            foreach (var sourcePath in Directory.EnumerateFiles(outputPath, "*", SearchOption.AllDirectories))
            {
                var sourceFullPath = Path.GetFullPath(sourcePath);
                var relativePath = Path.GetRelativePath(outputFullPath, sourceFullPath);

                if (!buildReportPaths.Contains(relativePath))
                {
                    logger?.AddEntry(LogLevel.Warning,
                        $"Unexpected file in content directory build output (not listed in build report): {relativePath}");
                }

                var targetPath = Path.Combine(path, relativePath);
                var parentDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                    Directory.CreateDirectory(parentDir);
                if (File.Exists(targetPath))
                {
                    logger?.AddEntry(LogLevel.Warning,
                        $"Content Directory post-process expected to copy a file, but the target already exists: {targetPath}");
                }
                else
                    FileUtil.CopyFileOrDirectory(sourcePath, targetPath);
                registry.AddFile(targetPath);
            }

            return path;
        }

        /// <summary>
        /// Method to archive the content directory build files.
        /// </summary>
        /// <param name="archiveOutputDirectory">The output directory.</param>
        /// <param name="avgMaxSize">Desired average max archive size.</param>
        /// <param name="registry">File registry to store results.  Files that are archived should be deleted and removed from the registry.  New archives should be added.</param>
        public virtual void ArchiveContentDirectories(string archiveOutputDirectory, long avgMaxSize, FileRegistry registry)
        {
            ContentDirectoryArchiver.ArchiveAndUpdateRegistry(archiveOutputDirectory, avgMaxSize, registry);
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
                    if (schema.ContentDirectoryProviderType.Value != null)
                        providerTypes.Add(schema.ContentDirectoryProviderType.Value);
                    if (schema.GroupRootAssetProviderType.Value != null)
                        providerTypes.Add(schema.GroupRootAssetProviderType.Value);
                    if (schema.GroupRootAssetEntryProviderType.Value != null)
                        providerTypes.Add(schema.GroupRootAssetEntryProviderType.Value);
                    break; // Short-circuit after finding first schema since all groups have the same values
                }
            }
        }

        /// <summary>
        /// Generates a collection of content catalogs based on the provided build input, build context, and build
        /// results.
        /// </summary>
        /// <param name="builderInput">The input data for the Addressables build process, including configuration and settings.</param>
        /// <param name="aaContext">The build context containing information about the Addressables assets and providers.</param>
        /// <param name="addrResult">The result of the Addressables player build, including content directory build results.</param>
        /// <returns>A list of <see cref="ContentCatalogData"/> objects representing the generated content catalogs.</returns>

        public List<ContentCatalogData> GenerateCatalogs(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext, AddressablesPlayerBuildResult addrResult)
        {
            CollectContentDirectoryProviderTypes(aaContext.Settings, aaContext.providerTypes);
            aaContext.providerTypes.Add(typeof(AtlasSpriteProvider));

            List<ContentCatalogData> catalogs = new List<ContentCatalogData>();
            foreach (var catalogId in m_CatalogIdToRootAssetMap.Keys)
            {
                var hashingObjects = new List<string>();
                for (int i = 0; i < addrResult.ContentDirectoryBuildResults.Count; ++i)
                    if (addrResult.ContentDirectoryBuildResults[i].CatalogName == catalogId)
                        hashingObjects.Add(addrResult.ContentDirectoryBuildResults[i].Hash);
                var buildResultHash = HashingMethods.Calculate(hashingObjects.ToArray()).ToString();

                catalogs.Add(GenerateCatalogEntries(catalogId, aaContext, builderInput, buildResultHash));
            }

            return catalogs;
        }

        Type GetContentDirectoryProviderType(string catalogId)
        {
            Type contentDirectoryProviderType = typeof(ContentDirectoryProvider);
            if (!m_CatalogIdToRootAssetMap.TryGetValue(catalogId, out var rootAssets) || rootAssets.Count <= 0)
            {
                throw new Exception("No root assets found for catalog " + catalogId);
            }
            // Get provider type from first group with this catalogId (all groups with same catalogId should have same provider types)
            var firstRootAsset = rootAssets[0];
            if (m_RootAssetToGroupMap.TryGetValue(firstRootAsset, out var firstGroup))
            {
                var firstSchema = firstGroup.GetSchema<ContentDirectoryGroupSchema>();
                if (firstSchema != null && firstSchema.IsEnabled && firstSchema.ContentDirectoryProviderType.Value != null)
                    contentDirectoryProviderType = firstSchema.ContentDirectoryProviderType.Value;
            }
            return contentDirectoryProviderType;
        }

        ContentCatalogData GenerateCatalogEntries(string catalogId,
            AddressableAssetsBuildContext aaContext,
            AddressablesDataBuilderInput builderInput,
            string buildHash)
        {
            ICatalogBuilder catalogBuilder;
            List<ContentCatalogDataEntry> catalogEntries = new List<ContentCatalogDataEntry>();
            using (builderInput.Logger.ScopedStep(LogLevel.Info, $"Generating catalog for {catalogId}"))
            {
                Type contentDirectoryProviderType = GetContentDirectoryProviderType(catalogId);

                //The ContentDirectory location that an asset needs.
                ContentCatalogDataEntry contentDirectoryEntry = new ContentCatalogDataEntry(
                typeof(ContentDirectoryHandle),
                m_CatalogIdToLoadPathMap[catalogId],
                contentDirectoryProviderType.FullName,
                new List<string>() { m_CatalogIdToLoadPathMap[catalogId] }
            );
                catalogEntries.Add(contentDirectoryEntry);

                List<Object> rootAssets = m_CatalogIdToRootAssetMap[catalogId];
                List<string> rootAssetPaths = m_CatalogIdToRootAssetsPathMap[catalogId];

                for (int i = 0; i < rootAssets.Count; i++)
                {
                    var rootAssetObj = rootAssets[i];

                    //Not sure that I love the hard reliance on this type here, but not sure of a better way right now.
                    GroupRootAsset rootAsset = rootAssetObj as GroupRootAsset;
                    if (rootAsset == null)
                    {
                        //the rootAsset has been unloaded somehow, let's see if we can reload it
                        rootAsset = AssetDatabase.LoadAssetAtPath<GroupRootAsset>(rootAssetPaths[i]);

                        if (rootAsset == null)
                            continue;
                    }

                    var group = m_RootAssetToGroupMap[rootAsset];
                    var schema = group.GetSchema<ContentDirectoryGroupSchema>();
                    if (schema == null || !schema.IsEnabled)
                        continue;
                    var loadPath = schema.LoadPath.GetValue(group.Settings);

                    // Get provider type from schema, fallback to default if null
                    Type groupRootAssetProviderType = schema.GroupRootAssetProviderType.Value ?? typeof(GroupRootAssetProvider);

                    //The AddressableContentDirectory location that contains the Loadable entries.
                    ContentCatalogDataEntry addressableContentDirectory = new ContentCatalogDataEntry(
                        typeof(GroupRootAsset),
                        rootAsset.name,
                        groupRootAssetProviderType.FullName,
                        new List<string>() { rootAsset.name },
                        new List<string>() { loadPath }
                    );
                    catalogEntries.Add(addressableContentDirectory);

                    using (builderInput.Logger.ScopedStep(LogLevel.Info, $"Generating entries for {rootAsset.name}"))
                    {
                        //The individual Loadable entries.
                        foreach (var asset in rootAsset.Assets)
                        {
                            var convertedType = AddressableAssetUtility.MapEditorTypeToRuntimeType(asset.type, false);
                            if (IsEditorTypeOrNull(convertedType))
                            {
                                LogEditorTypeStrippedWarning(asset.type, asset.key, isAssetPath: false);
                                continue;
                            }
                            if (asset.type == typeof(SpriteAtlas))
                            {
                                if (m_SpriteAtlasKeyToSprites.TryGetValue(asset.key, out var spritesEntries))
                                {
                                    foreach (var spriteEntry in spritesEntries)
                                    {
                                        ContentCatalogDataEntry spriteAddressableEntry = new ContentCatalogDataEntry(
                                            typeof(Sprite),
                                            spriteEntry.address,
                                            typeof(AtlasSpriteProvider).FullName,
                                            new List<string>() { spriteEntry.address },
                                            new List<string>() { asset.key }  // Dependency on parent atlas
                                        );
                                        catalogEntries.Add(spriteAddressableEntry);
                                    }
                                }
                            }

                            // Get provider type from schema, fallback to default if null
                            Type groupRootAssetEntryProviderType = schema.GroupRootAssetEntryProviderType.Value ?? typeof(GroupRootAssetEntryProvider);

                            ContentCatalogDataEntry addressableEntry = new ContentCatalogDataEntry(
                                convertedType, //make sure we convert our editor types to runtime types
                                asset.key,
                                groupRootAssetEntryProviderType.FullName,
                                new List<string>() { asset.key, asset.guid },
                                new List<string>() { rootAsset.name }
                            );

                            addressableEntry.Keys.AddRange(asset.labels);
                            catalogEntries.Add(addressableEntry);
                        }
                    }
                }
            }

            string catalogName = catalogId;

            var aaSettings = aaContext.Settings;
            string localLoadPath = "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/" + catalogId;
            string localBuildPath = Addressables.BuildPath;
            var remoteBuildPath = aaSettings.RemoteCatalogBuildPath.GetValue(aaSettings);
            var remoteLoadPath = aaSettings.RemoteCatalogLoadPath.GetValue(aaSettings);
            var catalogPathConfig = new CatalogPathConfig()
            {
                BuildPath = localBuildPath,
                LoadPath = localLoadPath,
                RemoteBuildPath = remoteBuildPath, //should be empty for now
                RemoteLoadPath = remoteLoadPath, //should be empty for now
                RuntimeCatalogFilename = catalogName,
                VersionedCatalogFileName = catalogName,
            };

#if ENABLE_JSON_CATALOG
            catalogBuilder = new JsonCatalogBuilder();
#else
            catalogBuilder = new BinaryCatalogBuilder();
#endif
            ContentCatalogData catalogData = catalogBuilder.GenerateCatalog(
                                    builderInput.Logger,
                                    catalogPathConfig,
                                    catalogId,
                                    catalogEntries,
                                    aaContext.runtimeData.CatalogLocations,
                                    aaContext.providerTypes,
                                    builderInput.Registry,
                                    buildHash,
                                    aaContext.Settings.BuildRemoteCatalog,
                                    aaContext.Settings.CatalogRequestsTimeout);
            return catalogData;
        }

        /// <summary>
        /// Generates a content update for Addressable assets based on the provided input and context.
        /// </summary>
        /// <param name="builderInput">The input parameters for the Addressables data builder, including build settings and options.</param>
        /// <param name="aaContext">The build context containing information about the Addressable assets and their dependencies.</param>
        /// <param name="extractData">The task responsible for extracting data required for the content update process.</param>
        /// <param name="cachedState">A list of cached asset states representing the previous build's asset information.</param>
        /// <param name="addrResult">The result object that will store the outcome of the Addressables player build process.</param>
        public void GenerateContentUpdate(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext, ExtractDataTask extractData, List<CachedAssetState> cachedState, AddressablesPlayerBuildResult addrResult)
        {
        }

        /// <summary>
        /// Relocates the metadata hash file to a known location for player build type stripping purposes
        /// </summary>
        /// <param name="builderInput">The input parameters for the Addressables data builder, including build settings and options.</param>
        /// <param name="aaContext">The build context containing information about the Addressable assets and their dependencies.</param>
        /// <param name="contentCatalog">Content catalog data of the build.</param>
        public void GenerateTypeStrippingInfo(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext, ContentCatalogData contentCatalog)
        {
            if (!Directory.Exists(m_MetadataDirectoryPath))
                return;

            string scriptsOnlyCacheMetadataPath = Path.Combine(m_MetadataDirectoryPath, "ScriptsOnlyCache.yaml");
            string platformSpecificCachePath = aaContext.Settings.GetContentStateBuildPath();
            string scriptsOnlyCacheBuildPath = Path.Combine(platformSpecificCachePath, "ScriptsOnlyCache.yaml");
            if (File.Exists(scriptsOnlyCacheBuildPath))
                File.Delete(scriptsOnlyCacheBuildPath);
            string directory = Path.GetDirectoryName(scriptsOnlyCacheBuildPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            FileUtil.CopyFileOrDirectory(scriptsOnlyCacheMetadataPath, scriptsOnlyCacheBuildPath);
            builderInput.Registry.AddFile(scriptsOnlyCacheBuildPath);
        }

        /// <summary>
        /// Initializes the build context and prepares internal data structures for processing.
        /// </summary>
        /// <param name="aaContext">The <see cref="AddressableAssetsBuildContext"/> containing the build context information.</param>
        /// <param name="dataBuilder">The <see cref="IDataBuilder"/> responsible for managing the data building process.</param>
        public void Init(AddressableAssetsBuildContext aaContext, IDataBuilder dataBuilder)
        {
            m_BuiltData = false;
            m_CatalogIdToRootAssetMap.Clear();
            m_RootAssetToGroupMap.Clear();
            m_RootAssetToAssetPathMap.Clear();
            m_CatalogIdToLoadPathMap.Clear();
            m_SpriteAtlasKeyToSprites.Clear();
            m_ContentDirectoryFilePaths.Clear();
            WebGLContentDirectoryManifest.ClearManifest();

            CreateContentDirectoryBuildFolder();
            m_CatalogIdToRootAssetMap.Clear();
            m_RootAssetToGroupMap.Clear();
            m_RootAssetToAssetPathMap.Clear();
            m_CatalogIdToLoadPathMap.Clear();
        }

        /// <summary>
        /// Determines whether the data has been successfully built and is available for use.
        /// </summary>
        /// <returns><see langword="true"/> if the data is considered built and the catalog file exists; otherwise, <see
        /// langword="false"/>.</returns>
        public bool IsDataBuilt()
        {
            if (!m_BuiltData)
            {
                return true;
            }
            return !string.IsNullOrEmpty(m_CatalogBuildPath) && File.Exists(m_CatalogBuildPath);
        }

        Dictionary<string, List<AddressableAssetEntry>> m_SpriteAtlasKeyToSprites = new Dictionary<string, List<AddressableAssetEntry>>();

        /// <summary>
        /// Processes the specified group schema.
        /// </summary>
        /// <param name="schema">The schema to process. Must be of type <see cref="ContentDirectoryGroupSchema"/>.</param>
        /// <param name="assetGroup">The Addressable Asset Group associated with the schema.</param>
        /// <param name="aaContext">The build context containing settings and other relevant data for the Addressable system.</param>
        /// <returns>An empty string if no error was encountered, otherwise it returns the error.</returns>
        public string ProcessGroupSchema(AddressableAssetGroupSchema schema, AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            if (schema is not ContentDirectoryGroupSchema contentDirectoryGroupSchema || !contentDirectoryGroupSchema.IncludeInBuild || !contentDirectoryGroupSchema.IsEnabled)
                return "";

            GroupRootAsset directoryAsset = ScriptableObject.CreateInstance<GroupRootAsset>();
            directoryAsset.Key = assetGroup.Guid;

            string assetPath = $"{RootAssetBuildPath}/{assetGroup.Name}_RootAsset.asset";
            string catalogId = schema.CatalogId;

            if (!m_CatalogIdToRootAssetMap.ContainsKey(catalogId))
            {
                m_CatalogIdToRootAssetMap[catalogId] = new List<Object>();
                m_CatalogIdToRootAssetsPathMap[catalogId] = new List<string>();
            }

            // Gather all assets including subassets and sprites for sprite atlas
            var allEntries = new List<AddressableAssetEntry>();
            foreach (var entry in assetGroup.entries)
            {
                entry.GatherAllAssets(allEntries, includeSelf: true, recurseAll: true, includeSubObjects: true);
            }

            foreach (var entry in allEntries)
            {
                var targetAsset = entry.TargetAsset;
                if (targetAsset == null)
                    continue;

                if (entry.ParentEntry != null && entry.ParentEntry.TargetAsset.GetType() == typeof(SpriteAtlas))
                {
                    var parentKey = entry.ParentEntry.address;
                    if (m_SpriteAtlasKeyToSprites.ContainsKey(parentKey))
                    {
                        m_SpriteAtlasKeyToSprites[parentKey].Add(entry);
                    }
                    else
                    {
                        m_SpriteAtlasKeyToSprites.Add(parentKey, new List<AddressableAssetEntry> { entry });
                    }
                    continue;
                }

                // For subassets, use parent's GUID since subassets don't have their own GUID
                string entryGuid = entry.guid;
                if (string.IsNullOrEmpty(entryGuid) && entry.IsSubAsset && entry.ParentEntry != null)
                {
                    entryGuid = entry.ParentEntry.guid;
                }
                var convertedType = AddressableAssetUtility.MapEditorTypeToRuntimeType(entry.MainAssetType, false);
                if (IsEditorTypeOrNull(convertedType))
                {
                    LogEditorTypeStrippedWarning(entry.MainAssetType, entry.AssetPath, isAssetPath: true);
                    continue;
                }

                if (entry.IsScene)
                    directoryAsset.Assets.Add(CreateLoadableSceneInfo(entry, entryGuid, convertedType));
                else
                    directoryAsset.Assets.Add(CreateLoadableInfo(entry, entryGuid, convertedType));
            }

            AssetDatabase.CreateAsset(directoryAsset, assetPath);

            m_CatalogIdToRootAssetMap[catalogId].Add(directoryAsset);
            m_CatalogIdToRootAssetsPathMap[catalogId].Add(assetPath);

            m_RootAssetToGroupMap[directoryAsset] = assetGroup;
            m_RootAssetToAssetPathMap[directoryAsset] = assetPath;

            m_CatalogIdToLoadPathMap[catalogId] = contentDirectoryGroupSchema.LoadPath.GetValue(aaContext.Settings);

            return string.Empty;
        }

        void CreateContentDirectoryBuildFolder()
        {
            if (Directory.Exists(RootAssetBuildPath))
                Directory.Delete(RootAssetBuildPath, true);
            Directory.CreateDirectory(RootAssetBuildPath);
            AssetDatabase.ImportAsset(RootAssetBuildPath, ImportAssetOptions.ForceSynchronousImport);
        }

        LoadableInfo CreateLoadableSceneInfo(AddressableAssetEntry entry, string entryGuid, Type convertedType)
        {
            var labels = new List<string>(entry.labels);
            var loadableInfo = new LoadableInfo
            {
                key = entry.address,
                guid = entryGuid,
                labels = labels,
                type = convertedType,
                loadable = null,
                scene = LoadableSceneIdEditorUtility.CreateLoadableSceneId(entry.AssetPath)
            };
            return loadableInfo;
        }

        LoadableInfo CreateLoadableInfo(AddressableAssetEntry entry, string entryGuid, Type convertedType)
        {
            // Use TargetAsset instead of MainAsset to properly reference subassets (e.g., Sprites from a Texture2D)
            // MainAsset returns the parent asset, while TargetAsset returns the actual subasset
            var labels = new List<string>(entry.labels);

            var address = entry.address;
            var targetAsset = entry.TargetAsset ?? entry.MainAsset;
            if (entry.MainAsset != entry.TargetAsset && entry.MainAssetType != typeof(SpriteAtlas))
            {
                labels.Add($"{entryGuid}[{targetAsset.name}]");
            }

            var loadableRef = LoadableObjectIdEditorUtility.CreateLoadableObjectId(targetAsset);
            var loadableInfo = new LoadableInfo
            {
                key = address,
                guid = entryGuid,
                labels = labels,
                type = convertedType,
                loadable = new Loadable<Object>(loadableRef),
                scene = default
            };
            return loadableInfo;
        }
    }
}
#endif
