using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace UnityEditor.AddressableAssets.Build.AnalyzeRules
{
    /// <summary>
    /// Base class for handling analyzing bundle rules tasks and checking dependencies
    /// </summary>
    public class BundleRuleBase : AnalyzeRule
    {
        [NonSerialized]
        internal Dictionary<string, List<GUID>> m_ResourcesToDependencies = new Dictionary<string, List<GUID>>();

        [NonSerialized]
        internal readonly List<ContentCatalogDataEntry> m_Locations = new List<ContentCatalogDataEntry>();

        [NonSerialized]
        internal readonly List<AssetBundleBuild> m_AllBundleInputDefs = new List<AssetBundleBuild>();

        [NonSerialized]
        internal readonly Dictionary<string, string> m_BundleToAssetGroup = new Dictionary<string, string>();

        [NonSerialized]
        internal List<AddressableAssetEntry> m_AssetEntries = new List<AddressableAssetEntry>();

        [NonSerialized]
        internal ExtractDataTask m_ExtractData = null;

        /// <summary>
        /// The BuildTask used to extract write data from the build.
        /// </summary>
        protected ExtractDataTask ExtractData => m_ExtractData;
        /// <summary>
        /// A mapping of resources to a list of guids that correspond to their dependencies
        /// </summary>
        protected Dictionary<string, List<GUID>> ResourcesToDependencies => m_ResourcesToDependencies;
        /// <summary>
        /// A list of all the Asset Bundle input definitions. Each entry is an Asset Bundle build map
        /// </summary>
        protected internal List<AssetBundleBuild> AllBundleInputDefs => m_AllBundleInputDefs;

        internal IList<IBuildTask> RuntimeDataBuildTasks(string builtinShaderBundleName)
        {
            IList<IBuildTask> buildTasks = new List<IBuildTask>();

            // Setup
            buildTasks.Add(new SwitchToBuildPlatform());
            buildTasks.Add(new RebuildSpriteAtlasCache());

            // Player Scripts
            buildTasks.Add(new BuildPlayerScripts());

            // Dependency
            buildTasks.Add(new CalculateSceneDependencyData());
            buildTasks.Add(new CalculateAssetDependencyData());
            buildTasks.Add(new StripUnusedSpriteSources());
            buildTasks.Add(new CreateBuiltInBundle(builtinShaderBundleName));

            // Packing
            buildTasks.Add(new GenerateBundlePacking());
            buildTasks.Add(new UpdateBundleObjectLayout());

            buildTasks.Add(new GenerateBundleCommands());
            buildTasks.Add(new GenerateSubAssetPathMaps());
            buildTasks.Add(new GenerateBundleMaps());

            buildTasks.Add(new GenerateLocationListsTask());

            return buildTasks;
        }

        /// <summary>
        /// Get context for current Addressables settings
        /// </summary>
        /// <param name="settings"> The current Addressables settings object </param>
        /// <returns> The build context information </returns>
        protected internal AddressableAssetsBuildContext GetBuildContext(AddressableAssetSettings settings)
        {
            ResourceManagerRuntimeData runtimeData = new ResourceManagerRuntimeData();
            runtimeData.LogResourceManagerExceptions = settings.buildSettings.LogResourceManagerExceptions;

            var aaContext = new AddressableAssetsBuildContext
            {
                Settings = settings,
                runtimeData = runtimeData,
                bundleToAssetGroup = m_BundleToAssetGroup,
                locations = m_Locations,
                providerTypes = new HashSet<Type>(),
                assetEntries = m_AssetEntries,
                assetGroupToBundles = new Dictionary<AddressableAssetGroup, List<string>>()
            };
            return aaContext;
        }

        /// <summary>
        /// Check path is valid path for Addressables entry
        /// </summary>
        /// <param name="path"> The path to check</param>
        /// <returns>Whether path is valid</returns>
        protected bool IsValidPath(string path)
        {
            return AddressableAssetUtility.IsPathValidForEntry(path) &&
                   !AddressableAssetUtility.StringContains(path, "/Resources/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Refresh build to check bundles against current rules
        /// </summary>
        /// <param name="buildContext"> Context information for building</param>
        /// <returns> The return code of whether analyze build was successful, </returns>
        protected internal ReturnCode RefreshBuild(AddressableAssetsBuildContext buildContext)
        {
            var settings = buildContext.Settings;
            var context = new AddressablesDataBuilderInput(settings);

            var buildTarget = context.Target;
            var buildTargetGroup = context.TargetGroup;
            var buildParams = new AddressableAssetsBundleBuildParameters(settings, m_BundleToAssetGroup, buildTarget,
                buildTargetGroup, settings.buildSettings.bundleBuildPath);
            var builtinShaderBundleName =
                settings.DefaultGroup.Name.ToLower().Replace(" ", "").Replace('\\', '/').Replace("//", "/") +
                "_unitybuiltinshaders.bundle";
            var buildTasks = RuntimeDataBuildTasks(builtinShaderBundleName);
            m_ExtractData = new ExtractDataTask();
            buildTasks.Add(m_ExtractData);

            IBundleBuildResults buildResults;
            var exitCode = ContentPipeline.BuildAssetBundles(buildParams, new BundleBuildContent(m_AllBundleInputDefs),
                out buildResults, buildTasks, buildContext);

            return exitCode;
        }

        /// <summary>
        /// Get dependencies from bundles
        /// </summary>
        /// <returns> The list of GUIDs of bundle dependencies</returns>
        protected List<GUID> GetAllBundleDependencies()
        {
            if (m_ExtractData == null)
            {
                Debug.LogError("Build not run, RefreshBuild needed before GetAllBundleDependencies");
                return new List<GUID>();
            }

            var explicitGuids = m_ExtractData.WriteData.AssetToFiles.Keys;
            var implicitGuids = GetImplicitGuidToFilesMap().Keys;
            var allBundleGuids = explicitGuids.Union(implicitGuids);

            return allBundleGuids.ToList();
        }

        /// <summary>
        /// Add Resource and Bundle dependencies in common to map of resources to dependencies
        /// </summary>
        /// <param name="bundleDependencyGuids"> GUID list of bundle dependencies</param>
        protected internal void IntersectResourcesDepedenciesWithBundleDependencies(List<GUID> bundleDependencyGuids)
        {
            foreach (var key in m_ResourcesToDependencies.Keys)
            {
                var bundleDependencies = bundleDependencyGuids.Intersect(m_ResourcesToDependencies[key]).ToList();

                m_ResourcesToDependencies[key].Clear();
                m_ResourcesToDependencies[key].AddRange(bundleDependencies);
            }
        }

        /// <summary>
        /// Build map of resources to corresponding dependencies
        /// </summary>
        /// <param name="resourcePaths"> Array of resource paths</param>
        protected internal virtual void BuiltInResourcesToDependenciesMap(string[] resourcePaths)
        {
            for (int sceneIndex = 0; sceneIndex < resourcePaths.Length; ++sceneIndex)
            {
                string path = resourcePaths[sceneIndex];
                if (EditorUtility.DisplayCancelableProgressBar("Generating built-in resource dependency map",
                        "Checking " + path + " for duplicates with Addressables content.",
                        (float)sceneIndex / resourcePaths.Length))
                {
                    m_ResourcesToDependencies.Clear();
                    EditorUtility.ClearProgressBar();
                    return;
                }

                string[] dependencies;
                if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    using (var w = new BuildInterfacesWrapper())
                    {
                        var usageTags = new BuildUsageTagSet();
                        BuildSettings settings = new BuildSettings
                        {
                            group = EditorUserBuildSettings.selectedBuildTargetGroup,
                            target = EditorUserBuildSettings.activeBuildTarget,
                            typeDB = null,
                            buildFlags = ContentBuildFlags.None
                        };

                        SceneDependencyInfo sceneInfo =
                            ContentBuildInterface.CalculatePlayerDependenciesForScene(path, settings, usageTags);
                        dependencies = new string[sceneInfo.referencedObjects.Count];
                        for (int i = 0; i < sceneInfo.referencedObjects.Count; ++i)
                        {
                            if (string.IsNullOrEmpty(sceneInfo.referencedObjects[i].filePath))
                                dependencies[i] = AssetDatabase.GUIDToAssetPath(sceneInfo.referencedObjects[i].guid.ToString());
                            else
                                dependencies[i] = sceneInfo.referencedObjects[i].filePath;
                        }
                    }
                }
                else
                    dependencies = AssetDatabase.GetDependencies(path);

                if (!m_ResourcesToDependencies.ContainsKey(path))
                    m_ResourcesToDependencies.Add(path, new List<GUID>(dependencies.Length));
                else
                    m_ResourcesToDependencies[path].Capacity += dependencies.Length;

                foreach (string dependency in dependencies)
                {
                    if (dependency.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || dependency.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        continue;
                    m_ResourcesToDependencies[path].Add(new GUID(AssetDatabase.AssetPathToGUID(dependency)));
                }
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Use bundle names to create group names for AssetBundleBuild
        /// </summary>
        /// <param name="buildContext">Context information for building</param>
        protected internal void ConvertBundleNamesToGroupNames(AddressableAssetsBuildContext buildContext)
        {
            if (m_ExtractData == null)
            {
                Debug.LogError("Build not run, RefreshBuild needed before ConvertBundleNamesToGroupNames");
                return;
            }

            Dictionary<string, string> bundleNamesToUpdate = new Dictionary<string, string>();

            foreach (var assetGroup in buildContext.Settings.groups)
            {
                if (assetGroup == null)
                    continue;

                List<string> bundles;
                if (buildContext.assetGroupToBundles.TryGetValue(assetGroup, out bundles))
                {
                    foreach (string bundle in bundles)
                    {
                        var keys = m_ExtractData.WriteData.FileToBundle.Keys.Where(key => m_ExtractData.WriteData.FileToBundle[key] == bundle);
                        foreach (string key in keys)
                            bundleNamesToUpdate.Add(key, assetGroup.Name);
                    }
                }
            }

            foreach (string key in bundleNamesToUpdate.Keys)
            {
                var bundleName = m_ExtractData.WriteData.FileToBundle[key];
                string convertedName = ConvertBundleName(bundleName, bundleNamesToUpdate[key]);
                if (m_ExtractData.WriteData.FileToBundle.ContainsKey(key))
                    m_ExtractData.WriteData.FileToBundle[key] = convertedName;
                for (int i = 0; i < m_AllBundleInputDefs.Count; ++i)
                {
                    if (m_AllBundleInputDefs[i].assetBundleName == bundleName)
                    {
                        var input = m_AllBundleInputDefs[i];
                        input.assetBundleName = convertedName;
                        m_AllBundleInputDefs[i] = input;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Generate input definitions and entries for AssetBundleBuild
        /// </summary>
        /// <param name="settings">The current Addressables settings object</param>
        protected internal void CalculateInputDefinitions(AddressableAssetSettings settings)
        {
            int updateFrequency = Mathf.Max(settings.groups.Count / 10, 1);
            bool progressDisplayed = false;
            for (int groupIndex = 0; groupIndex < settings.groups.Count; ++groupIndex)
            {
                AddressableAssetGroup group = settings.groups[groupIndex];
                if (group == null)
                    continue;
                if (!progressDisplayed || groupIndex % updateFrequency == 0)
                {
                    progressDisplayed = true;
                    if (EditorUtility.DisplayCancelableProgressBar("Calculating Input Definitions", "",
                            (float)groupIndex / settings.groups.Count))
                    {
                        m_AssetEntries.Clear();
                        m_BundleToAssetGroup.Clear();
                        m_AllBundleInputDefs.Clear();
                        break;
                    }
                }

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema != null && schema.IncludeInBuild)
                {
                    List<AssetBundleBuild> bundleInputDefinitions = new List<AssetBundleBuild>();
                    m_AssetEntries.AddRange(BuildScriptPackedMode.PrepGroupBundlePacking(group, bundleInputDefinitions, schema));

                    for (int i = 0; i < bundleInputDefinitions.Count; i++)
                    {
                        if (m_BundleToAssetGroup.ContainsKey(bundleInputDefinitions[i].assetBundleName))
                            bundleInputDefinitions[i] = CreateUniqueBundle(bundleInputDefinitions[i]);

                        m_BundleToAssetGroup.Add(bundleInputDefinitions[i].assetBundleName, schema.Group.Guid);
                    }

                    m_AllBundleInputDefs.AddRange(bundleInputDefinitions);
                }
            }

            if (progressDisplayed)
                EditorUtility.ClearProgressBar();
        }

        internal AssetBundleBuild CreateUniqueBundle(AssetBundleBuild bid)
        {
            return CreateUniqueBundle(bid, m_BundleToAssetGroup);
        }

        /// <summary>
        /// Create new AssetBundleBuild
        /// </summary>
        /// <param name="bid">ID for new AssetBundleBuild</param>
        /// <param name="bundleToAssetGroup"> Map of bundle names to asset group Guids</param>
        /// <returns>Asset Bundle build map entry</returns>
        protected internal static AssetBundleBuild CreateUniqueBundle(AssetBundleBuild bid, Dictionary<string, string> bundleToAssetGroup)
        {
            int count = 1;
            var newName = bid.assetBundleName;
            while (bundleToAssetGroup.ContainsKey(newName) && count < 1000)
                newName = bid.assetBundleName.Replace(".bundle", string.Format("{0}.bundle", count++));
            return new AssetBundleBuild
            {
                assetBundleName = newName,
                addressableNames = bid.addressableNames,
                assetBundleVariant = bid.assetBundleVariant,
                assetNames = bid.assetNames
            };
        }

        /// <summary>
        /// Get bundle's object ids that have no dependency file
        /// </summary>
        /// <param name="fileName"> Name of bundle file </param>
        /// <returns> List of GUIDS of objects in bundle with no dependency file</returns>
        protected List<GUID> GetImplicitGuidsForBundle(string fileName)
        {
            if (m_ExtractData == null)
            {
                Debug.LogError("Build not run, RefreshBuild needed before GetImplicitGuidsForBundle");
                return new List<GUID>();
            }

            List<GUID> guids = (from id in m_ExtractData.WriteData.FileToObjects[fileName]
                where !m_ExtractData.WriteData.AssetToFiles.Keys.Contains(id.guid)
                select id.guid).ToList();
            return guids;
        }

        /// <summary>
        /// Build map of implicit guids to their bundle files
        /// </summary>
        /// <returns> Dictionary of implicit guids to their corresponding file</returns>
        protected internal Dictionary<GUID, List<string>> GetImplicitGuidToFilesMap()
        {
            if (m_ExtractData == null)
            {
                Debug.LogError("Build not run, RefreshBuild needed before GetImplicitGuidToFilesMap");
                return new Dictionary<GUID, List<string>>();
            }

            Dictionary<GUID, List<string>> implicitGuids = new Dictionary<GUID, List<string>>();
            IEnumerable<KeyValuePair<ObjectIdentifier, string>> validImplicitGuids =
                from fileToObject in m_ExtractData.WriteData.FileToObjects
                from objectId in fileToObject.Value
                where !m_ExtractData.WriteData.AssetToFiles.Keys.Contains(objectId.guid)
                select new KeyValuePair<ObjectIdentifier, string>(objectId, fileToObject.Key);

            //Build our Dictionary from our list of valid implicit guids (guids not already in explicit guids)
            foreach (var objectIdToFile in validImplicitGuids)
            {
                if (!implicitGuids.ContainsKey(objectIdToFile.Key.guid))
                    implicitGuids.Add(objectIdToFile.Key.guid, new List<string>());
                implicitGuids[objectIdToFile.Key.guid].Add(objectIdToFile.Value);
            }

            return implicitGuids;
        }

        /// <summary>
        /// Calculate built in resources and corresponding bundle dependencies
        /// </summary>
        /// <param name="settings">The current Addressables settings object</param>
        /// <param name="builtInResourcesPaths">Array of resource paths</param>
        /// <returns>List of rule results after calculating resource and bundle dependency combined</returns>
        protected List<AnalyzeResult> CalculateBuiltInResourceDependenciesToBundleDependecies(AddressableAssetSettings settings, string[] builtInResourcesPaths)
        {
            List<AnalyzeResult> results = new List<AnalyzeResult>();

            if (!BuildUtility.CheckModifiedScenesAndAskToSave())
            {
                Debug.LogError("Cannot run Analyze with unsaved scenes");
                results.Add(new AnalyzeResult {resultName = ruleName + "Cannot run Analyze with unsaved scenes"});
                return results;
            }

            EditorUtility.DisplayProgressBar("Calculating Built-in dependencies", "Calculating dependencies between Built-in resources and Addressables", 0);
            try
            {
                // bulk of work and progress bars displayed in these methods
                var buildSuccess = BuildAndGetResourceDependencies(settings, builtInResourcesPaths);
                if (buildSuccess != ReturnCode.Success)
                {
                    if (buildSuccess == ReturnCode.SuccessNotRun)
                    {
                        results.Add(new AnalyzeResult {resultName = ruleName + " - No issues found."});
                        return results;
                    }

                    results.Add(new AnalyzeResult {resultName = ruleName + "Analyze build failed. " + buildSuccess});
                    return results;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            results = (from resource in m_ResourcesToDependencies.Keys
                from dependency in m_ResourcesToDependencies[resource]
                let assetPath = AssetDatabase.GUIDToAssetPath(dependency.ToString())
                let files = m_ExtractData.WriteData.FileToObjects.Keys
                from file in files
                where m_ExtractData.WriteData.FileToObjects[file].Any(oid => oid.guid == dependency)
                where m_ExtractData.WriteData.FileToBundle.ContainsKey(file)
                let bundle = m_ExtractData.WriteData.FileToBundle[file]
                select new AnalyzeResult
                {
                    resultName =
                        resource + kDelimiter +
                        bundle + kDelimiter +
                        assetPath,
                    severity = MessageType.Warning
                }).ToList();

            if (results.Count == 0)
                results.Add(new AnalyzeResult {resultName = ruleName + " - No issues found."});

            return results;
        }

        /// <summary>
        /// Calculates and gathers dependencies for built in data.
        /// </summary>
        /// <param name="settings">The AddressableAssetSettings to pull data from.</param>
        /// <param name="builtInResourcesPaths">The paths that lead to all the built in Resource locations</param>
        /// <returns>A ReturnCode indicating various levels of success or failure.</returns>
        protected ReturnCode BuildAndGetResourceDependencies(AddressableAssetSettings settings, string[] builtInResourcesPaths)
        {
            BuiltInResourcesToDependenciesMap(builtInResourcesPaths);
            if (m_ResourcesToDependencies == null || m_ResourcesToDependencies.Count == 0)
                return ReturnCode.SuccessNotRun;

            CalculateInputDefinitions(settings);
            if (m_AllBundleInputDefs == null || m_AllBundleInputDefs.Count == 0)
                return ReturnCode.SuccessNotRun;

            EditorUtility.DisplayProgressBar("Calculating Built-in dependencies",
                "Calculating dependencies between Built-in resources and Addressables", 0.5f);

            ReturnCode exitCode = ReturnCode.Error;
            var context = GetBuildContext(settings);
            exitCode = RefreshBuild(context);
            if (exitCode < ReturnCode.Success)
            {
                EditorUtility.ClearProgressBar();
                return exitCode;
            }

            EditorUtility.DisplayProgressBar("Calculating Built-in dependencies",
                "Calculating dependencies between Built-in resources and Addressables", 0.9f);
            IntersectResourcesDepedenciesWithBundleDependencies(GetAllBundleDependencies());
            ConvertBundleNamesToGroupNames(context);

            return exitCode;
        }

        /// <summary>
        /// Convert bundle name to include group name
        /// </summary>
        /// <param name="bundleName">Current bundle name</param>
        /// <param name="groupName">Group name of bundle's group</param>
        /// <returns>The new bundle name</returns>
        protected string ConvertBundleName(string bundleName, string groupName)
        {
            string[] bundleNameSegments = bundleName.Split('_');
            bundleNameSegments[0] = groupName.Replace(" ", "").ToLower();
            return string.Join("_", bundleNameSegments);
        }

        /// <summary>
        /// Clear all previously gathered bundle data and analysis
        /// </summary>
        public override void ClearAnalysis()
        {
            m_Locations.Clear();
            m_AssetEntries.Clear();
            m_AllBundleInputDefs.Clear();
            m_BundleToAssetGroup.Clear();
            m_ResourcesToDependencies.Clear();
            m_ResultData = null;
            m_ExtractData = null;

            base.ClearAnalysis();
        }

        /// <summary>
        /// Data object for results of resource based analysis rules
        /// </summary>
        protected internal struct ResultData
        {
            /// <summary>
            /// The path of the resource file
            /// </summary>
            public string ResourcePath;

            /// <summary>
            /// The name of the Asset Bundle
            /// </summary>
            public string AssetBundleName;

            /// <summary>
            /// The path of a Unity asset
            /// </summary>
            public string AssetPath;
        }

        /// <inheritdoc />
        public override bool CanFix
        {
            get { return false; }
        }

        private List<ResultData> m_ResultData = null;

        /// <summary>
        /// Duplicate Results between Addressables and Player content.
        /// </summary>
        protected IEnumerable<ResultData> Results
        {
            get
            {
                if (m_ResultData == null)
                {
                    if (ExtractData == null)
                    {
                        Debug.LogError("RefreshAnalysis needs to be called before getting results");
                        return new List<ResultData>(0);
                    }

                    m_ResultData = new List<ResultData>(512);

                    foreach (string resource in ResourcesToDependencies.Keys)
                    {
                        var dependencies = ResourcesToDependencies[resource];
                        foreach (GUID dependency in dependencies)
                        {
                            string assetPath = AssetDatabase.GUIDToAssetPath(dependency.ToString());
                            var files = ExtractData.WriteData.FileToObjects.Keys;
                            foreach (string file in files)
                            {
                                if (m_ExtractData.WriteData.FileToObjects[file].Any(oid => oid.guid == dependency) &&
                                    m_ExtractData.WriteData.FileToBundle.ContainsKey(file))
                                {
                                    string assetBundleName = ExtractData.WriteData.FileToBundle[file];
                                    m_ResultData.Add(new ResultData()
                                    {
                                        AssetBundleName = assetBundleName,
                                        AssetPath = assetPath,
                                        ResourcePath = resource
                                    });
                                }
                            }
                        }
                    }
                }

                return m_ResultData;
            }
        }

        /// <summary>
        /// Clear analysis and calculate built in content and corresponding bundle dependencies
        /// </summary>
        /// <param name="settings">The current Addressables settings object</param>
        /// <returns>List of results from analysis</returns>
        public override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
        {
            ClearAnalysis();
            List<AnalyzeResult> results = new List<AnalyzeResult>();

            if (!BuildUtility.CheckModifiedScenesAndAskToSave())
            {
                Debug.LogError("Cannot run Analyze with unsaved scenes");
                results.Add(new AnalyzeResult {resultName = ruleName + "Cannot run Analyze with unsaved scenes"});
                return results;
            }

            EditorUtility.DisplayProgressBar("Calculating Built-in dependencies", "Calculating dependencies between Resources and Addressables", 0);
            try
            {
                // bulk of work and progress bars displayed in these methods
                string[] resourcePaths = GetResourcePaths();

                var buildSuccess = BuildAndGetResourceDependencies(settings, resourcePaths);
                if (buildSuccess == ReturnCode.SuccessNotRun)
                {
                    results.Add(new AnalyzeResult {resultName = ruleName + " - No issues found."});
                    return results;
                }

                if (buildSuccess != ReturnCode.Success)
                {
                    results.Add(new AnalyzeResult {resultName = ruleName + "Analyze build failed. " + buildSuccess});
                    return results;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            foreach (ResultData result in Results)
            {
                results.Add(new AnalyzeResult()
                {
                    resultName =
                        result.ResourcePath + kDelimiter +
                        result.AssetBundleName + kDelimiter +
                        result.AssetPath,
                    severity = MessageType.Warning
                });
            }

            return results;
        }

        /// <summary>
        /// Gets an array of resource paths that are to be compared against the addressables build content
        /// </summary>
        /// <returns>Array of Resource paths to compare against</returns>
        internal protected virtual string[] GetResourcePaths()
        {
            return new string[0];
        }

        /// <inheritdoc />
        public override void FixIssues(AddressableAssetSettings settings)
        {
            //Do nothing.  There's nothing to fix.
        }
    }
}
