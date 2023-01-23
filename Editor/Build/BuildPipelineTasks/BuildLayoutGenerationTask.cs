using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets.Build.BuildPipelineTasks
{
    /// <summary>
    /// The BuildTask used to generate the bundle layout.
    /// </summary>
    public class BuildLayoutGenerationTask : IBuildTask
    {
        const int k_Version = 1;
        const bool k_PrettyPrint = false;

        internal static Action<string, BuildLayout> s_LayoutCompleteCallback;

        /// <summary>
        /// The GenerateLocationListsTask version.
        /// </summary>
        public int Version
        {
            get { return k_Version; }
        }

        /// <summary>
        /// The mapping of the old to new bundle names.
        /// </summary>
        public Dictionary<string, string> BundleNameRemap
        {
            get { return m_BundleNameRemap; }
            set { m_BundleNameRemap = value; }
        }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IAddressableAssetsBuildContext m_AaBuildContext;

        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;

        [InjectContext]
        IBundleWriteData m_WriteData;

        [InjectContext(ContextUsage.In, true)]
        IBuildLogger m_Log;

        [InjectContext]
        IBuildResults m_Results;

        [InjectContext(ContextUsage.In)]
        IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.In)]
        IBundleBuildResults m_BuildBundleResults;
#pragma warning restore 649

        internal Dictionary<string, string> m_BundleNameRemap;
        internal AddressablesDataBuilderInput m_AddressablesInput;
        internal ContentCatalogData m_ContentCatalogData;

        private bool IsContentUpdateBuild => m_AddressablesInput != null && m_AddressablesInput.PreviousContentState != null;

        internal static string m_LayoutFileName = "buildlayout";
        internal static string m_LayoutFilePath = $"{Addressables.LibraryPath}{m_LayoutFileName}";
        internal static string m_ReportsFilePath = $"{Addressables.BuildReportPath}{m_LayoutFileName}";

        internal static string GetLayoutFilePathForFormat(ProjectConfigData.ReportFileFormat fileFormat)
        {
            string ext = (fileFormat == ProjectConfigData.ReportFileFormat.JSON) ? "json" : "txt";
            return $"{m_LayoutFilePath}.{ext}";
        }

        internal static string TimeStampedReportPath(DateTime now)
        {
            string stringNow = string.Format("{0:D4}.{1:D2}.{2:D2}.{3:D2}.{4:D2}.{5:D2}", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
            return $"{m_ReportsFilePath}_{stringNow}.json";
        }

        static AssetBucket GetOrCreate(Dictionary<string, AssetBucket> buckets, string asset)
        {
            if (!buckets.TryGetValue(asset, out AssetBucket bucket))
            {
                bucket = new AssetBucket();
                bucket.guid = asset;
                buckets.Add(asset, bucket);
            }

            return bucket;
        }

        class AssetBucket
        {
            public string guid;
            public bool isFilePathBucket;
            public List<ObjectSerializedInfo> objs = new List<ObjectSerializedInfo>();
            public BuildLayout.ExplicitAsset ExplictAsset;

            public ulong CalcObjectSize()
            {
                ulong sum = 0;
                foreach (var obj in objs)
                    sum += obj.header.size;
                return sum;
            }

            public ulong CalcStreamedSize()
            {
                ulong sum = 0;
                foreach (var obj in objs)
                    sum += obj.rawData.size;
                return sum;
            }
        }

        AssetType GetSceneObjectType(string name)
        {
            if (AssetType.TryParse<AssetType>(name, true, out var rst))
                return rst;
            //TODO: I'm removing this warning for now since it can spam the logs during a build. 
            //If we want to re-add it, we need to do so carefully.  
            //Debug.LogWarning($"Unhandled Scene Object type {name}");
            return AssetType.SceneObject;
        }

        private BuildLayout CreateBuildLayout()
        {
            AddressableAssetsBuildContext aaContext = (AddressableAssetsBuildContext)m_AaBuildContext;

            LayoutLookupTables lookup = null;
            BuildLayout result = null;
            using (m_Log.ScopedStep(LogLevel.Info, "Generate Lookup tables"))
                lookup = GenerateLookupTables(aaContext);
            using (m_Log.ScopedStep(LogLevel.Info, "Generate Build Layout"))
                result = GenerateBuildLayout(aaContext, lookup);
            return result;
        }

        private LayoutLookupTables GenerateLookupTables(AddressableAssetsBuildContext aaContext)
        {
            LayoutLookupTables lookup = new LayoutLookupTables();

            foreach (string bundleName in m_WriteData.FileToBundle.Values.Distinct())
            {
                BuildLayout.Bundle bundle = new BuildLayout.Bundle();
                if (m_BuildBundleResults.BundleInfos.TryGetValue(bundleName, out var info))
                {
                    bundle.CRC = info.Crc;
                    bundle.Hash = info.Hash;
                }

                bundle.Name = bundleName;
                UnityEngine.BuildCompression compression = m_Parameters.GetCompressionForIdentifier(bundle.Name);
                bundle.Compression = compression.compression.ToString();
                lookup.Bundles.Add(bundleName, bundle);
            }

            foreach (BuildLayout.Bundle b in lookup.Bundles.Values)
            {
                if (aaContext.bundleToImmediateBundleDependencies.TryGetValue(b.Name, out List<string> deps))
                    b.Dependencies = deps.Select(x => lookup.Bundles[x]).Where(x => b != x).ToList();
                if (aaContext.bundleToExpandedBundleDependencies.TryGetValue(b.Name, out List<string> deps2))
                    b.ExpandedDependencies = deps2.Select(x => lookup.Bundles[x]).Where(x => b != x).ToList();
            }

            // create files
            foreach (KeyValuePair<string, string> fileBundle in m_WriteData.FileToBundle)
            {
                BuildLayout.Bundle bundle = lookup.Bundles[fileBundle.Value];
                BuildLayout.File f = new BuildLayout.File();
                f.Name = fileBundle.Key;
                f.Bundle = bundle;

                WriteResult result = m_Results.WriteResults[f.Name];
                foreach (ResourceFile rf in result.resourceFiles)
                {
                    var sf = new BuildLayout.SubFile();
                    sf.IsSerializedFile = rf.serializedFile;
                    sf.Name = rf.fileAlias;
                    sf.Size = (ulong)new FileInfo(rf.fileName).Length;
                    f.SubFiles.Add(sf);
                }

                bundle.Files.Add(f);
                lookup.Files.Add(f.Name, f);
            }

            // create assets
            foreach (KeyValuePair<GUID, List<string>> assetFile in m_WriteData.AssetToFiles)
            {
                BuildLayout.File file = lookup.Files[assetFile.Value[0]];
                BuildLayout.ExplicitAsset a = new BuildLayout.ExplicitAsset();
                a.Guid = assetFile.Key.ToString();
                a.AssetPath = AssetDatabase.GUIDToAssetPath(a.Guid);
                a.File = file;
                a.Bundle = file.Bundle;
                file.Assets.Add(a);
                lookup.GuidToExplicitAsset.Add(a.Guid, a);
            }

            Dictionary<string, List<BuildLayout.DataFromOtherAsset>> guidToPulledInBuckets =
                new Dictionary<string, List<BuildLayout.DataFromOtherAsset>>();

            foreach (BuildLayout.File file in lookup.Files.Values)
            {
                Dictionary<string, AssetBucket> buckets = new Dictionary<string, AssetBucket>();
                WriteResult writeResult = m_Results.WriteResults[file.Name];
                List<ObjectSerializedInfo> sceneObjects = new List<ObjectSerializedInfo>();

                foreach (ObjectSerializedInfo info in writeResult.serializedObjects)
                {
                    if (info.serializedObject.guid.Empty())
                    {
                        if (info.serializedObject.filePath.Equals("temp:/assetbundle", StringComparison.OrdinalIgnoreCase))
                        {
                            file.BundleObjectInfo = new BuildLayout.AssetBundleObjectInfo();
                            file.BundleObjectInfo.Size = info.header.size;
                            continue;
                        }
                        else if (info.serializedObject.filePath.StartsWith("temp:/preloaddata",
                                     StringComparison.OrdinalIgnoreCase))
                        {
                            file.PreloadInfoSize = (int)info.header.size;
                            continue;
                        }
                        else if (info.serializedObject.filePath.StartsWith("temp:/", StringComparison.OrdinalIgnoreCase))
                        {
                            sceneObjects.Add(info);
                            continue;
                        }
                        else if (!string.IsNullOrEmpty(info.serializedObject.filePath))
                        {
                            AssetBucket pathBucket = GetOrCreate(buckets, info.serializedObject.filePath.ToString());
                            pathBucket.isFilePathBucket = true;
                            pathBucket.objs.Add(info);
                            continue;
                        }
                    }

                    AssetBucket bucket = GetOrCreate(buckets, info.serializedObject.guid.ToString());
                    bucket.objs.Add(info);
                }

                if (sceneObjects.Count > 0)
                {
                    BuildLayout.ExplicitAsset sceneAsset = file.Assets.First(x => x.AssetPath.EndsWith(".unity"));
                    AssetBucket bucket = GetOrCreate(buckets, sceneAsset.Guid);
                    bucket.objs.AddRange(sceneObjects);
                }

                // Update buckets with a reference to their explicit asset
                file.Assets.ForEach(eAsset =>
                {
                    if (!buckets.TryGetValue(eAsset.Guid, out AssetBucket b))
                        b = GetOrCreate(buckets, eAsset.Guid); // some assets might not pull in any objects
                    b.ExplictAsset = eAsset;
                });

                // Cache all object types from results to find type for when implicit asset
                Dictionary<ObjectIdentifier, Type[]> objectTypes = new Dictionary<ObjectIdentifier, Type[]>(1024);
                foreach (KeyValuePair<GUID, AssetResultData> assetResult in m_Results.AssetResults)
                {
                    foreach (var resultEntry in assetResult.Value.ObjectTypes)
                    {
                        if(!objectTypes.ContainsKey(resultEntry.Key))
                            objectTypes.Add(resultEntry.Key, resultEntry.Value);
                    }
                }

                // Create entries for buckets that are implicitly pulled in
                Dictionary<string, BuildLayout.DataFromOtherAsset> guidToOtherData =
                    new Dictionary<string, BuildLayout.DataFromOtherAsset>();

                foreach (AssetBucket bucket in buckets.Values.Where(x => x.ExplictAsset == null))
                {
                    string assetPath = bucket.isFilePathBucket ? bucket.guid : AssetDatabase.GUIDToAssetPath(bucket.guid);
                    if (assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                        assetPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        file.MonoScriptCount++;
                        file.MonoScriptSize += bucket.CalcObjectSize();
                        continue;
                    }

                    var implicitAsset = new BuildLayout.DataFromOtherAsset();
                    implicitAsset.AssetPath = assetPath;
                    implicitAsset.AssetGuid = bucket.guid;
                    implicitAsset.SerializedSize = bucket.CalcObjectSize();
                    implicitAsset.StreamedSize = bucket.CalcStreamedSize();
                    implicitAsset.ObjectCount = bucket.objs.Count;
                    implicitAsset.File = file;
                    file.OtherAssets.Add(implicitAsset);

                    if (lookup.UsedImplicits.TryGetValue(implicitAsset.AssetGuid, out var dataList))
                        dataList.Add(implicitAsset);
                    else
                        lookup.UsedImplicits.Add(implicitAsset.AssetGuid, new List<BuildLayout.DataFromOtherAsset>(){implicitAsset});

                    guidToOtherData[implicitAsset.AssetGuid] = implicitAsset;

                    if (lookup.AssetPathToTypeMap.ContainsKey(implicitAsset.AssetPath))
                        implicitAsset.MainAssetType = lookup.AssetPathToTypeMap[implicitAsset.AssetPath];
                    else
                    {
                        implicitAsset.MainAssetType = BuildLayoutHelpers.GetAssetType(AssetDatabase.GetMainAssetTypeAtPath(implicitAsset.AssetPath));
                        lookup.AssetPathToTypeMap[implicitAsset.AssetPath] = implicitAsset.MainAssetType;
                    }

                    foreach (ObjectSerializedInfo bucketObj in bucket.objs)
                    {
                        Type objType = null;
                        if (objectTypes.TryGetValue(bucketObj.serializedObject, out Type[] types) && types.Length > 0)
                            objType = types[0];

                        AssetType eType = objType == null ? AssetType.Other : BuildLayoutHelpers.GetAssetType(objType);
                        if (implicitAsset.IsScene)
                        {
                            if (eType == AssetType.Other)
                                eType = AssetType.SceneObject;
                        }

                        implicitAsset.Objects.Add(new BuildLayout.ObjectData()
                        {
                            LocalIdentifierInFile = bucketObj.serializedObject.localIdentifierInFile,
                            AssetType = eType,
                            SerializedSize = bucketObj.header.size,
                            StreamedSize = bucketObj.rawData.size
                        });
                    }

                    if (!guidToPulledInBuckets.TryGetValue(implicitAsset.AssetGuid,
                            out List<BuildLayout.DataFromOtherAsset> bucketList))
                        bucketList = guidToPulledInBuckets[implicitAsset.AssetGuid] = new List<BuildLayout.DataFromOtherAsset>();
                    bucketList.Add(implicitAsset);
                }

                // Add references
                foreach (BuildLayout.ExplicitAsset asset in file.Assets)
                {
                    AssetBucket bucket = buckets[asset.Guid];
                    asset.SerializedSize = bucket.CalcObjectSize();
                    asset.StreamedSize = bucket.CalcStreamedSize();

                    if (lookup.AssetPathToTypeMap.ContainsKey(asset.AssetPath))
                        asset.MainAssetType = lookup.AssetPathToTypeMap[asset.AssetPath];
                    else
                    {
                        asset.MainAssetType = BuildLayoutHelpers.GetAssetType(AssetDatabase.GetMainAssetTypeAtPath(asset.AssetPath));
                        lookup.AssetPathToTypeMap[asset.AssetPath] = asset.MainAssetType;
                    }

                    if (asset.MainAssetType == AssetType.GameObject)
                    {
#if UNITY_2022_2_OR_NEWER
                        Type importerType = AssetDatabase.GetImporterType(asset.AssetPath);
                        if (importerType == typeof(ModelImporter))
                            asset.MainAssetType = AssetType.Model;
                        else if (importerType != null)
#endif
                            asset.MainAssetType = AssetType.Prefab;
                    }

                    GUID.TryParse(asset.Guid, out GUID guid);
                    if (m_Results.AssetResults.TryGetValue(guid, out var data))
                        asset.AssetHash = data.Hash;

                    foreach (ObjectSerializedInfo bucketObj in bucket.objs)
                    {
                        Type objType = null;
                        if (objectTypes.TryGetValue(bucketObj.serializedObject, out Type[] types) && types.Length > 0)
                            objType = types[0];

                        AssetType eType = objType == null ? AssetType.Other : BuildLayoutHelpers.GetAssetType(objType);
                        if (asset.IsScene && eType == AssetType.Other)
                            eType = GetSceneObjectType(bucketObj.serializedObject.filePath.Remove(0, 6));

                        asset.Objects.Add(new BuildLayout.ObjectData()
                        {
                            LocalIdentifierInFile = bucketObj.serializedObject.localIdentifierInFile,
                            AssetType = eType,
                            SerializedSize = bucketObj.header.size,
                            StreamedSize = bucketObj.rawData.size
                        });
                    }

                    IEnumerable<ObjectIdentifier> refs = null;
                    if (m_DependencyData.AssetInfo.TryGetValue(new GUID(asset.Guid), out AssetLoadInfo info))
                        refs = info.referencedObjects;
                    else
                        refs = m_DependencyData.SceneInfo[new GUID(asset.Guid)].referencedObjects;
                    foreach (string refGUID in refs.Select(x => x.guid.Empty() ? x.filePath : x.guid.ToString()).Distinct())
                    {
                        if (guidToOtherData.TryGetValue(refGUID, out BuildLayout.DataFromOtherAsset dfoa))
                        {
                            dfoa.ReferencingAssets.Add(asset);
                            asset.InternalReferencedOtherAssets.Add(dfoa);
                        }
                        else if (buckets.TryGetValue(refGUID, out AssetBucket refBucket))
                        {
                            asset.InternalReferencedExplicitAssets.Add(refBucket.ExplictAsset);
                        }
                        else if (lookup.GuidToExplicitAsset.TryGetValue(refGUID, out BuildLayout.ExplicitAsset refAsset))
                        {
                            asset.ExternallyReferencedAssets.Add(refAsset);
                        }
                    }
                }
            }

            return lookup;
        }

        private BuildLayout GenerateBuildLayout(AddressableAssetsBuildContext aaContext, LayoutLookupTables lookup)
        {
            BuildLayout layout = new BuildLayout();
            layout.BuildStart = aaContext.buildStartTime;
            AddressableAssetSettings aaSettings = aaContext.Settings;
            if (m_ContentCatalogData != null)
                layout.BuildResultHash = m_ContentCatalogData.m_BuildResultHash;

            using (m_Log.ScopedStep(LogLevel.Info, "Generate Basic Information"))
            {
                SetLayoutMetaData(layout, aaSettings);
                layout.AddressablesEditorSettings = GetAddressableEditorSettings(aaSettings);
                layout.AddressablesRuntimeSettings = GetAddressableRuntimeSettings(aaContext, m_ContentCatalogData);
            }

            if (IsContentUpdateBuild)
                layout.BuildType = BuildType.UpdateBuild;
            else
                layout.BuildType = BuildType.NewBuild;

            // Map from GUID to AddrssableAssetEntry
            lookup.GuidToEntry = aaContext.assetEntries.ToDictionary(x => x.guid, x => x);

            // create groups
            foreach (AddressableAssetGroup group in aaSettings.groups)
            {
                if (group.Name != group.name)
                {
                    Debug.LogWarningFormat(
                        "Group name in settings does not match name in group asset, reset group name: \"{0}\" to \"{1}\"",
                        group.name, group.Name);
                    group.name = group.Name;
                }

                var grp = new BuildLayout.Group();
                grp.Name = group.Name;
                grp.Guid = group.Guid;
                if (group.IsDefaultGroup())
                    layout.DefaultGroup = grp;

                foreach (AddressableAssetGroupSchema schema in group.Schemas)
                {
                    var sd = GenerateSchemaData(schema, aaSettings);

                    BundledAssetGroupSchema bSchema = schema as BundledAssetGroupSchema;
                    if (bSchema != null)
                    {
                        for (int i = 0; i < sd.KvpDetails.Count; ++i)
                        {
                            if (sd.KvpDetails[i].Item1 == "BundleMode")
                            {
                                string modeStr = bSchema.BundleMode.ToString();
                                sd.KvpDetails[i] = new Tuple<string, string>("PackingMode", modeStr);
                                grp.PackingMode = modeStr;
                                break;
                            }
                        }

                        lookup.GroupNameToBuildPath[group.name] = bSchema.BuildPath.GetValue(aaSettings);
                    }

                    grp.Schemas.Add(sd);
                }

                lookup.GroupLookup.Add(group.Guid, grp);
                layout.Groups.Add(grp);
            }

            // Create a lookup for bundle update states
            foreach (ContentCatalogDataEntry entry in aaContext.locations)
            {
                if (entry.Data is AssetBundleRequestOptions options)
                {
                    lookup.BundleNameToRequestOptions.Add(options.BundleName, options);
                    lookup.BundleNameToCatalogEntry.Add(options.BundleName, entry);
                }
            }

            if (IsContentUpdateBuild)
            {
                foreach (CachedBundleState prevState in m_AddressablesInput.PreviousContentState.cachedBundles)
                {
                    if (prevState.data is AssetBundleRequestOptions options)
                        lookup.BundleNameToPreviousRequestOptions.Add(options.BundleName, options);
                }
            }

            using (m_Log.ScopedStep(LogLevel.Info, "Correlate Bundles to groups"))
            {
                foreach (BuildLayout.Bundle b in lookup.Bundles.Values)
                    CorrelateBundleToAssetGroup(layout, b, lookup, aaContext);
            }

            using (m_Log.ScopedStep(LogLevel.Info, "Apply Addressable info to layout data"))
                ApplyAddressablesInformationToExplicitAssets(layout, lookup);
            using (m_Log.ScopedStep(LogLevel.Info, "Process additional bundle data"))
                PostProcessBundleData(lookup);
            using (m_Log.ScopedStep(LogLevel.Info, "Generating implicit inclusion data"))
                AddImplicitAssetsToLayout(lookup, layout);

            SetDuration(layout);
            return layout;
        }

        BuildLayout.SchemaData GenerateSchemaData(AddressableAssetGroupSchema schema, AddressableAssetSettings aaSettings)
        {
            var sd = new BuildLayout.SchemaData();
            sd.Guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(schema));
            Type schemaType = schema.GetType();
            sd.Type = schemaType.Name;

            var properties = schemaType.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (!property.PropertyType.IsSerializable || !property.CanRead)
                    continue;
                string propertyName = property.Name;
                if (propertyName == "name" || propertyName == "hideFlags")
                    continue;

                if (property.PropertyType.IsPrimitive || property.PropertyType.IsEnum)
                {
                    object propertyObject = property.GetValue(schema);
                    if (propertyObject != null)
                        sd.KvpDetails.Add(new Tuple<string, string>(propertyName, propertyObject.ToString()));
                }
                else if (property.PropertyType == typeof(string))
                {
                    if (property.GetValue(schema) is string stringValue)
                        sd.KvpDetails.Add(new Tuple<string, string>(propertyName, stringValue));
                }
                else if (property.PropertyType == typeof(SerializedType))
                {
                    SerializedType serializeTypeValue = (SerializedType)property.GetValue(schema);
                    sd.KvpDetails.Add(new Tuple<string, string>(propertyName, serializeTypeValue.ClassName));
                }
                else if (property.PropertyType == typeof(ProfileValueReference))
                {
                    if (property.GetValue(schema) is ProfileValueReference profileValue)
                        sd.KvpDetails.Add(
                            new Tuple<string, string>(propertyName, profileValue.GetValue(aaSettings)));
                }
            }

            return sd;
        }

        void CorrelateBundleToAssetGroup(BuildLayout layout, BuildLayout.Bundle b, LayoutLookupTables lookup, AddressableAssetsBuildContext aaContext)
        {
            int indexFrom = b.Name.IndexOf(".bundle", StringComparison.Ordinal);
            string nameWithoutExtension = indexFrom > 0 ? b.Name.Remove(indexFrom) : b.Name;
            b.InternalName = nameWithoutExtension;
            if (aaContext.bundleToAssetGroup.TryGetValue(b.Name, out var grpName))
            {
                var assetGroup = lookup.GroupLookup[grpName];
                b.Name = m_BundleNameRemap[b.Name];
                b.Group = assetGroup;
                lookup.FilenameToBundle[b.Name] = b;
                var filePath = Path.Combine(lookup.GroupNameToBuildPath[assetGroup.Name], b.Name);
                if (File.Exists(filePath))
                    b.FileSize = (ulong)new FileInfo(filePath).Length;
                else if (!IsContentUpdateBuild)
                    Debug.LogWarning($"AssetBundle {b.Name} from Addressable Group \"{assetGroup.Name}\" was detected as part of the build, but the file could not be found.");
                assetGroup.Bundles.Add(b);
            }
            else
            {
                // bundleToAssetGroup doesn't contain the builtin bundles. The builtin content is built using values from the default group
                AddressableAssetGroup defaultGroup = aaContext.Settings.DefaultGroup;
                b.Name = m_BundleNameRemap[b.Name];
                b.Group = lookup.GroupLookup[defaultGroup.Guid]; // should this be set?
                lookup.FilenameToBundle[b.Name] = b;
                b.FileSize = (ulong)new FileInfo(Path.Combine(lookup.GroupNameToBuildPath[defaultGroup.Name], b.Name)).Length;
                layout.BuiltInBundles.Add(b);
            }
        }

        void PostProcessBundleData(LayoutLookupTables lookup)
        {
            HashSet<BuildLayout.Bundle> rootBundles = new HashSet<BuildLayout.Bundle>(lookup.Bundles.Values);
            foreach (BuildLayout.Bundle b in lookup.Bundles.Values)
            {
                SetBundleDataFromCatalogEntry(b, lookup);
                GenerateBundleDependencyAndEfficiencyInformation(b, rootBundles);
            }

            CalculateBundleEfficiencies(rootBundles);
        }

        internal static void CalculateBundleEfficiencies(IEnumerable<BuildLayout.Bundle> rootBundles)
        {
            Dictionary<BuildLayout.Bundle.BundleDependency, BuildLayout.Bundle.EfficiencyInfo> bundleDependencyCache = new Dictionary<BuildLayout.Bundle.BundleDependency, BuildLayout.Bundle.EfficiencyInfo>();
            foreach (BuildLayout.Bundle b in rootBundles)
                CalculateEfficiency(b, bundleDependencyCache);
        }

        /// <summary>
        /// Calculates the Efficiency of bundle and all bundles below it in the dependency tree and caches the results.
        /// Example: Given 3 bundles A, B, and C, each containing 10 assets. A depends on 2 assets in B, and B depends on 4 assets in C.
        /// The Efficiency of the dependencyLink from A->B would be 2/10 -> 20% and the ExpandedEfficiency of A->B would be (2 + 4)/(10 + 10) -> 6/20 -> 30%
        /// </summary>
        /// <param name="bundle"> the root of the dependency tree that the CalculateEfficiency call will start from. </param>
        /// <param name="bundleDependencyCache"> Cache of all bundle dependencies that have already been calculated </param>
        internal static void CalculateEfficiency(BuildLayout.Bundle bundle, Dictionary<BuildLayout.Bundle.BundleDependency, BuildLayout.Bundle.EfficiencyInfo> bundleDependencyCache = null)
        {
            Stack<BuildLayout.Bundle.BundleDependency> stk = new Stack<BuildLayout.Bundle.BundleDependency>();
            Queue<BuildLayout.Bundle> q = new Queue<BuildLayout.Bundle>();
            HashSet<BuildLayout.Bundle> seenBundles = new HashSet<BuildLayout.Bundle>();

            if (bundleDependencyCache == null)
                bundleDependencyCache = new Dictionary<BuildLayout.Bundle.BundleDependency, BuildLayout.Bundle.EfficiencyInfo>();

            q.Enqueue(bundle);

            // Populate the stack of BundleDependencies with the lowest depth BundleDependencies being at the top of the stack
            while (q.Count > 0)
            {
                var curBundle = q.Dequeue();
                foreach (var bd in curBundle.BundleDependencies)
                {
                    if (bundleDependencyCache.ContainsKey(bd))
                        break;

                    if (!seenBundles.Contains(curBundle))
                    {
                        q.Enqueue(bd.DependencyBundle);
                        stk.Push(bd);
                    }
                }
                seenBundles.Add(curBundle);
            }

            // Get the required information out of each BundleDependency, caching the necessary info for each as you work your way up the tree
            while (stk.Count > 0)
            {
                var curBd = stk.Pop();

                int totalReferencedAssetCount = 0;
                int totalDependentAssetCount = 0;
                foreach (var bd in curBd.DependencyBundle.BundleDependencies)
                {
                    if (bundleDependencyCache.TryGetValue(bd, out var ei))
                    {
                        totalReferencedAssetCount += ei.referencedAssetCount;
                        totalDependentAssetCount += ei.depAssetCount;
                    }
                }

                var newEfficiencyInfo = new BuildLayout.Bundle.EfficiencyInfo()
                {
                    depAssetCount = curBd.DependencyBundle.AssetCount + totalDependentAssetCount,
                    referencedAssetCount = curBd.uniqueReferencedAssets + totalReferencedAssetCount,
                };

                curBd.Efficiency =  newEfficiencyInfo.depAssetCount > 0 ? (float) curBd.uniqueReferencedAssets / curBd.DependencyBundle.AssetCount : 1f;
                curBd.ExpandedEfficiency = newEfficiencyInfo.depAssetCount > 0 ? (float) newEfficiencyInfo.referencedAssetCount / newEfficiencyInfo.depAssetCount : 1f;
                bundleDependencyCache[curBd] = newEfficiencyInfo;
            }
        }

        void SetBundleDataFromCatalogEntry(BuildLayout.Bundle b, LayoutLookupTables lookup)
        {
            if (lookup.BundleNameToCatalogEntry.TryGetValue(b.InternalName, out var entry))
            {
                b.LoadPath = entry.InternalId;
                b.Provider = entry.Provider;
                b.ResultType = entry.ResourceType.Name;
            }

            if (lookup.BundleNameToPreviousRequestOptions.TryGetValue(b.InternalName, out var prevOptions))
            {
                if (m_BuildBundleResults.BundleInfos.TryGetValue(b.Name, out var currentBundleDetails))
                {
                    if (currentBundleDetails.Hash.ToString() != prevOptions.Hash)
                    {
                        b.BuildStatus = BundleBuildStatus.Modified;
                        if (entry?.Data is AssetBundleRequestOptions currentOptions)
                            if (currentOptions.Hash == prevOptions.Hash)
                                b.BuildStatus = BundleBuildStatus.ModifiedUpdatePrevented;
                    }
                    else
                    {
                        b.BuildStatus = BundleBuildStatus.Unmodified;
                    }
                }
            }
        }

        void ApplyAddressablesInformationToExplicitAssets(BuildLayout layout, LayoutLookupTables lookup)
        {
            HashSet<string> loadPathsForBundle = new HashSet<string>();
            foreach (var bundle in BuildLayoutHelpers.EnumerateBundles(layout))
            {
                loadPathsForBundle.Clear();
                for (int fileIndex = 0; fileIndex < bundle.Files.Count; ++fileIndex)
                {
                    foreach (BuildLayout.ExplicitAsset rootAsset in bundle.Files[fileIndex].Assets)
                    {
                        if (lookup.GuidToEntry.TryGetValue(rootAsset.Guid, out AddressableAssetEntry rootEntry))
                        {
                            ApplyAddressablesInformationToExplicitAsset(lookup, rootAsset, rootEntry, loadPathsForBundle);
                        }
                    }
                }
            }
        }

        private static void ApplyAddressablesInformationToExplicitAsset(LayoutLookupTables lookup, BuildLayout.ExplicitAsset rootAsset, AddressableAssetEntry rootEntry, HashSet<string> loadPathsForBundle)
        {
            rootAsset.AddressableName = rootEntry.address;
            rootAsset.MainAssetType = BuildLayoutHelpers.GetAssetType(rootEntry.MainAssetType);
            rootAsset.InternalId = rootEntry.GetAssetLoadPath(true, loadPathsForBundle);
            rootAsset.Labels = new string[rootEntry.labels.Count];
            rootEntry.labels.CopyTo(rootAsset.Labels);
            rootAsset.GroupGuid = rootEntry.parentGroup.Guid;

            if (rootAsset.Bundle == null)
            {
                Debug.LogError($"Failed to get bundle information for AddressableAssetEntry: {rootEntry.AssetPath}");
                return;
            }

            foreach (BuildLayout.ExplicitAsset referencedAsset in rootAsset.ExternallyReferencedAssets)
            {
                if (referencedAsset.Bundle == null)
                {
                    Debug.LogError($"Failed to get bundle information for AddressableAssetEntry: {rootEntry.AssetPath}");
                    continue;
                }

                // Create the dependency between rootAssets bundle and referenced Assets bundle,
                rootAsset.Bundle.UpdateBundleDependency(rootAsset, referencedAsset);
            }
        }

        void GenerateBundleDependencyAndEfficiencyInformation(BuildLayout.Bundle b, HashSet<BuildLayout.Bundle> rootBundles)
        {
            b.ExpandedDependencyFileSize = 0;
            b.DependencyFileSize = 0;
            foreach (var dependency in b.Dependencies)
            {
                dependency.DependentBundles.Add(b);
                rootBundles.Remove(dependency);
                b.DependencyFileSize += dependency.FileSize;
            }

            foreach (var expandedDependency in b.ExpandedDependencies)
                b.ExpandedDependencyFileSize += expandedDependency.FileSize;

            foreach (var file in b.Files)
                b.AssetCount += file.Assets.Count;

            b.SerializeBundleToDependencyLink();
        }

        void AddImplicitAssetsToLayout(LayoutLookupTables lookup, BuildLayout layout)
        {
            foreach (KeyValuePair<string,List<BuildLayout.DataFromOtherAsset>> pair in lookup.UsedImplicits)
            {
                if (pair.Value.Count <= 1)
                    continue;

                BuildLayout.AssetDuplicationData assetDuplication = new BuildLayout.AssetDuplicationData();
                assetDuplication.AssetGuid = pair.Key;
                bool hasDuplicatedObjects = false;

                foreach (BuildLayout.DataFromOtherAsset implicitData in pair.Value)
                {
                    foreach (BuildLayout.ObjectData objectData in implicitData.Objects)
                    {
                        var existing = assetDuplication.DuplicatedObjects.Find(data => data.LocalIdentifierInFile == objectData.LocalIdentifierInFile);
                        if (existing != null)
                            existing.IncludedInBundleFiles.Add(implicitData.File);
                        else
                        {
                            assetDuplication.DuplicatedObjects.Add(
                                new BuildLayout.ObjectDuplicationData()
                                {
                                    IncludedInBundleFiles = new List<BuildLayout.File> {implicitData.File},
                                    LocalIdentifierInFile = objectData.LocalIdentifierInFile
                                });
                            hasDuplicatedObjects = true;
                        }
                    }
                }

                if (!hasDuplicatedObjects)
                    continue;

                for (int i = assetDuplication.DuplicatedObjects.Count-1; i >= 0; --i)
                {
                    if (assetDuplication.DuplicatedObjects[i].IncludedInBundleFiles.Count <= 1)
                        assetDuplication.DuplicatedObjects.RemoveAt(i);
                }

                if (assetDuplication.DuplicatedObjects.Count > 0)
                    layout.DuplicatedAssets.Add(assetDuplication);
            }
        }

        private static void SetDuration(BuildLayout layout)
        {
            var duration = DateTime.Now - layout.BuildStart;
            layout.Duration = duration.TotalSeconds;
        }

        static BuildLayout.AddressablesEditorData GetAddressableEditorSettings(AddressableAssetSettings aaSettings)
        {
            BuildLayout.AddressablesEditorData editorSettings = new BuildLayout.AddressablesEditorData();
            editorSettings.SettingsHash = aaSettings.currentHash.ToString();

            editorSettings.DisableSubAssetRepresentations = aaSettings.DisableVisibleSubAssetRepresentations;
            editorSettings.MaxConcurrentWebRequests = aaSettings.MaxConcurrentWebRequests;
            editorSettings.NonRecursiveBuilding = aaSettings.NonRecursiveBuilding;
            editorSettings.ContiguousBundles = aaSettings.ContiguousBundles;
            editorSettings.UniqueBundleIds = aaSettings.UniqueBundleIds;

            if (aaSettings.ShaderBundleNaming == ShaderBundleNaming.Custom)
                editorSettings.ShaderBundleNaming = aaSettings.ShaderBundleCustomNaming;
            else
                editorSettings.ShaderBundleNaming = aaSettings.ShaderBundleNaming.ToString();
            if (aaSettings.MonoScriptBundleNaming == MonoScriptBundleNaming.Custom)
                editorSettings.MonoScriptBundleNaming = aaSettings.MonoScriptBundleCustomNaming;
            else
                editorSettings.MonoScriptBundleNaming = aaSettings.MonoScriptBundleNaming.ToString();
            editorSettings.StripUnityVersionFromBundleBuild = aaSettings.StripUnityVersionFromBundleBuild;

            editorSettings.BuildRemoteCatalog = aaSettings.BuildRemoteCatalog;
            if (aaSettings.BuildRemoteCatalog)
                editorSettings.RemoteCatalogLoadPath = aaSettings.RemoteCatalogLoadPath.GetValue(aaSettings);
            editorSettings.CatalogRequestsTimeout = aaSettings.CatalogRequestsTimeout;
            editorSettings.BundleLocalCatalog = aaSettings.BundleLocalCatalog;
            editorSettings.OptimizeCatalogSize = aaSettings.OptimizeCatalogSize;
            editorSettings.DisableCatalogUpdateOnStartup = aaSettings.DisableCatalogUpdateOnStartup;

            var profile = aaSettings.profileSettings.GetProfile(aaSettings.activeProfileId);
            editorSettings.ActiveProfile = new BuildLayout.Profile()
            {
                Id = profile.id,
                Name = profile.profileName
            };

            editorSettings.ActiveProfile.Values = new BuildLayout.StringPair[profile.values.Count];
            for (int i = 0; i < profile.values.Count; ++i)
                editorSettings.ActiveProfile.Values[i] = (new BuildLayout.StringPair()
                    {Key = profile.values[i].id, Value = profile.values[i].value});

            return editorSettings;
        }

        private static void SetLayoutMetaData(BuildLayout layoutOut, AddressableAssetSettings aaSettings)
        {
            layoutOut.UnityVersion = Application.unityVersion;
            PackageManager.PackageInfo info = PackageManager.PackageInfo.FindForAssembly(typeof(BuildLayoutPrinter).Assembly);
            if (info != null)
                layoutOut.PackageVersion = $"{info.name}: {info.version}";
            layoutOut.BuildTarget = EditorUserBuildSettings.activeBuildTarget;
            layoutOut.BuildScript = aaSettings.ActivePlayerDataBuilder.Name;
            layoutOut.PlayerBuildVersion = aaSettings.PlayerBuildVersion;
        }

        static BuildLayout.AddressablesRuntimeData GetAddressableRuntimeSettings(AddressableAssetsBuildContext aaContext, ContentCatalogData contentCatalog)
        {
            if (aaContext.runtimeData == null)
            {
                Debug.LogError("Could not get runtime data for Addressables BuildReport");
                return null;
            }

            BuildLayout.AddressablesRuntimeData runtimeSettings = new BuildLayout.AddressablesRuntimeData();
            runtimeSettings.ProfilerEvents = aaContext.runtimeData.ProfileEvents;
            runtimeSettings.LogResourceManagerExceptions = aaContext.runtimeData.LogResourceManagerExceptions;

            runtimeSettings.CatalogLoadPaths = new List<string>();
            foreach (ResourceLocationData catalogLocation in aaContext.runtimeData.CatalogLocations)
                runtimeSettings.CatalogLoadPaths.Add(catalogLocation.InternalId);
            if (contentCatalog != null)
                runtimeSettings.CatalogHash = contentCatalog.localHash;

            return runtimeSettings;
        }

        /// <summary>
        /// Runs the build task with the injected context.
        /// </summary>
        /// <returns>The success or failure ReturnCode</returns>
        public ReturnCode Run()
        {
            BuildLayout layout = CreateBuildLayout();

            string destinationPath = TimeStampedReportPath(layout.BuildStart);
            using (m_Log.ScopedStep(LogLevel.Info, "Writing BuildReport File"))
                layout.WriteToFile(destinationPath, k_PrettyPrint);

            if (ProjectConfigData.BuildLayoutReportFileFormat == ProjectConfigData.ReportFileFormat.TXT)
            {
                using (m_Log.ScopedStep(LogLevel.Info, "Writing Layout Text File"))
                {
                    string txtFilePath = GetLayoutFilePathForFormat(ProjectConfigData.ReportFileFormat.TXT);
                    using (FileStream s = File.Open(txtFilePath, FileMode.Create))
                        BuildLayoutPrinter.WriteBundleLayout(s, layout);
                    Debug.Log($"Text build layout written to {txtFilePath} and json build layout written to {destinationPath}");
                }
            }
            else
            {
                string legacyJsonFilePath = GetLayoutFilePathForFormat(ProjectConfigData.ReportFileFormat.JSON);
                Directory.CreateDirectory(Path.GetDirectoryName(legacyJsonFilePath));
                if (File.Exists(legacyJsonFilePath))
                    File.Delete(legacyJsonFilePath);
                File.Copy(destinationPath, legacyJsonFilePath);
                Debug.Log($"Json build layout written to {legacyJsonFilePath}");
            }

            s_LayoutCompleteCallback?.Invoke(destinationPath, layout);
            return ReturnCode.Success;
        }

        /// <summary>
        /// Creates an Error report for the error provided
        /// </summary>
        /// <param name="error">Build error string</param>
        /// <returns>The success or failure ReturnCode</returns>
        public static void GenerateErrorReport(string error, AddressableAssetsBuildContext aaContext, AddressablesContentState previousContentState)
        {
            if (aaContext == null)
                return;
            AddressableAssetSettings aaSettings = aaContext.Settings;
            if (aaSettings == null)
                return;

            BuildLayout layout = new BuildLayout();
            layout.BuildStart = aaContext.buildStartTime;
            layout.BuildError = error;
            SetLayoutMetaData(layout, aaSettings);
            layout.AddressablesEditorSettings = GetAddressableEditorSettings(aaSettings);

            if (previousContentState != null)
                layout.BuildType = BuildType.UpdateBuild;
            else
                layout.BuildType = BuildType.NewBuild;

            string destinationPath = TimeStampedReportPath(layout.BuildStart);
            layout.WriteToFile(destinationPath, k_PrettyPrint);
        }
    }
}
