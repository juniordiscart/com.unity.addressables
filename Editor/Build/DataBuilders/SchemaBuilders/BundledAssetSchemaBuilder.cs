using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.CatalogBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using static UnityEditor.AddressableAssets.Build.ContentUpdateScript;

namespace UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders
{
    /// <summary>
    /// Schema builder that processes BundledAssetGroupSchema to build AssetBundles from Addressable groups.
    /// Handles bundle packing, naming, post-processing, and catalog generation for bundled assets.
    /// </summary>
    public class BundledAssetSchemaBuilder : ISchemaBuilder
    {
        /// <summary>
        /// The extension to use for type tree data files when type tree data extraction is enabled.
        /// </summary>
        public const string kTypeTreeDataExtension = ".typetreedata";
        /// <summary>
        /// The file name to use for type tree data when type tree data extraction is enabled.
        /// This file will be moved to the catalog build path with a hash as the file name during the build.
        /// </summary>
        public const string kTypeTreeDataFileName = "AssetBundle" + kTypeTreeDataExtension;

        private bool m_BuiltData;

        BuildScriptSchemaDriven m_DataBuilder;

        List<AssetBundleBuild> m_AllBundleInputDefs;
        HashSet<string> m_CreatedProviderIds;
        Dictionary<AddressableAssetGroup, (string, string)[]> m_GroupToBundleNames;
        Dictionary<string, string> m_BundleToInternalId;
        private Dictionary<string, List<ContentCatalogDataEntry>> m_PrimaryKeyToDependers;
        private Dictionary<string, ContentCatalogDataEntry> m_PrimaryKeyToLocation;
        private Dictionary<string, List<ContentCatalogDataEntry>> GetPrimaryKeyToDependerLocations(List<ContentCatalogDataEntry> locations)
        {
            if (m_PrimaryKeyToDependers != null)
                return m_PrimaryKeyToDependers;
            if (locations == null || locations.Count == 0)
            {
                Debug.LogError("Attempting to get Entries dependent on key, but currently no locations");
                return new Dictionary<string, List<ContentCatalogDataEntry>>(0);
            }

            m_PrimaryKeyToDependers = new Dictionary<string, List<ContentCatalogDataEntry>>(locations.Count);
            foreach (ContentCatalogDataEntry location in locations)
            {
                for (int i = 0; i < location.Dependencies.Count; ++i)
                {
                    string dependencyKey = location.Dependencies[i] as string;
                    if (string.IsNullOrEmpty(dependencyKey))
                        continue;

                    if (!m_PrimaryKeyToDependers.TryGetValue(dependencyKey, out var dependers))
                    {
                        dependers = new List<ContentCatalogDataEntry>();
                        m_PrimaryKeyToDependers.Add(dependencyKey, dependers);
                    }

                    dependers.Add(location);
                }
            }

            return m_PrimaryKeyToDependers;
        }
        private Dictionary<string, ContentCatalogDataEntry> GetPrimaryKeyToLocation(List<ContentCatalogDataEntry> locations)
        {
            if (m_PrimaryKeyToLocation != null)
                return m_PrimaryKeyToLocation;
            if (locations == null || locations.Count == 0)
            {
                Debug.LogError("Attempting to get Primary key to entries dependent on key, but currently no locations");
                return new Dictionary<string, ContentCatalogDataEntry>();
            }

            m_PrimaryKeyToLocation = new Dictionary<string, ContentCatalogDataEntry>();
            foreach (var loc in locations)
            {
                if (loc != null && loc.Keys[0] != null && loc.Keys[0] is string && !m_PrimaryKeyToLocation.ContainsKey((string)loc.Keys[0]))
                    m_PrimaryKeyToLocation[(string)loc.Keys[0]] = loc;
            }

            return m_PrimaryKeyToLocation;
        }


        private string m_CatalogBuildPath;
        private string m_BuiltTypeTreeDataPath;
        UnityEditor.Build.Pipeline.Utilities.LinkXmlGenerator m_Linker;

        /// <inheritdoc/>
        public string Name => "Bundled Assets";

        /// <inheritdoc/>
        public bool CanBuildSchema(AddressableAssetGroupSchema schema)
        {
            return schema is BundledAssetGroupSchema;
        }

        /// <inheritdoc/>
        public void Init(AddressableAssetsBuildContext aaContext, IDataBuilder dataBuilder)
        {
            m_BuiltData = false;

            m_DataBuilder = dataBuilder as BuildScriptSchemaDriven;

            m_AllBundleInputDefs = new List<AssetBundleBuild>();
            m_CreatedProviderIds = new HashSet<string>();
            m_GroupToBundleNames = new Dictionary<AddressableAssetGroup, (string, string)[]>();
            m_BundleToInternalId = new Dictionary<string, string>();

            // force these caches to be rebuilt
            m_PrimaryKeyToDependers = null;
            m_PrimaryKeyToLocation = null;
            m_CatalogBuildPath = null;
            m_BuiltTypeTreeDataPath = null;

            m_Linker = UnityEditor.Build.Pipeline.Utilities.LinkXmlGenerator.CreateDefault();
            m_Linker.AddAssemblies(new[] { typeof(Addressables).Assembly, typeof(UnityEngine.ResourceManagement.ResourceManager).Assembly });
            m_Linker.AddTypes(aaContext.Settings.CertificateHandlerType);
        }

        struct SBPSettingsOverwriterScope : IDisposable
        {
            bool m_PrevSlimResults;


            public SBPSettingsOverwriterScope(bool forceFullWriteResults)
            {
                m_PrevSlimResults = ScriptableBuildPipeline.slimWriteResults;
                if (forceFullWriteResults)
                    ScriptableBuildPipeline.slimWriteResults = false;
            }

            public void Dispose()
            {
                ScriptableBuildPipeline.slimWriteResults = m_PrevSlimResults;
            }
        }


        internal static string GetBuiltInBundleNamePrefix(AddressableAssetsBuildContext aaContext)
        {
            return GetBuiltInBundleNamePrefix(aaContext.Settings);
        }

        internal static string GetBuiltInBundleNamePrefix(AddressableAssetSettings settings)
        {
            string value = "";
            switch (settings.BuiltInBundleNaming)
            {
                case BuiltInBundleNaming.DefaultGroupGuid:
                    value = settings.DefaultGroup.Guid;
                    break;
                case BuiltInBundleNaming.ProjectName:
                    value = Hash128.Compute(GetProjectName()).ToString();
                    break;
                case BuiltInBundleNaming.Custom:
                    value = settings.BuiltInBundleCustomNaming;
                    break;
            }

            return value;
        }

        void AddBundleProvider(AddressableAssetsBuildContext aaContext, BundledAssetGroupSchema schema)
        {
            var bundleProviderId = schema.GetBundleCachedProviderId();

            if (!m_CreatedProviderIds.Contains(bundleProviderId))
            {
                m_CreatedProviderIds.Add(bundleProviderId);
                var bundleProviderType = schema.AssetBundleProviderType.Value;
                aaContext.providerTypes.Add(bundleProviderType);
            }
        }

        internal static string GetMonoScriptBundleNamePrefix(AddressableAssetsBuildContext aaContext)
        {
            return GetMonoScriptBundleNamePrefix(aaContext.Settings);
        }

        internal static string GetMonoScriptBundleNamePrefix(AddressableAssetSettings settings)
        {
            string value = null;
            switch (settings.MonoScriptBundleNaming)
            {
                case MonoScriptBundleNaming.ProjectName:
                    value = Hash128.Compute(GetProjectName()).ToString();
                    break;
                case MonoScriptBundleNaming.DefaultGroupGuid:
                    value = settings.DefaultGroup.Guid;
                    break;
                case MonoScriptBundleNaming.Custom:
                    value = settings.MonoScriptBundleCustomNaming;
                    break;
            }

            return value;
        }

        /// <inheritdoc/>
        public void Build(BuildContext buildContext,
            AddressablesDataBuilderInput builderInput,
            AddressableAssetsBuildContext aaContext,
            ExtractDataTask extractData,
            List<CachedAssetState> cachedState,
            AddressablesPlayerBuildResult addrResult)
        {
            if (m_AllBundleInputDefs.Count > 0)
            {
                aaContext.ContainsAssetBundleData = true;

                var buildTarget = builderInput.Target;
                var buildTargetGroup = builderInput.TargetGroup;

                var buildParams = new AddressableAssetsBundleBuildParameters(
                    aaContext.Settings,
                    aaContext.bundleToAssetGroup,
                    buildTarget,
                    buildTargetGroup,
                    aaContext.Settings.buildSettings.bundleBuildPath);

                var builtinBundleName = GetBuiltInBundleNamePrefix(aaContext) + $"{BuildScriptBase.BuiltInBundleBaseName}.bundle";

                string typeTreeDataBuildPath = null;

#if UNITY_6000_5_OR_NEWER
                if (aaContext.Settings.ExtractTypeTreeData)
                {
                    buildParams.ContentBuildFlags |= UnityEditor.Build.Content.ContentBuildFlags.ExtractTypeTree;
                    typeTreeDataBuildPath = Path.Combine(aaContext.Settings.buildSettings.bundleBuildPath, kTypeTreeDataFileName);
                }
#endif

                string monoScriptBundleName = GetMonoScriptBundleNamePrefix(aaContext);
                if (!string.IsNullOrEmpty(monoScriptBundleName))
                    monoScriptBundleName += "_monoscripts.bundle";
                var buildTasks = RuntimeDataBuildTasks(builtinBundleName, monoScriptBundleName, typeTreeDataBuildPath);
                buildTasks.Add(extractData);

                IBundleBuildResults results;
                using (builderInput.Logger.ScopedStep(LogLevel.Info, "ContentPipeline.BuildAssetBundles"))
                using (new SBPSettingsOverwriterScope(ProjectConfigData.GenerateBuildLayout)) // build layout generation requires full SBP write results
                {
                    var buildContent = new BundleBuildContent(m_AllBundleInputDefs);
                    var exitCode = ContentPipeline.BuildAssetBundles(buildContext, buildParams, buildContent, out results, buildTasks, aaContext, builderInput.Logger);

                    if (exitCode < ReturnCode.Success)
                        throw new Exception("SBP Error" + exitCode);
                }

#if UNITY_6000_5_OR_NEWER
                if (aaContext.Settings.ExtractTypeTreeData && File.Exists(typeTreeDataBuildPath))
                    MoveFileToDestinationWithTimestampIfDifferent(typeTreeDataBuildPath, Path.Combine(Addressables.BuildPath, kTypeTreeDataFileName), builderInput.Logger);
#endif

                var groups = new List<AddressableAssetGroup>(aaContext.Settings.groups.Count);
                for (var i = 0; i < aaContext.Settings.groups.Count; i++)
                {
                    var g = aaContext.Settings.groups[i];
                    if (g != null)
                        groups.Add(g);
                }

                var postCatalogUpdateCallbacks = new List<Action>();
                using (builderInput.Logger.ScopedStep(LogLevel.Info, "PostProcessBundles"))
                using (var progressTracker = new UnityEditor.Build.Pipeline.Utilities.ProgressTracker())
                {
                    progressTracker.UpdateTask("Post Processing AssetBundles");

                    AddressableAssetGroup sharedBundleGroup = aaContext.Settings.GetSharedBundleGroup();
                    foreach (var assetGroup in groups)
                    {
                        if (!aaContext.assetGroupToBundles.ContainsKey(assetGroup))
                            continue;

                        using (builderInput.Logger.ScopedStep(LogLevel.Info, assetGroup.name))
                        {
                            PostProcessBundles(assetGroup, results, addrResult,
                                builderInput.Registry, aaContext, builderInput.Logger,
                                postCatalogUpdateCallbacks, sharedBundleGroup);
                        }
                    }
                }

                using (builderInput.Logger.ScopedStep(LogLevel.Info, "Process Catalog Entries"))
                {
                    Dictionary<string, ContentCatalogDataEntry> locationIdToCatalogEntryMap = BuildLocationIdToCatalogEntryMap(aaContext.locations);
                    ContentUpdateContext contentUpdateContext = default;
                    if (builderInput.PreviousContentState != null)
                    {
                        contentUpdateContext = new ContentUpdateContext()
                        {
                            BundleToInternalBundleIdMap = m_BundleToInternalId,
                            GuidToPreviousAssetStateMap = BuildGuidToCachedAssetStateMap(builderInput.PreviousContentState, aaContext.Settings),
                            IdToCatalogDataEntryMap = locationIdToCatalogEntryMap,
                            WriteData = extractData.WriteData,
                            ContentState = builderInput.PreviousContentState,
                            Registry = builderInput.Registry,
                            PreviousAssetStateCarryOver = cachedState
                        };
                    }
                    ProcessCatalogEntriesForBuild(aaContext, groups, builderInput, extractData.WriteData,
                        contentUpdateContext, m_BundleToInternalId, locationIdToCatalogEntryMap);
                    foreach (var postUpdateCatalogCallback in postCatalogUpdateCallbacks)
                        postUpdateCatalogCallback.Invoke();

                    foreach (var r in results.WriteResults)
                    {
                        var resultValue = r.Value;
                        m_Linker.AddTypes(resultValue.includedTypes);
                        m_Linker.AddSerializedClass(resultValue.includedSerializeReferenceFQN);
                    }
                }
                m_BuiltData = true;
            }
        }


        private static void ProcessCatalogEntriesForBuild(AddressableAssetsBuildContext aaContext,
IEnumerable<AddressableAssetGroup> validGroups, AddressablesDataBuilderInput builderInput, IBundleWriteData writeData,
ContentUpdateContext contentUpdateContext, Dictionary<string, string> bundleToInternalId, Dictionary<string, ContentCatalogDataEntry> locationIdToCatalogEntryMap)
        {
            using (var progressTracker = new UnityEditor.Build.Pipeline.Utilities.ProgressTracker())
            {
                progressTracker.UpdateTask("Post Processing Catalog Entries");
                if (builderInput.PreviousContentState != null)
                {
                    RevertUnchangedAssetsToPreviousAssetState.Run(aaContext, contentUpdateContext);
                }
                else
                {
                    foreach (var assetGroup in validGroups)
                        SetAssetEntriesBundleFileIdToCatalogEntryBundleFileId(assetGroup.entries, bundleToInternalId, writeData, locationIdToCatalogEntryMap);
                }
            }

            bundleToInternalId.Clear();
        }


        private static Dictionary<string, ContentCatalogDataEntry> BuildLocationIdToCatalogEntryMap(List<ContentCatalogDataEntry> locations)
        {
            Dictionary<string, ContentCatalogDataEntry> locationIdToCatalogEntryMap = new Dictionary<string, ContentCatalogDataEntry>();
            foreach (var location in locations)
                locationIdToCatalogEntryMap[location.InternalId] = location;

            return locationIdToCatalogEntryMap;
        }

        private static Dictionary<string, CachedAssetState> BuildGuidToCachedAssetStateMap(AddressablesContentState contentState, AddressableAssetSettings settings)
        {
            Dictionary<string, CachedAssetState> addressableEntryToCachedStateMap = new Dictionary<string, CachedAssetState>();
            foreach (var cachedInfo in contentState.cachedInfos)
                addressableEntryToCachedStateMap[cachedInfo.asset.guid.ToString()] = cachedInfo;

            return addressableEntryToCachedStateMap;
        }


        internal static string GetProjectName()
        {
            return new DirectoryInfo(Path.GetDirectoryName(Application.dataPath)).Name;
        }

        internal static void SetAssetEntriesBundleFileIdToCatalogEntryBundleFileId(ICollection<AddressableAssetEntry> assetEntries, Dictionary<string, string> bundleNameToInternalBundleIdMap,
IBundleWriteData writeData, Dictionary<string, ContentCatalogDataEntry> locationIdToCatalogEntryMap)
        {
            foreach (var loc in assetEntries)
            {
                AddressableAssetEntry processedEntry = loc;
                if (loc.IsFolder && loc.SubAssets.Count > 0)
                    processedEntry = loc.SubAssets[0];
                GUID guid = new GUID(processedEntry.guid);
                //For every entry in the write data we need to ensure the BundleFileId is set so we can save it correctly in the cached state
                if (writeData.AssetToFiles.TryGetValue(guid, out List<string> files))
                {
                    string file = files[0];
                    string fullBundleName = writeData.FileToBundle[file];
                    string convertedLocation;

                    if (!bundleNameToInternalBundleIdMap.TryGetValue(fullBundleName, out convertedLocation))
                    {
                        Debug.LogWarning($"Unable to find bundleId for key: {fullBundleName}.");
                        continue;
                    }

                    if (locationIdToCatalogEntryMap.TryGetValue(convertedLocation,
                        out ContentCatalogDataEntry catalogEntry))
                    {
                        loc.BundleFileId = catalogEntry.InternalId;

                        //This is where we strip out the temporary hash added to the bundle name for Content Update for the AssetEntry
                        if (loc.parentGroup?.GetSchema<BundledAssetGroupSchema>()?.BundleNaming ==
                            BundledAssetGroupSchema.BundleNamingStyle.NoHash)
                        {
                            loc.BundleFileId = StripHashFromBundleLocation(loc.BundleFileId);
                        }
                    }
                }
            }
        }

        static string StripHashFromBundleLocation(string hashedBundleLocation)
        {
            return hashedBundleLocation.Remove(hashedBundleLocation.LastIndexOf('_')) + ".bundle";
        }

        /// <inheritdoc/>
        public string ProcessGroupSchema(AddressableAssetGroupSchema schema,
            AddressableAssetGroup assetGroup,
            AddressableAssetsBuildContext aaContext)
        {
            // we have to do this to keep support for protected methods
            return m_DataBuilder.ProcessLegacyMethodBundledAssetSchema(schema as BundledAssetGroupSchema, assetGroup, aaContext);
        }

        /// <summary>
        /// The processing of the bundled asset schema.  This is where the bundle(s) for a given group are actually setup.
        /// </summary>
        /// <param name="schema">The BundledAssetGroupSchema to process</param>
        /// <param name="assetGroup">The group this schema was pulled from</param>
        /// <param name="aaContext">The general Addressables build builderInput</param>
        /// <param name="includedGroupsInBuild">List to which processed groups are added for tracking.</param>
        /// <returns>The error string, if any.</returns>
        public virtual string ProcessBundledAssetSchema(
            BundledAssetGroupSchema schema,
            AddressableAssetGroup assetGroup,
            AddressableAssetsBuildContext aaContext,
            List<AddressableAssetGroup> includedGroupsInBuild)
        {
            if (schema == null || !schema.IncludeInBuild || !schema.IsEnabled || !assetGroup.entries.Any())
                return string.Empty;

            includedGroupsInBuild?.Add(assetGroup);

            AddBundleProvider(aaContext, schema);

            var assetProviderId = schema.GetAssetCachedProviderId();
            if (!m_CreatedProviderIds.Contains(assetProviderId))
            {
                m_CreatedProviderIds.Add(assetProviderId);
                var assetProviderType = schema.BundledAssetProviderType.Value;
                aaContext.providerTypes.Add(assetProviderType);
            }

            string buildPath = schema.BuildPath.GetValue(aaContext.Settings);
            if (buildPath == AddressableAssetProfileSettings.undefinedEntryValue)
                return ($"Addressable group {assetGroup.Name} build path is set to undefined. Change the path to build content.");

            string loadPath = schema.LoadPath.GetValue(aaContext.Settings);
            if (loadPath == AddressableAssetProfileSettings.undefinedEntryValue)
                Addressables.LogWarning($"Addressable group {assetGroup.Name} load path is set to undefined. Change the path to load content.");

            if (loadPath.StartsWith("http://", StringComparison.Ordinal) && PlayerSettings.insecureHttpOption == InsecureHttpOption.NotAllowed)
                Addressables.LogWarning($"Addressable group {assetGroup.Name} uses insecure http for its load path.  To allow http connections for UnityWebRequests, change your settings in Edit > Project Settings > Player > Other Settings > Configuration > Allow downloads over HTTP.");

            if (schema.Compression == BundledAssetGroupSchema.BundleCompressionMode.LZMA && aaContext.runtimeData.BuildTarget == BuildTarget.WebGL.ToString())
                Addressables.LogWarning($"Addressable group {assetGroup.Name} uses LZMA compression, which cannot be decompressed on WebGL. Use LZ4 compression instead.");

            var bundleInputDefs = new List<AssetBundleBuild>();
            var list = BuildScriptSchemaDriven.PrepGroupBundlePacking(assetGroup, bundleInputDefs, schema);
            aaContext.assetEntries.AddRange(list);
            List<string> uniqueNames = HandleBundleNames(bundleInputDefs, aaContext.bundleToAssetGroup, assetGroup.Guid);
            (string, string)[] groupBundles = new (string, string)[uniqueNames.Count];
            for (int i = 0; i < uniqueNames.Count; ++i)
                groupBundles[i] = (bundleInputDefs[i].assetBundleName, uniqueNames[i]);
            m_GroupToBundleNames.Add(assetGroup, groupBundles);
            m_AllBundleInputDefs.AddRange(bundleInputDefs);
            return string.Empty;
        }

        internal static List<string> HandleBundleNames(List<AssetBundleBuild> bundleInputDefs, Dictionary<string, string> bundleToAssetGroup = null, string assetGroupGuid = null)
        {
            var generatedUniqueNames = new List<string>();
            var handledNames = new HashSet<string>();

            for (int i = 0; i < bundleInputDefs.Count; i++)
            {
                AssetBundleBuild bundleBuild = bundleInputDefs[i];
                string assetBundleName = bundleBuild.assetBundleName;
                if (handledNames.Contains(assetBundleName))
                {
                    int count = 1;
                    var newName = assetBundleName;
                    while (handledNames.Contains(newName) && count < 1000)
                        newName = assetBundleName.Replace(".bundle", string.Format("{0}.bundle", count++));
                    assetBundleName = newName;
                }

                string hashedAssetBundleName = HashingMethods.Calculate(assetBundleName) + ".bundle";
                generatedUniqueNames.Add(assetBundleName);
                handledNames.Add(assetBundleName);

                bundleBuild.assetBundleName = hashedAssetBundleName;
                bundleInputDefs[i] = bundleBuild;

                if (bundleToAssetGroup != null)
                    bundleToAssetGroup.Add(hashedAssetBundleName, assetGroupGuid);
            }

            return generatedUniqueNames;
        }

        // Tests can set this flag to prevent player script compilation. This is the most expensive part of small builds
        // and isn't needed for most tests.
        internal static bool s_SkipCompilePlayerScripts = false;

        // QUESTION: Do we want to make this public?
        IList<IBuildTask> RuntimeDataBuildTasks(string builtinBundleName, string monoScriptBundleName, string typeTreeExtractionPath)
        {
            var buildTasks = new List<IBuildTask>();

            // Setup
            buildTasks.Add(new SwitchToBuildPlatform());
            buildTasks.Add(new RebuildSpriteAtlasCache());

            // Player Scripts
            if (!s_SkipCompilePlayerScripts)
                buildTasks.Add(new BuildPlayerScripts());
            buildTasks.Add(new PostScriptsCallback());

            // Dependency
            buildTasks.Add(new CalculateSceneDependencyData());
            buildTasks.Add(new CalculateAssetDependencyData());
            buildTasks.Add(new AddHashToBundleNameTask());
            buildTasks.Add(new StripUnusedSpriteSources());
            buildTasks.Add(new CreateBuiltInBundle(builtinBundleName));
            if (!string.IsNullOrEmpty(monoScriptBundleName))
                buildTasks.Add(new CreateMonoScriptBundle(monoScriptBundleName));
            buildTasks.Add(new PostDependencyCallback());

            // Packing
            buildTasks.Add(new GenerateBundlePacking());
            buildTasks.Add(new UpdateBundleObjectLayout());
            buildTasks.Add(new GenerateBundleCommands());
            buildTasks.Add(new GenerateSubAssetPathMaps());
            buildTasks.Add(new GenerateBundleMaps());
            buildTasks.Add(new PostPackingCallback());

            // Writing
            buildTasks.Add(new WriteSerializedFiles());
            buildTasks.Add(new ArchiveAndCompressBundles());
            buildTasks.Add(new GenerateLocationListsTask());
#if UNITY_6000_5_OR_NEWER
            if(!string.IsNullOrEmpty(typeTreeExtractionPath))
                buildTasks.Add(new CombineExtractedTypeTreeData { OutputPath = typeTreeExtractionPath });
#endif
            buildTasks.Add(new PostWritingCallback());

            return buildTasks;
        }

        static void MoveFileToDestinationWithTimestampIfDifferent(string srcPath, string destPath, IBuildLogger log)
        {
            if (srcPath == destPath)
                return;

            DateTime time = File.GetLastWriteTime(srcPath);
            DateTime destTime = File.Exists(destPath) ? File.GetLastWriteTime(destPath) : new DateTime();

            if (destTime == time)
                return;

            using (log.ScopedStep(LogLevel.Verbose, "Move File", $"{srcPath} -> {destPath}"))
            {
                var directory = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                else if (File.Exists(destPath))
                    File.Delete(destPath);
                File.Move(srcPath, destPath);
            }
        }






        void PostProcessBundles(AddressableAssetGroup assetGroup, IBundleBuildResults buildResult, AddressablesPlayerBuildResult addrResult, FileRegistry registry,
    AddressableAssetsBuildContext aaContext, IBuildLogger logger, List<Action> postCatalogUpdateCallbacks, AddressableAssetGroup sharedBundleGroup)
        {
            var schema = assetGroup.GetSchema<BundledAssetGroupSchema>();
            if (schema == null || !schema.IsEnabled)
                return;

            var path = schema.BuildPath.GetValue(assetGroup.Settings);
            if (string.IsNullOrEmpty(path))
                return;

            List<string> builtBundleNames = aaContext.assetGroupToBundles[assetGroup];
            List<string> outputBundleNames = null;

            if (m_GroupToBundleNames.TryGetValue(assetGroup, out (string, string)[] bundleValues))
            {
                outputBundleNames = new List<string>(builtBundleNames.Count);
                for (int i = 0; i < builtBundleNames.Count; ++i)
                {
                    string outputName = null;
                    foreach ((string, string) bundleValue in bundleValues)
                    {
                        if (schema.BundleMode == BundledAssetGroupSchema.BundlePackingMode.PackSeparately ||
                            assetGroup.Settings.UniqueBundleIds)
                        {
                            if (builtBundleNames[i].StartsWith(bundleValue.Item1, StringComparison.Ordinal))
                                outputName = bundleValue.Item2;
                        }
                        else if (builtBundleNames[i].Equals(bundleValue.Item1, StringComparison.Ordinal))
                            outputName = bundleValue.Item2;

                        if (outputName != null)
                            break;
                    }
                    outputBundleNames.Add(string.IsNullOrEmpty(outputName) ? builtBundleNames[i] : outputName);
                }
            }
            else
            {
                outputBundleNames = new List<string>(builtBundleNames);
            }

            for (int i = 0; i < builtBundleNames.Count; ++i)
            {
                AddressablesPlayerBuildResult.BundleBuildResult bundleResultInfo = new AddressablesPlayerBuildResult.BundleBuildResult();
                bundleResultInfo.SourceAssetGroup = assetGroup;
                bundleResultInfo.CatalogName = ResourceManagerRuntimeData.kCatalogAddress;

                if (GetPrimaryKeyToLocation(aaContext.locations).TryGetValue(builtBundleNames[i], out ContentCatalogDataEntry dataEntry))
                {
                    var info = buildResult.BundleInfos[builtBundleNames[i]];
                    bundleResultInfo.Crc = info.Crc;
                    bundleResultInfo.Hash = info.Hash.ToString();
                    var bundleName = Path.GetFileNameWithoutExtension(info.FileName);
                    if (!schema.StripDownloadOptions)
                    {
                        dataEntry.Data = new AssetBundleRequestOptions
                        {
                            Crc = schema.UseAssetBundleCrc ? info.Crc : 0,
                            UseCrcForCachedBundle = schema.UseAssetBundleCrcForCachedBundles,
                            UseUnityWebRequestForLocalBundles = schema.UseUnityWebRequestForLocalBundles,
                            Hash = schema.UseAssetBundleCache ? info.Hash.ToString() : "",
                            ChunkedTransfer = schema.ChunkedTransfer,
                            RedirectLimit = schema.RedirectLimit,
                            RetryCount = schema.RetryCount,
                            Timeout = schema.Timeout,
                            BundleName = bundleName,
                            AssetLoadMode = schema.AssetLoadMode,
                            BundleSize = GetFileSize(info.FileName),
                            ClearOtherCachedVersionsWhenLoaded = schema.AssetBundledCacheClearBehavior == BundledAssetGroupSchema.CacheClearBehavior.ClearWhenWhenNewVersionLoaded
                        };
                    }
                    bundleResultInfo.InternalBundleName = bundleName;

                    if (assetGroup == sharedBundleGroup && info.Dependencies.Length == 0 && !string.IsNullOrEmpty(info.FileName) &&
                        (info.FileName.EndsWith($"{BuildScriptBase.BuiltInBundleBaseName}.bundle", StringComparison.Ordinal)
                         || info.FileName.EndsWith("_monoscripts.bundle", StringComparison.Ordinal)))
                    {
                        outputBundleNames[i] = m_DataBuilder.ConstructOutputName(null, schema, info, outputBundleNames[i]);
                    }
                    else
                    {
                        int extensionLength = Path.GetExtension(outputBundleNames[i]).Length;
                        string[] deconstructedBundleName = outputBundleNames[i].Substring(0, outputBundleNames[i].Length - extensionLength).Split('_');
                        string reconstructedBundleName = string.Join("_", deconstructedBundleName, 1, deconstructedBundleName.Length - 1) + ".bundle";
                        outputBundleNames[i] = m_DataBuilder.ConstructOutputName(assetGroup, schema, info, reconstructedBundleName);
                    }

                    dataEntry.InternalId = dataEntry.InternalId.Remove(dataEntry.InternalId.Length - builtBundleNames[i].Length) + outputBundleNames[i];
                    SetPrimaryKey(dataEntry, outputBundleNames[i], aaContext);

                    if (!m_BundleToInternalId.ContainsKey(builtBundleNames[i]))
                        m_BundleToInternalId.Add(builtBundleNames[i], dataEntry.InternalId);

                    if (dataEntry.InternalId.StartsWith("http:\\", StringComparison.Ordinal))
                        dataEntry.InternalId = dataEntry.InternalId.Replace("http:\\", "http://").Replace("\\", "/");
                    else if (dataEntry.InternalId.StartsWith("https:\\", StringComparison.Ordinal))
                        dataEntry.InternalId = dataEntry.InternalId.Replace("https:\\", "https://").Replace("\\", "/");
                }
                else
                {
                    Debug.LogWarningFormat("Unable to find ContentCatalogDataEntry for bundle {0}.", outputBundleNames[i]);
                }

                var targetPath = Path.Combine(path, outputBundleNames[i]);
                bundleResultInfo.FilePath = targetPath;
                var srcPath = Path.Combine(assetGroup.Settings.buildSettings.bundleBuildPath, builtBundleNames[i]);

                var namingSchema = assetGroup.GetSchema<BundledAssetGroupSchema>();
                if (namingSchema != null && namingSchema.IsEnabled && namingSchema.BundleNaming == BundledAssetGroupSchema.BundleNamingStyle.NoHash)
                {
                    outputBundleNames[i] = StripHashFromBundleLocation(outputBundleNames[i]);
                    bundleResultInfo.FilePath = StripHashFromBundleLocation(bundleResultInfo.FilePath);
                }

                aaContext.internalToOutputBundleName.Add(builtBundleNames[i], outputBundleNames[i]);
                MoveFileToDestinationWithTimestampIfDifferent(srcPath, targetPath, logger);
                AddPostCatalogUpdatesInternal(assetGroup, postCatalogUpdateCallbacks, dataEntry, targetPath, registry);

                if (addrResult != null)
                    addrResult.AssetBundleBuildResults.Add(bundleResultInfo);

                registry.AddFile(targetPath);
            }
        }


        internal void AddPostCatalogUpdatesInternal(AddressableAssetGroup assetGroup, List<Action> postCatalogUpdates, ContentCatalogDataEntry dataEntry, string targetBundlePath,
FileRegistry registry)
        {
            var namingSchema = assetGroup.GetSchema<BundledAssetGroupSchema>();
            if (namingSchema != null && namingSchema.IsEnabled && namingSchema.BundleNaming == BundledAssetGroupSchema.BundleNamingStyle.NoHash)
            {
                postCatalogUpdates.Add(() =>
                {
                    //This is where we strip out the temporary hash for the final bundle location and filename
                    string bundlePathWithoutHash = StripHashFromBundleLocation(targetBundlePath);
                    if (File.Exists(targetBundlePath))
                    {
                        if (File.Exists(bundlePathWithoutHash))
                            File.Delete(bundlePathWithoutHash);
                        string destFolder = Path.GetDirectoryName(bundlePathWithoutHash);
                        if (!string.IsNullOrEmpty(destFolder) && !Directory.Exists(destFolder))
                            Directory.CreateDirectory(destFolder);

                        File.Move(targetBundlePath, bundlePathWithoutHash);
                    }

                    if (registry != null)
                    {
                        if (!registry.ReplaceBundleEntry(targetBundlePath, bundlePathWithoutHash))
                            Debug.LogErrorFormat("Unable to find registered file for bundle {0}.", targetBundlePath);
                    }

                    if (dataEntry != null)
                        if (DataEntryDiffersFromBundleFilename(dataEntry, bundlePathWithoutHash))
                            dataEntry.InternalId = StripHashFromBundleLocation(dataEntry.InternalId);
                });
            }
        }

        // if false, there is no need to remove the hash from dataEntry.InternalId
        bool DataEntryDiffersFromBundleFilename(ContentCatalogDataEntry dataEntry, string bundlePathWithoutHash)
        {
            string dataEntryId = dataEntry.InternalId;
            string dataEntryFilename = Path.GetFileName(dataEntryId);
            string bundleFileName = Path.GetFileName(bundlePathWithoutHash);

            return dataEntryFilename != bundleFileName;
        }

        /// <summary>
        /// Sets the primary key of the given location. Syncing with other locations that have a dependency on this location
        /// </summary>
        /// <param name="forLocation">CatalogEntry to set the primary key for</param>
        /// <param name="newPrimaryKey">New Primary key to set on location</param>
        /// <param name="aaContext">Addressables build context to collect and assign other location data</param>
        /// <exception cref="ArgumentException"></exception>
        private void SetPrimaryKey(ContentCatalogDataEntry forLocation, string newPrimaryKey, AddressableAssetsBuildContext aaContext)
        {
            if (forLocation == null || forLocation.Keys == null || forLocation.Keys.Count == 0)
                throw new ArgumentException("Cannot change primary key. Invalid catalog entry");

            string originalKey = forLocation.Keys[0] as string;
            if (string.IsNullOrEmpty(originalKey))
                throw new ArgumentException("Invalid primary key for catalog entry " + forLocation.ToString());

            forLocation.Keys[0] = newPrimaryKey;
            m_PrimaryKeyToLocation.Remove(originalKey);
            m_PrimaryKeyToLocation.Add(newPrimaryKey, forLocation);

            if (!GetPrimaryKeyToDependerLocations(aaContext.locations).TryGetValue(originalKey, out var dependers))
                return; // nothing depends on it

            foreach (ContentCatalogDataEntry location in dependers)
            {
                for (int i = 0; i < location.Dependencies.Count; ++i)
                {
                    string keyString = location.Dependencies[i] as string;
                    if (string.IsNullOrEmpty(keyString))
                        continue;
                    if (keyString == originalKey)
                    {
                        location.Dependencies[i] = newPrimaryKey;
                        break;
                    }
                }
            }

            m_PrimaryKeyToDependers.Remove(originalKey);
            m_PrimaryKeyToDependers.Add(newPrimaryKey, dependers);
        }

        private static long GetFileSize(string fileName)
        {
            try
            {
                return new FileInfo(fileName).Length;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return 0;
            }
        }

        /// <inheritdoc/>
        public bool IsDataBuilt()
        {
            if (!m_BuiltData)
            {
                return true;
            }
            return !String.IsNullOrEmpty(m_CatalogBuildPath) && File.Exists(m_CatalogBuildPath);
        }

        /// <inheritdoc/>
        public void GenerateTypeStrippingInfo(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext, ContentCatalogData contentCatalog)
        {
            using (builderInput.Logger.ScopedStep(LogLevel.Info, "Generate link"))
            {
                foreach (var pd in contentCatalog.ResourceProviderData)
                {
                    m_Linker.AddTypes(pd.ObjectType.Value);
                    m_Linker.AddTypes(pd.GetRuntimeTypes());
                }

                m_Linker.AddTypes(contentCatalog.InstanceProviderData.ObjectType.Value);
                m_Linker.AddTypes(contentCatalog.InstanceProviderData.GetRuntimeTypes());
                m_Linker.AddTypes(contentCatalog.SceneProviderData.ObjectType.Value);
                m_Linker.AddTypes(contentCatalog.SceneProviderData.GetRuntimeTypes());

                foreach (var o in aaContext.Settings.InitializationObjects)
                {
                    if (o is IObjectInitializationDataProvider io)
                    {
                        var id = io.CreateObjectInitializationData();
                        aaContext.runtimeData.InitializationObjects.Add(id);
                        m_Linker.AddTypes(id.ObjectType.Value);
                        m_Linker.AddTypes(id.GetRuntimeTypes());
                    }
                }

                m_Linker.AddTypes(typeof(Addressables));
                Directory.CreateDirectory(Addressables.BuildPath + "/AddressablesLink/");
                m_Linker.Save(Addressables.BuildPath + "/AddressablesLink/link.xml");
            }
        }

        /// <inheritdoc/>
        public void GenerateContentUpdate(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext, ExtractDataTask extractData, List<CachedAssetState> cachedState, AddressablesPlayerBuildResult addrResult)
        {
            if (extractData.BuildCache != null && builderInput.PreviousContentState == null)
            {
                using (builderInput.Logger.ScopedStep(LogLevel.Info, "Generate Content Update State"))
                {
                    var tempPath = Path.GetDirectoryName(Application.dataPath) + "/" + Addressables.LibraryPath + PlatformMappingService.GetPlatformPathSubFolder() + "/addressables_content_state.bin";
                    var playerBuildVersion = builderInput.PlayerVersion;

                    var remoteCatalogLoadPath = aaContext.Settings.BuildRemoteCatalog
                        ? aaContext.Settings.RemoteCatalogLoadPath.GetValue(aaContext.Settings)
                        : string.Empty;

                    var allEntries = new List<AddressableAssetEntry>();
                    using (builderInput.Logger.ScopedStep(LogLevel.Info, "Get Assets"))
                        aaContext.Settings.GetAllAssets(allEntries, false, ContentUpdateScript.GroupFilterFunc);

                    if (ContentUpdateScript.SaveContentState(
                        aaContext.locations,
                        aaContext.GuidToCatalogLocation,
                        tempPath,
                        allEntries,
                        extractData.DependencyData,
                        playerBuildVersion,
                        remoteCatalogLoadPath,
                        m_BuiltTypeTreeDataPath,
                        cachedState))
                    {
                        string contentStatePath = ContentUpdateScript.GetContentStateDataPath(false, aaContext.Settings);
                        if (ResourceManagerConfig.ShouldPathUseWebRequest(contentStatePath))
                        {
#if ENABLE_CCD
                            contentStatePath = Path.Combine(aaContext.Settings.RemoteCatalogBuildPath.GetValue(aaContext.Settings), Path.GetFileName(tempPath));
#else
                            contentStatePath = ContentUpdateScript.PreviousContentStateFileCachePath;
#endif
                        }

                        m_DataBuilder.CopyAndRegisterContentState(tempPath, contentStatePath, builderInput, addrResult);
                    }
                }
            }

            if (addrResult != null)
                addrResult.IsUpdateContentBuild = builderInput.PreviousContentState != null;
        }

        /// <inheritdoc/>
        public List<ContentCatalogData> GenerateCatalogs(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext, AddressablesPlayerBuildResult addrResult)
        {
            // save off the catalog build path for IsDataBuilt checks
            m_CatalogBuildPath = Path.Combine(Addressables.BuildPath, builderInput.RuntimeCatalogFilename);

            var aaSettings = aaContext.Settings;
            var versionedFileName = aaSettings.profileSettings.EvaluateString(aaSettings.activeProfileId, "/catalog_" + builderInput.PlayerVersion);
            var remoteBuildPath = aaSettings.RemoteCatalogBuildPath.Id != "" ? aaSettings.RemoteCatalogBuildPath.GetValue(aaSettings) : "";
            var remoteLoadPath = aaSettings.RemoteCatalogLoadPath.Id != "" ?  aaSettings.RemoteCatalogLoadPath.GetValue(aaSettings) : "";
            var catalogPathConfig = new CatalogPathConfig()
            {
                BuildPath = Addressables.BuildPath,
                RemoteBuildPath = remoteBuildPath,
                RemoteLoadPath = remoteLoadPath,
                RuntimeCatalogFilename = builderInput.RuntimeCatalogFilename,
                VersionedCatalogFileName = versionedFileName,
            };

            string buildResultHash = null;
            if (addrResult != null)
            {
                object[] hashingObjects = new object[addrResult.AssetBundleBuildResults.Count];
                for (int i = 0; i < addrResult.AssetBundleBuildResults.Count; ++i)
                    hashingObjects[i] = addrResult.AssetBundleBuildResults[i].Hash;
                buildResultHash = HashingMethods.Calculate(hashingObjects).ToString();
            }

#if UNITY_6000_5_OR_NEWER
            // this variable is always reset when Init is called at the start of a build when we initialize the build context.
            m_BuiltTypeTreeDataPath = Path.Combine(Addressables.BuildPath, kTypeTreeDataFileName);
            if (aaContext.Settings.ExtractTypeTreeData)
            {
                aaContext.providerTypes.Add(typeof(CachedFileProvider));
                if (builderInput.PreviousContentState != null)
                {
                    var strippedPath = Path.GetTempFileName();
                    if (builderInput.PreviousContentState.typeTreeHashes != null)
                        ContentBuildInterface.StripTypeTreeDataFromFile(builderInput.PreviousContentState.typeTreeHashes, m_BuiltTypeTreeDataPath, strippedPath);
                    else
                        strippedPath = m_BuiltTypeTreeDataPath;

                    var hashStr = Hash128.Compute(File.ReadAllBytes(strippedPath)).ToString();
                    var newPath = $"{aaContext.Settings.RemoteCatalogBuildPath.GetValue(aaContext.Settings)}/{hashStr}{kTypeTreeDataExtension}";
                    if (!Directory.Exists(Path.GetDirectoryName(newPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                    if(File.Exists(newPath))
                        File.Delete(newPath);
                    File.Move(strippedPath, newPath);
                    builderInput.Registry.AddFile(newPath);

                    string remoteURL = $"{aaContext.Settings.RemoteCatalogLoadPath.GetValue(aaContext.Settings)}/{hashStr}{kTypeTreeDataExtension}";
                    aaContext.locations.Add(new ContentCatalogDataEntry(typeof(string),
                        remoteURL,  //for remote content, the url
                        typeof(CachedFileProvider).FullName,
                        new string[] { ResourceManagerRuntimeData.kTypeTreeDataAddress },
                        null,
                        new ProviderLoadRequestOptions
                        {
                            IgnoreFailures = false,
                            LocalCachePath = $"{hashStr[0]}{hashStr[1]}/{hashStr}"
                        }));
                }
                //only add the local tt data location if this is NOT a content update OR if the baseline build has hashes (tt extraction was enabled)
                if (builderInput.PreviousContentState == null || (builderInput.PreviousContentState.typeTreeHashes != null && builderInput.PreviousContentState.typeTreeHashes.Length > 0))
                {
                    aaContext.locations.Add(new ContentCatalogDataEntry(typeof(string),
                    "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/" + kTypeTreeDataFileName,
                    typeof(CachedFileProvider).FullName,
                    new string[] { ResourceManagerRuntimeData.kTypeTreeDataAddress }));
                }
            }
            else
            {
                if (File.Exists(m_BuiltTypeTreeDataPath))
                    File.Delete(m_BuiltTypeTreeDataPath);
                m_BuiltTypeTreeDataPath = string.Empty;
            }
#endif

#if ENABLE_JSON_CATALOG
            CatalogBundleConfig catalogBundleConfig = null;
            if (aaContext.Settings.BundleLocalCatalog)
            {
                var configFolder = AddressableAssetSettingsDefaultObject.kDefaultConfigFolder;
                if (builderInput.AddressableSettings != null && builderInput.AddressableSettings.IsPersisted)
                    configFolder = builderInput.AddressableSettings.ConfigFolder;

                catalogBundleConfig = new CatalogBundleConfig
                {
                    ConfigFolder = configFolder
                };
            }

            var catalogBuilder = new JsonCatalogBuilder();
            m_CatalogBuildPath += ".json";
            return new List<ContentCatalogData>() {

                catalogBuilder.GenerateCatalog(
                builderInput.Logger,
                catalogPathConfig,
                ResourceManagerRuntimeData.kCatalogAddress, //TODO: if we move AssetBundle builds to support multiple catalogs, we can change this to use the schema CatalogId
                aaContext.locations,
                aaContext.runtimeData.CatalogLocations,
                aaContext.providerTypes,
                builderInput.Registry,
                buildResultHash,
                aaContext.Settings.BuildRemoteCatalog,
                aaContext.Settings.CatalogRequestsTimeout)
            };
#else
            var catalogBuilder = new BinaryCatalogBuilder();
            m_CatalogBuildPath += ".bin";
            return new List<ContentCatalogData>() {
                catalogBuilder.GenerateCatalog(
                builderInput.Logger,
                catalogPathConfig,
                ResourceManagerRuntimeData.kCatalogAddress, //TODO: if we move AssetBundle builds to support multiple catalogs, we can change this to use the schema CatalogId
                aaContext.locations,
                aaContext.runtimeData.CatalogLocations,
                aaContext.providerTypes,
                builderInput.Registry,
                buildResultHash,
                aaContext.Settings.BuildRemoteCatalog,
                aaContext.Settings.CatalogRequestsTimeout)
            };
#endif
        }
    }
}
