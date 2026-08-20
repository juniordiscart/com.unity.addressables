using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.CatalogBuilders;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets;
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
    /// <remarks>
    /// <para>This is the preferred base class for custom Addressables build scripts. Subclass it and
    /// override <see cref="CreateSchemaBuilders"/> to add, replace, or wrap the schema builders that
    /// drive the build. Most build customisations — bundle packing, catalog generation, content
    /// update logic — should be implemented by subclassing the relevant
    /// <see cref="UnityEditor.AddressableAssets.Build.ISchemaBuilder"/> (e.g.
    /// <see cref="SchemaBuilders.BundledAssetSchemaBuilder"/>) and returning it from
    /// <see cref="CreateSchemaBuilders"/>, rather than overriding methods on the build script
    /// itself.</para>
    /// <para><see cref="BuildScriptPackedMode"/> extends this class with additional virtual hooks
    /// (<c>ProcessBundledAssetSchema</c>, <c>ConstructAssetBundleName</c>) for compatibility with
    /// earlier extension patterns. New code should prefer schema-builder overrides instead.</para>
    /// </remarks>
    [CreateAssetMenu(fileName = "BuildScriptSchemaDriver.asset", menuName = "Addressables/Content Builders/Default Build Schema Driven")]
    [AddressablesHelpURL("Builds.html")]
    public class BuildScriptSchemaDriven : BuildScriptBase
    {
        [NonSerialized]
        private BuildContext m_BuildContext;

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
            get { return "Default Build Script"; }
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
            m_IncludedGroupsInBuild = new List<AddressableAssetGroup>();

            InitializeBuildContext(builderInput, out AddressableAssetsBuildContext aaContext);

            try
            {
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
            }
            finally
            {
                using (Log.ScopedStep(LogLevel.Info, "Cleanup"))
                {
                    Cleanup();
                }
            }

            DisplayBuildReport();
            return result;
        }

        private void Cleanup()
        {
            m_BuildContext = null;
            m_SchemaBuilders = null;
            m_IncludedGroupsInBuild = null;
        }

        private TResult CreateErrorResult<TResult>(string errorString, AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext) where TResult : IDataBuilderResult
        {
            BuildLayoutGenerationTask.GenerateErrorReport(errorString, aaContext, builderInput.PreviousContentState);
            return AddressableAssetBuildResult.CreateResult<TResult>(null, 0, errorString);
        }

        internal void InitializeBuildContext(AddressablesDataBuilderInput builderInput, out AddressableAssetsBuildContext aaContext)
        {
            using (Log.ScopedStep(LogLevel.Info, "InitializeBuildContext"))
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
                    IsLocalCatalogInBundle = CreateCatalogBuilder(aaSettings).SupportsLocalCatalogBundling && aaSettings.BundleLocalCatalog,
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

                m_BuildContext = new BuildContext(aaContext, Log);

                foreach (ISchemaBuilder schemaBuilder in SchemaBuilders)
                {
                    using (Log.ScopedStep(LogLevel.Verbose, $"{schemaBuilder.Name}.Init"))
                    {
                        schemaBuilder.Init(aaContext, builderInput, m_BuildContext, this);
                    }
                }
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

            aaContext.cachedState = new List<CachedAssetState>();
            if (!BuildUtility.CheckModifiedScenesAndAskToSave())
                return CreateErrorResult<TResult>("Unsaved scenes", builderInput, aaContext);

            AddInstanceAndSceneProvider(aaContext);

            var mergedLocations = BuildAndMergeLocations(aaContext, addrResult,
                out var builderToCatalogIds);

            var contentCatalogs = WriteMergedCatalogs(mergedLocations, builderInput,
                aaContext, addrResult, out var catalogIdToData);

            ApplyTypeStrippingAndContentUpdate(builderToCatalogIds, catalogIdToData,
                aaContext, addrResult);

            // sort catalogs to be deterministic
            aaContext.runtimeData.CatalogLocations.Sort((a, b) => string.Compare(a.InternalId, b.InternalId, StringComparison.Ordinal));

            // write settings.json
            var settingsPath = GenerateRuntimeSettingsFile(aaContext, builderInput);
            genericResult.LocationCount = aaContext.locations.Count;
            genericResult.OutputPath = settingsPath;

            GenerateBuildLayout(m_BuildContext, aaContext.internalToOutputBundleName, contentCatalogs.ToArray(), addrResult);
            return genericResult;
        }

        /// <summary>
        /// Runs each schema builder's <see cref="ISchemaBuilder.Build"/> and
        /// <see cref="ISchemaBuilder.GenerateCatalogLocations"/>, then merges the resulting
        /// locations by catalog id.
        /// </summary>
        private Dictionary<string, List<ContentCatalogDataEntry>> BuildAndMergeLocations(
            AddressableAssetsBuildContext aaContext,
            AddressablesPlayerBuildResult addrResult,
            out Dictionary<ISchemaBuilder, List<string>> builderToCatalogIds)
        {
            // this is the actual catalogs with all merged locations
            var mergedLocations = new Dictionary<string, List<ContentCatalogDataEntry>>();

            // this is a collection of the catalogs split by schema builder for generating
            // content updates and stripping information
            builderToCatalogIds = new Dictionary<ISchemaBuilder, List<string>>();

            foreach (ISchemaBuilder schemaBuilder in SchemaBuilders)
            {
                using (Log.ScopedStep(LogLevel.Info, $"Building {schemaBuilder.Name}"))
                {
                    schemaBuilder.Build(aaContext, addrResult);
                }

                using (Log.ScopedStep(LogLevel.Info, $"Generating {schemaBuilder.Name} Catalog Locations"))
                {
                    var locationsByCatalogId = schemaBuilder.GenerateCatalogLocations(aaContext, addrResult);
                    if (locationsByCatalogId == null)
                        continue;

                    var contributedIds = new List<string>();
                    foreach (var kvp in locationsByCatalogId)
                    {
                        if (!mergedLocations.TryGetValue(kvp.Key, out var existing))
                        {
                            existing = new List<ContentCatalogDataEntry>();
                            mergedLocations[kvp.Key] = existing;
                        }

                        existing.AddRange(kvp.Value);
                        contributedIds.Add(kvp.Key);
                    }

                    builderToCatalogIds[schemaBuilder] = contributedIds;
                }
            }

            return mergedLocations;
        }

        /// <summary>
        /// Writes one catalog file per merged catalog id and returns the generated catalog list.
        /// </summary>
        private List<ContentCatalogData> WriteMergedCatalogs(
            Dictionary<string, List<ContentCatalogDataEntry>> mergedLocations,
            AddressablesDataBuilderInput builderInput,
            AddressableAssetsBuildContext aaContext,
            AddressablesPlayerBuildResult addrResult,
            out Dictionary<string, ContentCatalogData> catalogIdToData)
        {
            var contentCatalogs = new List<ContentCatalogData>();
            using (Log.ScopedStep(LogLevel.Info, $"Writing Catalogs"))
            {
                catalogIdToData = new Dictionary<string, ContentCatalogData>();

                var sortedCatalogIds = new List<string>(mergedLocations.Keys);
                sortedCatalogIds.Sort(StringComparer.Ordinal);
                foreach (var catalogId in sortedCatalogIds)
                {
                    var catalogEntries = mergedLocations[catalogId];
                    catalogEntries.Sort((a, b) =>
                        string.Compare(a.InternalId, b.InternalId, StringComparison.Ordinal));

                    var catalogPathConfig = CreateCatalogPathConfig(aaContext.Settings, catalogId, builderInput.PlayerVersion, builderInput.RuntimeCatalogFilename);
                    var buildResultHash = ComputeCatalogBuildHash(catalogId, addrResult);

                    var catalogBuilder = CreateCatalogBuilder(aaContext.Settings);
                    CatalogBundleConfig catalogBundleConfig = null;
                    if (catalogId == ResourceManagerRuntimeData.kCatalogAddress
                        && catalogBuilder.SupportsLocalCatalogBundling
                        && aaContext.Settings.BundleLocalCatalog)
                    {
                        var configFolder = AddressableAssetSettingsDefaultObject.kDefaultConfigFolder;
                        if (builderInput.AddressableSettings != null && builderInput.AddressableSettings.IsPersisted)
                            configFolder = builderInput.AddressableSettings.ConfigFolder;
                        catalogBundleConfig = new CatalogBundleConfig
                        {
                            ConfigFolder = configFolder,
                            Target = builderInput.Target,
                            TargetGroup = builderInput.TargetGroup
                        };
                    }

                    using (Log.ScopedStep(LogLevel.Info, $"Generating {catalogBuilder.CatalogExtension} Catalog {catalogId}"))
                    {
                        var catalogData = catalogBuilder.GenerateCatalog(
                            Log,
                            catalogPathConfig,
                            catalogId,
                            catalogEntries,
                            aaContext.runtimeData.CatalogLocations,
                            aaContext.providerTypes,
                            builderInput.Registry,
                            buildResultHash,
                            aaContext.Settings.BuildRemoteCatalog,
                            aaContext.Settings.CatalogRequestsTimeout,
                            catalogBundleConfig);

                        if (catalogData != null)
                        {
                            contentCatalogs.Add(catalogData);
                            catalogIdToData[catalogId] = catalogData;
                        }
                        else
                        {
                            Log.AddEntry(LogLevel.Warning, $"No catalog generated for catalog id: {catalogId}");
                        }
                    }
                }
            }
            return contentCatalogs;
        }

        /// <summary>
        /// Runs per-builder type stripping and content-update, using the first generated catalog
        /// that each builder contributed as the representative.
        /// </summary>
        private void ApplyTypeStrippingAndContentUpdate(
            Dictionary<ISchemaBuilder, List<string>> builderToCatalogIds,
            Dictionary<string, ContentCatalogData> catalogIdToData,
            AddressableAssetsBuildContext aaContext,
            AddressablesPlayerBuildResult addrResult)
        {
            foreach (var kvp in builderToCatalogIds)
            {
                var schemaBuilder = kvp.Key;
                var catalogIds = kvp.Value;
                if (catalogIds.Count == 0)
                    continue;

                ContentCatalogData representativeCatalog = null;
                foreach (var id in catalogIds)
                {
                    if (catalogIdToData.TryGetValue(id, out representativeCatalog))
                        break;
                }

                if (representativeCatalog != null)
                    schemaBuilder.GenerateTypeStrippingInfo(aaContext, representativeCatalog);

                schemaBuilder.GenerateContentUpdate(aaContext, addrResult);
            }
        }

        private void AddInstanceAndSceneProvider(AddressableAssetsBuildContext aaContext)
        {
            aaContext.providerTypes.Add(instanceProviderType.Value);
            aaContext.providerTypes.Add(sceneProviderType.Value);
        }

        /// <summary>
        /// Builds a <see cref="CatalogPathConfig"/> for the given catalog id.
        /// The main catalog (<see cref="ResourceManagerRuntimeData.kCatalogAddress"/>) uses the
        /// runtime catalog filename and versioned filename from the builder input; additional catalog
        /// ids use the id itself as both filename and load-path suffix.
        /// </summary>
        internal CatalogPathConfig CreateCatalogPathConfig(AddressableAssetSettings aaSettings, string catalogId, string playerVersion, string runtimeCatalogFilename)
        {
            var remoteBuildPath = aaSettings.RemoteCatalogBuildPath.Id != "" ? aaSettings.RemoteCatalogBuildPath.GetValue(aaSettings) : "";
            var remoteLoadPath = aaSettings.RemoteCatalogLoadPath.Id != "" ? aaSettings.RemoteCatalogLoadPath.GetValue(aaSettings) : "";


            var runtimeCatalogFileName = catalogId;
            var versionedFileName = aaSettings.profileSettings.EvaluateString(aaSettings.activeProfileId, $"{catalogId}_{playerVersion}");
            if (catalogId == ResourceManagerRuntimeData.kCatalogAddress)
            {
                runtimeCatalogFileName = runtimeCatalogFilename;
                versionedFileName = aaSettings.profileSettings.EvaluateString(aaSettings.activeProfileId, $"{runtimeCatalogFilename}_{playerVersion}");
            }

            return new CatalogPathConfig
                {
                    BuildPath = Addressables.BuildPath,
                    LoadPath = "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/" + runtimeCatalogFileName,
                    RemoteBuildPath = remoteBuildPath,
                    RemoteLoadPath = remoteLoadPath,
                    RuntimeCatalogFilename = runtimeCatalogFileName,
                    VersionedCatalogFileName = versionedFileName,
                };
        }

        /// <summary>
        /// Computes the build result hash for a catalog.
        /// For the main catalog (<see cref="ResourceManagerRuntimeData.kCatalogAddress"/>) this
        /// includes asset-bundle build hashes. For all catalog ids, content-directory build hashes
        /// (when <c>ENABLE_CONTENT_DIRECTORIES</c> is set) matching the catalog id are also included.
        /// A bundled-only build produces a byte-identical hash to the previous implementation.
        /// </summary>
        private string ComputeCatalogBuildHash(string catalogId, AddressablesPlayerBuildResult addrResult)
        {
            if (addrResult == null)
                return null;

            var allHashes = new List<object>();

            if (catalogId == ResourceManagerRuntimeData.kCatalogAddress)
                foreach (var r in addrResult.AssetBundleBuildResults)
                    allHashes.Add(r.Hash);

#if ENABLE_CONTENT_DIRECTORIES
            foreach (var r in addrResult.ContentDirectoryBuildResults)
                if (r.CatalogName == catalogId)
                    allHashes.Add(r.Hash);
#endif

            return HashingMethods.Calculate(allHashes.ToArray()).ToString();
        }

        /// <summary>
        /// Generates the runtime settings JSON file that configures Addressables at runtime.
        /// </summary>
        /// <param name="aaContext">The Addressables build context containing runtime data.</param>
        /// <param name="builderInput">The build input containing settings and registry.</param>
        /// <returns>The path to the generated settings file.</returns>
        private string GenerateRuntimeSettingsFile(AddressableAssetsBuildContext aaContext, AddressablesDataBuilderInput builderInput)
        {
            using (Log.ScopedStep(LogLevel.Info, "Generate Settings"))
            {
                var settingsPath = Addressables.BuildPath + "/" + builderInput.RuntimeSettingsFilename;
                builderInput.Registry.WriteAndAddFile(settingsPath, JsonUtility.ToJson(aaContext.runtimeData));
                return settingsPath;
            }
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
            if (assetGroup == null || !assetGroup.IncludeInBuild)
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
                using (Log.ScopedStep(LogLevel.Verbose, "ProcessGroupSchema",
                           ("Name", schema.GetType().FullName),
                           ("Enabled", schema.IsEnabled.ToString())))
                {
                    string errorString = "";
                    if (schema.IsEnabled && assetGroup.IncludeInBuild)
                    {
                        errorString = schema.CanEnableSchema();
                        if (!string.IsNullOrEmpty(errorString))
                            return errorString;
                    }

                    errorString = ProcessGroupSchema(schema, assetGroup, aaContext);
                    if (!string.IsNullOrEmpty(errorString))
                        return errorString;
                }
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
                var errorString = schemaBuilder.ProcessGroupSchema(aaContext, schema);
                if (errorString != string.Empty)
                    return errorString;
            }
            return string.Empty;
        }

        /// <summary>
        /// A temporary list of the groups that get processed during a build.
        /// </summary>
        List<AddressableAssetGroup> m_IncludedGroupsInBuild;

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
            var labelStringBuilder = new StringBuilder();
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
                            foreach (var l in a.labels)
                                labelStringBuilder.Append(l);
                            var key = labelStringBuilder.ToString();
                            labelStringBuilder.Clear();

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

        private static void GenerateBuildInputDefinitions(List<AddressableAssetEntry> allEntries, List<AssetBundleBuild> buildInputDefs, string groupGuid, string address,
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
                    var settings = AddressableAssetSettingsDefaultObject.Settings;
                    var catalogExt = settings?.CatalogProviderType != null
                        ? $".{CreateCatalogBuilder(settings).CatalogExtension}"
                        : ".bin";
                    var catalogPath = Addressables.BuildPath + "/catalog" + catalogExt;
                    DeleteFile(catalogPath);
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

            return true;
        }

        internal ICatalogBuilder CreateCatalogBuilderForTest(AddressableAssetSettings settings) => CreateCatalogBuilder(settings);
    }
}
