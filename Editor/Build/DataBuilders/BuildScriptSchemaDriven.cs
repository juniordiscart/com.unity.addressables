using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Build.Pipeline;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Build scripts used for player builds and running with bundles in the editor.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildScriptSchemaDriver.asset", menuName = "Addressables/Content Builders/Default Build Schema Driven")]
    public class BuildScriptSchemaDriven : BuildScriptBase
    {

        [NonSerialized]
        private ISchemaBuilder[] m_SchemaBuilders;

        /// <summary>
        /// Gets the schema builders used by this build script to process different group schemas.
        /// Schema builders are created lazily via <see cref="CreateSchemaBuilders"/>.
        /// </summary>
        public ISchemaBuilder[] SchemaBuilders
        {
            get
            {
                if (m_SchemaBuilders == null)
                    m_SchemaBuilders = CreateSchemaBuilders();
                return m_SchemaBuilders;
            }
        }

        /// <summary>
        /// Creates and returns the schema builders used to process group schemas during the build.
        /// Override this method to provide custom schema builders.
        /// </summary>
        /// <returns>An array of schema builders that will process group schemas.</returns>
        public virtual ISchemaBuilder[] CreateSchemaBuilders()
        {
            return new ISchemaBuilder[] {
                new BundledAssetSchemaBuilder(),
#if ENABLE_CONTENT_DIRECTORIES
                new ContentDirectorySchemaBuilder(),
#endif
            };
        }

        /// <inheritdoc />
        public override string Name
        {
            get { return "Schema Driven Build"; }
        }

        /// <inheritdoc />
        public override bool CanBuildData<T>()
        {
            return typeof(T).IsAssignableFrom(typeof(AddressablesPlayerBuildResult));
        }

        /// <summary>
        /// Returns a delegate bound to this instance's <see cref="BuildDataImplementation{TResult}"/> via virtual dispatch.
        /// Used by <see cref="BuildScriptPackedMode"/> to invoke the protected hook from outside the class
        /// hierarchy while still honoring overrides on derived types in any assembly.
        /// Do not call from build-script overrides; override <see cref="BuildDataImplementation{TResult}"/> instead.
        /// </summary>
        /// <typeparam name="TResult">The type of <see cref="IDataBuilderResult"/> to produce.</typeparam>
        /// <returns>A delegate that invokes <see cref="BuildDataImplementation{TResult}"/> on this instance.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Func<AddressablesDataBuilderInput, TResult> GetBuildDataImplementationCallback<TResult>() where TResult : IDataBuilderResult
            => BuildDataImplementation<TResult>;

        /// <summary>
        /// Returns a delegate bound to this instance's <see cref="ProcessAllGroups"/> via virtual dispatch.
        /// Used by <see cref="BuildScriptPackedMode"/> to invoke the protected hook from outside the class
        /// hierarchy while still honoring overrides on derived types in any assembly.
        /// Do not call from build-script overrides; override <see cref="ProcessAllGroups"/> instead.
        /// </summary>
        /// <returns>A delegate that invokes <see cref="ProcessAllGroups"/> on this instance.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Func<AddressableAssetsBuildContext, string> GetProcessAllGroupsCallback()
            => ProcessAllGroups;

        /// <summary>
        /// Returns a delegate bound to this instance's <see cref="ProcessGroup"/> via virtual dispatch.
        /// Used by <see cref="BuildScriptPackedMode"/> to invoke the protected hook from outside the class
        /// hierarchy while still honoring overrides on derived types in any assembly.
        /// Do not call from build-script overrides; override <see cref="ProcessGroup"/> instead.
        /// </summary>
        /// <returns>A delegate that invokes <see cref="ProcessGroup"/> on this instance.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Func<AddressableAssetGroup, AddressableAssetsBuildContext, string> GetProcessGroupCallback()
            => ProcessGroup;

        /// <summary>
        /// Returns a delegate bound to this instance's <see cref="ProcessGroupSchema"/> via virtual dispatch.
        /// Used by <see cref="BuildScriptPackedMode"/> to invoke the protected hook from outside the class
        /// hierarchy while still honoring overrides on derived types in any assembly.
        /// Do not call from build-script overrides; override <see cref="ProcessGroupSchema"/> instead.
        /// </summary>
        /// <returns>A delegate that invokes <see cref="ProcessGroupSchema"/> on this instance.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Func<AddressableAssetGroupSchema, AddressableAssetGroup, AddressableAssetsBuildContext, string> GetProcessGroupSchemaCallback()
            => ProcessGroupSchema;

        /// <summary>
        /// Returns a delegate bound to this instance's <see cref="DoBuild{TResult}"/> via virtual dispatch.
        /// Used by <see cref="BuildScriptPackedMode"/> to invoke the protected hook from outside the class
        /// hierarchy while still honoring overrides on derived types in any assembly.
        /// Do not call from build-script overrides; override <see cref="DoBuild{TResult}"/> instead.
        /// </summary>
        /// <typeparam name="TResult">The type of <see cref="IDataBuilderResult"/> to produce.</typeparam>
        /// <returns>A delegate that invokes <see cref="DoBuild{TResult}"/> on this instance.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual Func<AddressablesDataBuilderInput, AddressableAssetsBuildContext, TResult> GetDoBuildCallback<TResult>() where TResult : IDataBuilderResult
            => DoBuild<TResult>;

        /// <summary>
        /// Returns a delegate bound to this instance's <see cref="ConstructAssetBundleName"/> via virtual dispatch.
        /// Used by <see cref="BuildScriptPackedMode"/> to invoke the protected hook from outside the class
        /// hierarchy while still honoring overrides on derived types in any assembly.
        /// Do not call from build-script overrides; override <see cref="ConstructAssetBundleName"/> instead.
        /// </summary>
        /// <returns>A delegate that invokes <see cref="ConstructAssetBundleName"/> on this instance.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Func<AddressableAssetGroup, BundledAssetGroupSchema, BundleDetails, string, string> GetConstructAssetBundleNameCallback()
            => ConstructAssetBundleName;

        /// <inheritdoc />
        protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
        {
            NotifyUserAboutBuildReport();

            TResult result = default(TResult);
            m_IncludedGroupsInBuild?.Clear();

            InitializeBuildContext(builderInput, out AddressableAssetsBuildContext aaContext);

            using (Log.ScopedStep(LogLevel.Info, "ProcessAllGroups"))
            {
                var errorString = ProcessAllGroups(aaContext);
                if (!string.IsNullOrEmpty(errorString))
                    result = CreateErrorResult<TResult>(errorString, builderInput, aaContext);
            }

            if (result == null)
            {
                result = DoBuild<TResult>(builderInput, aaContext);
            }

            if (result == null)
                return result;

            var span = DateTime.Now - aaContext.buildStartTime;
            result.Duration = span.TotalSeconds;
            if (string.IsNullOrEmpty(result.Error))
            {
                ClearContentUpdateNotifications(m_IncludedGroupsInBuild);
            }
            DisplayBuildReport();
            return result;
        }

        private TResult CreateErrorResult<TResult>(string errorString, AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext) where TResult : IDataBuilderResult
        {
            BuildLayoutGenerationTask.GenerateErrorReport(errorString, aaContext, builderInput.PreviousContentState);
            return AddressableAssetBuildResult.CreateResult<TResult>(null, 0, errorString);
        }

        internal void InitializeBuildContext(AddressablesDataBuilderInput builderInput, out AddressableAssetsBuildContext aaContext)
        {
            var now = DateTime.Now;
            var aaSettings = builderInput.AddressableSettings;
#if ENABLE_CCD
            // we have to populate the ccd managed data every time we build.
            try
            {
                CcdBuildEvents.Instance.PopulateCcdManagedData(aaSettings, aaSettings.activeProfileId);
            }
            catch (Exception e)
            {
                Addressables.LogError("Unable to populated CCD Managed Data. You may need to refresh remote data in the profile window.");
                throw;
            }
#endif
            var bundleToAssetGroup = new Dictionary<string, string>();
            var runtimeData = new ResourceManagerRuntimeData
            {
                SettingsHash = aaSettings.currentHash.ToString(),
                CertificateHandlerType = aaSettings.CertificateHandlerType,
                BuildTarget = builderInput.Target.ToString(),
#if ENABLE_CCD
                CcdManagedData = aaSettings.m_CcdManagedData,
#endif
                LogResourceManagerExceptions = aaSettings.buildSettings.LogResourceManagerExceptions,
                DisableCatalogUpdateOnStartup = aaSettings.DisableCatalogUpdateOnStartup,
#if ENABLE_JSON_CATALOG
                IsLocalCatalogInBundle = aaSettings.BundleLocalCatalog,
#endif
                AddressablesVersion = Addressables.Version,
                MaxConcurrentWebRequests = aaSettings.MaxConcurrentWebRequests,
                CatalogRequestsTimeout = aaSettings.CatalogRequestsTimeout
            };

            aaContext = new AddressableAssetsBuildContext
            {
                Settings = aaSettings,
                runtimeData = runtimeData,
                bundleToAssetGroup = bundleToAssetGroup,
                locations = new List<ContentCatalogDataEntry>(),
                providerTypes = new HashSet<Type>(),
                assetEntries = new List<AddressableAssetEntry>(),
                internalToOutputBundleName = new Dictionary<string, string>(),
                buildStartTime = now,
                ContainsAssetBundleData = false,
                ContainsContentDirectoryData = false
            };

            foreach (ISchemaBuilder schemaBuilder in SchemaBuilders)
            {
                schemaBuilder.Init(aaContext, this);
            }
        }

        /// <summary>
        /// The method that does the actual building after all the groups have been processed.
        /// </summary>
        /// <param name="builderInput">The generic builderInput of the</param>
        /// <param name="aaContext">Addressables context object</param>
        /// <typeparam name="TResult">The type of IDataBuilderResult object to be produced by the build</typeparam>
        /// <returns>The IDataBuilderResult produced by the build</returns>
        protected virtual TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext) where TResult : IDataBuilderResult
        {
            var genericResult = AddressableAssetBuildResult.CreateResult<TResult>();
            AddressablesPlayerBuildResult addrResult = genericResult as AddressablesPlayerBuildResult;

            ExtractDataTask extractData = new ExtractDataTask();
            List<CachedAssetState> carryOverCachedState = new List<CachedAssetState>();
            if (!BuildUtility.CheckModifiedScenesAndAskToSave())
                return CreateErrorResult<TResult>("Unsaved scenes", builderInput, aaContext);

            AddInstanceAndSceneProvider(aaContext);

            var contentCatalogs = new List<ContentCatalogData>();
            BuildContext buildContext = new BuildContext(aaContext, Log);
            foreach (ISchemaBuilder schemaBuilder in SchemaBuilders)
            {
                schemaBuilder.Build(
                    buildContext,
                    builderInput,
                    aaContext,
                    extractData,
                    carryOverCachedState,
                    addrResult);
                var schemaGeneratedCatalogs = schemaBuilder.GenerateCatalogs(builderInput, aaContext, addrResult);

                foreach (var contentCatalog in schemaGeneratedCatalogs)
                {
                    if (contentCatalog == null)
                    {
                        Debug.Log($"No catalog generated for schema builder: {schemaBuilder.Name}");
                        continue;
                    }
                    schemaBuilder.GenerateTypeStrippingInfo(builderInput, aaContext, contentCatalog);
                    schemaBuilder.GenerateContentUpdate(builderInput, aaContext, extractData, carryOverCachedState, addrResult);
                    contentCatalogs.Add(contentCatalog);
                }
            }
            // sort catalogs to be deterministic
            aaContext.runtimeData.CatalogLocations.Sort((a, b) => string.Compare(a.InternalId, b.InternalId, StringComparison.Ordinal));
            var settingsPath = GenerateRuntimeSettingsFile(aaContext, builderInput);
            genericResult.LocationCount = aaContext.locations.Count;
            genericResult.OutputPath = settingsPath;

            GenerateBuildLayout(extractData.BuildContext, aaContext.internalToOutputBundleName, contentCatalogs.ToArray(), addrResult);
            return genericResult;
        }

        private void AddInstanceAndSceneProvider(AddressableAssetsBuildContext aaContext)
        {
            aaContext.providerTypes.Add(instanceProviderType.Value);
            aaContext.providerTypes.Add(sceneProviderType.Value);
        }

        /// <summary>
        /// Generates the runtime settings JSON file that configures Addressables at runtime.
        /// </summary>
        /// <param name="aaContext">The Addressables build context containing runtime data.</param>
        /// <param name="builderInput">The build input containing settings and registry.</param>
        /// <returns>The path to the generated settings file.</returns>
        public string GenerateRuntimeSettingsFile(AddressableAssetsBuildContext aaContext, AddressablesDataBuilderInput builderInput)
        {
            var settingsPath = Addressables.BuildPath + "/" + builderInput.RuntimeSettingsFilename;

            using (Log.ScopedStep(LogLevel.Info, "Generate Settings"))
                WriteFile(settingsPath, JsonUtility.ToJson(aaContext.runtimeData), builderInput.Registry);

            return settingsPath;
        }

        private void GenerateBuildLayout(IBuildContext buildContext,
            Dictionary<string, string> bundleRenameMap,
            ContentCatalogData[] contentCatalogs,
            AddressablesPlayerBuildResult buildResult)
        {
            if (ProjectConfigData.GenerateBuildLayout && buildContext != null)
            {
                using (var progressTracker = new UnityEditor.Build.Pipeline.Utilities.ProgressTracker())
                {
                    progressTracker.UpdateTask("Generating Build Layout");
                    using (Log.ScopedStep(LogLevel.Info, "Generate Build Layout"))
                    {
                        List<IBuildTask> tasks = new List<IBuildTask>();
                        var buildLayoutTask = new BuildLayoutGenerationTask();
                        buildContext.SetContextObject<IBuildLayoutParameters>(new BuildLayoutParameters(bundleRenameMap, contentCatalogs, buildResult));
                        tasks.Add(buildLayoutTask);
                        BuildTasksRunner.Run(tasks, buildContext);
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            if (assetGroup == null)
                return string.Empty;

            if (assetGroup.Schemas.Count == 0)
            {
                Addressables.LogWarning($"{assetGroup.Name} does not have any associated AddressableAssetGroupSchemas. " +
                    $"Data from this group will not be included in the build. " +
                    $"If this is unexpected the AddressableGroup may have become corrupted.");
                return string.Empty;
            }

            foreach (var schema in assetGroup.Schemas)
            {
                string errorString = "";
                if (schema.IsEnabled)
                {
                    errorString = schema.CanEnableSchema();
                    if (!string.IsNullOrEmpty(errorString))
                        return errorString;
                }

                errorString = ProcessGroupSchema(schema, assetGroup, aaContext);
                if (!string.IsNullOrEmpty(errorString))
                    return errorString;
            }

            return string.Empty;
        }

        /// <summary>
        /// Called per group per schema to evaluate that schema.  This can be an easy entry point for implementing the
        ///  build aspects surrounding a custom schema.  Note, you should not rely on schemas getting called in a specific
        ///  order.
        /// </summary>
        /// <param name="schema">The schema to process</param>
        /// <param name="assetGroup">The group this schema was pulled from</param>
        /// <param name="aaContext">The general Addressables build builderInput</param>
        /// <returns>The error string, if any.</returns>
        protected virtual string ProcessGroupSchema(AddressableAssetGroupSchema schema, AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            foreach (var schemaBuilder in SchemaBuilders)
            {
                if (!schemaBuilder.CanBuildSchema(schema))
                    continue;
                var errorString = schemaBuilder.ProcessGroupSchema(schema, assetGroup, aaContext);
                if (errorString != string.Empty)
                    return errorString;
            }
            AssetDatabase.Refresh();
            return string.Empty;
        }

        /// <summary>
        /// A temporary list of the groups that get processed during a build.
        /// </summary>
        List<AddressableAssetGroup> m_IncludedGroupsInBuild = new List<AddressableAssetGroup>();

        /// <summary>
        /// Returns a delegate bound to this instance's <see cref="ProcessBundledAssetSchema"/> via virtual dispatch.
        /// Used by <see cref="BundledAssetSchemaBuilder"/> to invoke the protected hook from outside the class
        /// hierarchy while still honoring overrides on derived types in any assembly.
        /// Do not call from build-script overrides; override <see cref="ProcessBundledAssetSchema"/> instead.
        /// </summary>
        /// <returns>A delegate that invokes <see cref="ProcessBundledAssetSchema"/> on this instance.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Func<BundledAssetGroupSchema, AddressableAssetGroup, AddressableAssetsBuildContext, string>
            GetProcessBundledAssetSchemaCallback() => ProcessBundledAssetSchema;

        /// <summary>
        /// The processing of the bundled asset schema.  This is where the bundle(s) for a given group are actually setup.
        /// </summary>
        /// <param name="schema">The BundledAssetGroupSchema to process</param>
        /// <param name="assetGroup">The group this schema was pulled from</param>
        /// <param name="aaContext">The general Addressables build builderInput</param>
        /// <returns>The error string, if any.</returns>
        protected virtual string ProcessBundledAssetSchema(
            BundledAssetGroupSchema schema,
            AddressableAssetGroup assetGroup,
            AddressableAssetsBuildContext aaContext)
        {
            // this is to preserve backwards compatability, it is called from inside
            foreach (ISchemaBuilder schemaBuilder in SchemaBuilders)
            {
                if (schemaBuilder is BundledAssetSchemaBuilder bundledAssetSchemaBuilder)
                {
                    return bundledAssetSchemaBuilder.ProcessBundledAssetSchema(schema, assetGroup, aaContext, m_IncludedGroupsInBuild);
                }
            }
            return string.Empty;
        }


        internal static string CalculateGroupHash(BundledAssetGroupSchema.BundleInternalIdMode mode, AddressableAssetGroup assetGroup, IEnumerable<AddressableAssetEntry> entries)
        {
            switch (mode)
            {
                case BundledAssetGroupSchema.BundleInternalIdMode.GroupGuid:
                    return assetGroup.Guid;
                case BundledAssetGroupSchema.BundleInternalIdMode.GroupGuidProjectIdHash:
                    return HashingMethods.Calculate(assetGroup.Guid, Application.cloudProjectId).ToString();
                case BundledAssetGroupSchema.BundleInternalIdMode.GroupGuidProjectIdEntriesHash:
                    return HashingMethods.Calculate(assetGroup.Guid, Application.cloudProjectId, new HashSet<string>(entries.Select(e => e.guid))).ToString();
            }

            throw new Exception("Invalid naming mode.");
        }

        /// <summary>
        /// Processes an AddressableAssetGroup and generates AssetBundle input definitions based on the BundlePackingMode.
        /// </summary>
        /// <param name="assetGroup">The AddressableAssetGroup to be processed.</param>
        /// <param name="bundleInputDefs">The list of bundle definitions fed into the build pipeline AssetBundleBuild</param>
        /// <param name="schema">The BundledAssetGroupSchema of used to process the assetGroup.</param>
        /// <param name="entryFilter">A filter to remove AddressableAssetEntries from being processed in the build.</param>
        /// <returns>The total list of AddressableAssetEntries that were processed.</returns>
        public static List<AddressableAssetEntry> PrepGroupBundlePacking(AddressableAssetGroup assetGroup, List<AssetBundleBuild> bundleInputDefs, BundledAssetGroupSchema schema,
            Func<AddressableAssetEntry, bool> entryFilter = null)
        {
            var combinedEntries = new List<AddressableAssetEntry>();
            var packingMode = schema.BundleMode;
            var namingMode = schema.InternalBundleIdMode;
            bool ignoreUnsupportedFilesInBuild = assetGroup.Settings.IgnoreUnsupportedFilesInBuild;

            switch (packingMode)
            {
                case BundledAssetGroupSchema.BundlePackingMode.PackTogether:
                    {
                        var allEntries = new List<AddressableAssetEntry>();
                        foreach (AddressableAssetEntry a in assetGroup.entries)
                        {
                            if (entryFilter != null && !entryFilter(a))
                                continue;
                            a.GatherAllAssets(allEntries, true, true, false, entryFilter);
                        }

                        combinedEntries.AddRange(allEntries);
                        GenerateBuildInputDefinitions(allEntries, bundleInputDefs, CalculateGroupHash(namingMode, assetGroup, allEntries), "all", ignoreUnsupportedFilesInBuild);
                    }
                    break;
                case BundledAssetGroupSchema.BundlePackingMode.PackSeparately:
                    {
                        foreach (AddressableAssetEntry a in assetGroup.entries)
                        {
                            if (entryFilter != null && !entryFilter(a))
                                continue;
                            var allEntries = new List<AddressableAssetEntry>();
                            a.GatherAllAssets(allEntries, true, true, false, entryFilter);
                            combinedEntries.AddRange(allEntries);
                            GenerateBuildInputDefinitions(allEntries, bundleInputDefs, CalculateGroupHash(namingMode, assetGroup, allEntries), a.address, ignoreUnsupportedFilesInBuild);
                        }
                    }
                    break;
                case BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel:
                    {
                        var labelTable = new Dictionary<string, List<AddressableAssetEntry>>();
                        foreach (AddressableAssetEntry a in assetGroup.entries)
                        {
                            if (entryFilter != null && !entryFilter(a))
                                continue;
                            var sb = new StringBuilder();
                            foreach (var l in a.labels)
                                sb.Append(l);
                            var key = sb.ToString();
                            List<AddressableAssetEntry> entries;
                            if (!labelTable.TryGetValue(key, out entries))
                                labelTable.Add(key, entries = new List<AddressableAssetEntry>());
                            entries.Add(a);
                        }

                        foreach (var entryGroup in labelTable)
                        {
                            var allEntries = new List<AddressableAssetEntry>();
                            foreach (var a in entryGroup.Value)
                            {
                                if (entryFilter != null && !entryFilter(a))
                                    continue;
                                a.GatherAllAssets(allEntries, true, true, false, entryFilter);
                            }

                            combinedEntries.AddRange(allEntries);
                            GenerateBuildInputDefinitions(allEntries, bundleInputDefs, CalculateGroupHash(namingMode, assetGroup, allEntries), entryGroup.Key, ignoreUnsupportedFilesInBuild);
                        }
                    }
                    break;
                default:
                    throw new Exception("Unknown Packing Mode");
            }

            return combinedEntries;
        }

        internal static void GenerateBuildInputDefinitions(List<AddressableAssetEntry> allEntries, List<AssetBundleBuild> buildInputDefs, string groupGuid, string address,
            bool ignoreUnsupportedFilesInBuild)
        {
            var scenes = new List<AddressableAssetEntry>();
            var assets = new List<AddressableAssetEntry>();
            foreach (var e in allEntries)
            {
                ThrowExceptionIfInvalidFiletypeOrAddress(e, ignoreUnsupportedFilesInBuild);
                if (string.IsNullOrEmpty(e.AssetPath))
                    continue;
                if (e.IsScene)
                    scenes.Add(e);
                else
                    assets.Add(e);
            }

            if (assets.Count > 0)
                buildInputDefs.Add(GenerateBuildInputDefinition(assets, groupGuid + "_assets_" + address + ".bundle"));
            if (scenes.Count > 0)
                buildInputDefs.Add(GenerateBuildInputDefinition(scenes, groupGuid + "_scenes_" + address + ".bundle"));
        }

        private static void ThrowExceptionIfInvalidFiletypeOrAddress(AddressableAssetEntry entry, bool ignoreUnsupportedFilesInBuild)
        {
            if (entry.guid.Length > 0 && entry.address.Contains('[') && entry.address.Contains(']'))
                throw new Exception($"Address '{entry.address}' cannot contain '[ ]'.");
            if (entry.MainAssetType == typeof(DefaultAsset) && !AssetDatabase.IsValidFolder(entry.AssetPath))
            {
                if (ignoreUnsupportedFilesInBuild)
                    Debug.LogWarning($"Cannot recognize file type for entry located at '{entry.AssetPath}'. Asset location will be ignored.");
                else
                    throw new Exception($"Cannot recognize file type for entry located at '{entry.AssetPath}'. Asset import failed for using an unsupported file type.");
            }
        }

        internal static AssetBundleBuild GenerateBuildInputDefinition(List<AddressableAssetEntry> assets, string name)
        {
            var assetInternalIds = new HashSet<string>();
            var assetsInputDef = new AssetBundleBuild();
            assetsInputDef.assetBundleName = name.ToLower().Replace(" ", "").Replace('\\', '/').Replace("//", "/");
            assetsInputDef.assetNames = assets.Select(s => s.AssetPath).ToArray();
            assetsInputDef.addressableNames = assets.Select(s => s.GetAssetLoadPath(true, assetInternalIds)).ToArray();
            return assetsInputDef;
        }


        internal string ConstructOutputName(AddressableAssetGroup assetGroup, AddressableAssetGroupSchema schema, BundleDetails info, string name)
        {
            var bundledAssetSchema = schema as BundledAssetGroupSchema;
            if (bundledAssetSchema != null)
                return ConstructAssetBundleName(assetGroup, bundledAssetSchema, info, name);
            return "";
        }

        /// <summary>
        /// Creates a name for an asset bundle using the provided information.
        /// </summary>
        /// <param name="assetGroup">The asset group.</param>
        /// <param name="schema">The schema of the group.</param>
        /// <param name="info">The bundle information.</param>
        /// <param name="assetBundleName">The base name of the asset bundle.</param>
        /// <returns>Returns the asset bundle name with the provided information.</returns>
        protected virtual string ConstructAssetBundleName(AddressableAssetGroup assetGroup, BundledAssetGroupSchema schema, BundleDetails info, string assetBundleName)
        {
            if (assetGroup != null)
            {
                if (!assetGroup.AllowNestedFolders && schema.BundleMode == BundledAssetGroupSchema.BundlePackingMode.PackSeparately)
                    assetBundleName = assetBundleName.Replace('/', '_');

                string groupName = assetGroup.Name.Replace(" ", "").Replace('\\', '/').Replace("//", "/").ToLower();
                assetBundleName = groupName + "_" + assetBundleName;
            }

            string bundleNameWithHashing = BuildUtility.GetNameWithHashNaming(schema.BundleNaming, info.Hash.ToString(), assetBundleName);
            //For no hash, we need the hash temporarily for content update purposes.  This will be stripped later on.
            if (schema.BundleNaming == BundledAssetGroupSchema.BundleNamingStyle.NoHash)
            {
                bundleNameWithHashing = bundleNameWithHashing.Replace(".bundle", "_" + info.Hash.ToString() + ".bundle");
            }

            return bundleNameWithHashing;
        }


        /// <inheritdoc />
        public override void ClearCachedData()
        {
            if (Directory.Exists(Addressables.BuildPath))
            {
                try
                {
#if ENABLE_JSON_CATALOG
                    var catalogPath = Addressables.BuildPath + "/catalog.json";
                    DeleteFile(catalogPath);
#else
                    var catalogPath = Addressables.BuildPath + "/catalog.bin";
                    DeleteFile(catalogPath);
#endif
                    var settingsPath = Addressables.BuildPath + "/settings.json";
                    DeleteFile(settingsPath);
                    Directory.Delete(Addressables.BuildPath, true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <inheritdoc />
        public override bool IsDataBuilt()
        {
            var settingsPath = Addressables.BuildPath + "/settings.json";
            if (!File.Exists(settingsPath))
                return false;
            foreach (var schemaBuilder in SchemaBuilders)
            {
                if (!schemaBuilder.IsDataBuilt())
                    return false;
            }
            return true;
        }
    }
}
