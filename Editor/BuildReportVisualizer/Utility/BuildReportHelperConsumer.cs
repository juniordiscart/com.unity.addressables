#if UNITY_2022_2_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal abstract class BuildReportHelperAsset
    {
        public abstract BuildLayout.ExplicitAsset ImmediateReferencingAsset { get; set; }
        public abstract SortedDictionary<string, BuildLayout.ExplicitAsset> GUIDToReferencingAssets { get; set; }
    }

    internal class BuildReportHelperExplicitAsset : BuildReportHelperAsset
    {
        public override BuildLayout.ExplicitAsset ImmediateReferencingAsset { get; set; }
        public override SortedDictionary<string, BuildLayout.ExplicitAsset> GUIDToReferencingAssets { get; set; }

        public BuildLayout.ExplicitAsset Asset;
        public SortedDictionary<string, BuildReportHelperExplicitAssetDependency> GUIDToInternalReferencedExplicitAssets;
        public SortedDictionary<string, BuildReportHelperImplicitAssetDependency> GUIDToInternalReferencedOtherAssets;
        public SortedDictionary<string, BuildReportHelperAssetDependency> GUIDToExternallyReferencedAssets;

        public BuildReportHelperExplicitAsset(BuildLayout.ExplicitAsset asset, BuildLayout.ExplicitAsset referencingAsset, SortedDictionary<string, BuildReportHelperDuplicateImplicitAsset> duplicateAssets)
        {
            Asset = asset;
            ImmediateReferencingAsset = referencingAsset;

            GUIDToInternalReferencedExplicitAssets = new SortedDictionary<string, BuildReportHelperExplicitAssetDependency>();
            GUIDToInternalReferencedOtherAssets = new SortedDictionary<string, BuildReportHelperImplicitAssetDependency>();
            GUIDToExternallyReferencedAssets = new SortedDictionary<string, BuildReportHelperAssetDependency>();

            GenerateFlatListOfReferencedAssets(Asset, Asset, GUIDToInternalReferencedExplicitAssets, GUIDToInternalReferencedOtherAssets, GUIDToExternallyReferencedAssets, duplicateAssets);
            GUIDToReferencingAssets = BuildReportUtility.GetReferencingAssets(Asset);
        }

        void GenerateFlatListOfReferencedAssets(BuildLayout.ExplicitAsset asset, BuildLayout.ExplicitAsset mainAsset,
            SortedDictionary<string, BuildReportHelperExplicitAssetDependency> internalReferencedExplicitAssets,
            SortedDictionary<string, BuildReportHelperImplicitAssetDependency> internalReferencedOtherAssets,
            SortedDictionary<string, BuildReportHelperAssetDependency> externallyReferencedAssets,
            SortedDictionary<string, BuildReportHelperDuplicateImplicitAsset> duplicateAssets)
        {
            foreach (BuildLayout.ExplicitAsset explicitDep in asset.InternalReferencedExplicitAssets)
            {
                if (asset.Bundle == mainAsset.Bundle && !internalReferencedExplicitAssets.ContainsKey(explicitDep.Guid))
                    internalReferencedExplicitAssets.TryAdd(explicitDep.Guid, new BuildReportHelperExplicitAssetDependency(explicitDep, asset));
                else if (asset.Bundle != mainAsset.Bundle && !externallyReferencedAssets.ContainsKey(explicitDep.Guid))
                    externallyReferencedAssets.TryAdd(explicitDep.Guid, new BuildReportHelperExplicitAssetDependency(explicitDep, asset));
                GenerateFlatListOfReferencedAssets(explicitDep, mainAsset, internalReferencedExplicitAssets, internalReferencedOtherAssets, externallyReferencedAssets, duplicateAssets);
            }

            foreach (BuildLayout.DataFromOtherAsset implicitDep in asset.InternalReferencedOtherAssets)
            {
                if (asset.Bundle == mainAsset.Bundle && !internalReferencedOtherAssets.ContainsKey(implicitDep.AssetGuid))
                {
                    if (duplicateAssets.ContainsKey(implicitDep.AssetGuid))
                        internalReferencedOtherAssets.TryAdd(implicitDep.AssetGuid, new BuildReportHelperImplicitAssetDependency(duplicateAssets[implicitDep.AssetGuid], asset));
                    else
                        internalReferencedOtherAssets.TryAdd(implicitDep.AssetGuid, new BuildReportHelperImplicitAssetDependency(implicitDep, asset));
                }
                else if (asset.Bundle != mainAsset.Bundle && !externallyReferencedAssets.ContainsKey(implicitDep.AssetGuid))
                {
                    if (duplicateAssets.ContainsKey(implicitDep.AssetGuid))
                        externallyReferencedAssets.TryAdd(implicitDep.AssetGuid, new BuildReportHelperImplicitAssetDependency(duplicateAssets[implicitDep.AssetGuid], asset));
                    else
                        externallyReferencedAssets.TryAdd(implicitDep.AssetGuid, new BuildReportHelperImplicitAssetDependency(implicitDep, asset));
                }
            }

            foreach (BuildLayout.ExplicitAsset explicitDep in asset.ExternallyReferencedAssets)
            {
                if (!externallyReferencedAssets.ContainsKey(explicitDep.Guid))
                    externallyReferencedAssets.TryAdd(explicitDep.Guid, new BuildReportHelperExplicitAssetDependency(explicitDep, asset));
                GenerateFlatListOfReferencedAssets(explicitDep, mainAsset, internalReferencedExplicitAssets, internalReferencedOtherAssets, externallyReferencedAssets, duplicateAssets);
            }
        }
    }

    internal abstract class BuildReportHelperAssetDependency
    {
        public abstract BuildLayout.ExplicitAsset ImmediateReferencingAsset { get; set; }
        public abstract SortedDictionary<string, BuildLayout.ExplicitAsset> GUIDToReferencingAssets { get; set; }
    }

    internal class BuildReportHelperExplicitAssetDependency : BuildReportHelperAssetDependency
    {
        public BuildLayout.ExplicitAsset Asset { get; set; }
        public override BuildLayout.ExplicitAsset ImmediateReferencingAsset { get; set; }
        public override SortedDictionary<string, BuildLayout.ExplicitAsset> GUIDToReferencingAssets { get; set; }

        public BuildReportHelperExplicitAssetDependency(BuildLayout.ExplicitAsset asset, BuildLayout.ExplicitAsset referencingAsset)
        {
            Asset = asset;
            ImmediateReferencingAsset = referencingAsset;
            GUIDToReferencingAssets = BuildReportUtility.GetReferencingAssets(Asset);
        }
    }

    internal class BuildReportHelperImplicitAssetDependency : BuildReportHelperAssetDependency
    {
        public BuildLayout.DataFromOtherAsset Asset { get; set; }
        public override BuildLayout.ExplicitAsset ImmediateReferencingAsset { get; set; }
        public override SortedDictionary<string, BuildLayout.ExplicitAsset> GUIDToReferencingAssets { get; set; }

        public List<BuildLayout.Bundle> Bundles { get; set; }

        public BuildReportHelperImplicitAssetDependency(BuildLayout.DataFromOtherAsset asset, BuildLayout.ExplicitAsset immediateReferencingAsset)
        {
            Asset = asset;
            ImmediateReferencingAsset = immediateReferencingAsset;
            Bundles = new List<BuildLayout.Bundle>() { asset.File.Bundle };
            GUIDToReferencingAssets = new SortedDictionary<string, BuildLayout.ExplicitAsset>();
            foreach (BuildLayout.ExplicitAsset referencingAsset in asset.ReferencingAssets)
            {
                GUIDToReferencingAssets.TryAdd(referencingAsset.Guid, referencingAsset);
            }
        }

        public BuildReportHelperImplicitAssetDependency(BuildReportHelperDuplicateImplicitAsset duplicateAsset, BuildLayout.ExplicitAsset immediateReferencingAsset)
        {
            Asset = duplicateAsset.Asset;
            ImmediateReferencingAsset = immediateReferencingAsset;
            Bundles = duplicateAsset.Bundles;
            GUIDToReferencingAssets = duplicateAsset.GUIDToReferencingAssets;
        }
    }

    internal class BuildReportHelperDuplicateImplicitAsset : BuildReportHelperAsset
    {
        public override BuildLayout.ExplicitAsset ImmediateReferencingAsset { get; set; }

        public List<BuildLayout.Bundle> Bundles { get; set; }

        public BuildLayout.DataFromOtherAsset Asset;

        public override SortedDictionary<string, BuildLayout.ExplicitAsset> GUIDToReferencingAssets { get; set; }

        public BuildReportHelperDuplicateImplicitAsset(BuildLayout.DataFromOtherAsset asset, BuildLayout.AssetDuplicationData assetDupData)
        {
            Asset = asset;
            Bundles = new List<BuildLayout.Bundle>();
            GUIDToReferencingAssets = new SortedDictionary<string, BuildLayout.ExplicitAsset>();

            foreach (BuildLayout.File bundleFile in assetDupData.DuplicatedObjects.SelectMany(o => o.IncludedInBundleFiles))
            {
                Bundles.Add(bundleFile.Bundle);
                foreach (BuildLayout.ExplicitAsset explicitAsset in bundleFile.Assets)
                {
                    foreach (BuildLayout.DataFromOtherAsset otherAsset in explicitAsset.InternalReferencedOtherAssets)
                    {
                        if (otherAsset.AssetGuid == asset.AssetGuid)
                        {
                            GUIDToReferencingAssets.TryAdd(explicitAsset.Guid, explicitAsset);
                        }
                    }
                }
            }
        }
    }

    internal class BuildReportHelperConsumer : IBuildReportConsumer
    {
        SortedDictionary<string, BuildLayout.DataFromOtherAsset> m_GUIDToImplicitAssets;
        SortedDictionary<string, BuildReportHelperDuplicateImplicitAsset> m_GUIDToDuplicateAssets;

        internal SortedDictionary<string, BuildReportHelperDuplicateImplicitAsset> GUIDToDuplicateAssets => m_GUIDToDuplicateAssets;

        public void Consume(BuildLayout buildReport)
        {
            m_GUIDToImplicitAssets = GetGUIDToImplicitAssets(buildReport);
            m_GUIDToDuplicateAssets = GetGUIDToDuplicateAssets(buildReport, m_GUIDToImplicitAssets);
        }
        SortedDictionary<string, BuildLayout.DataFromOtherAsset> GetGUIDToImplicitAssets(BuildLayout report)
        {
            var guidToImplicitAssets = new SortedDictionary<string, BuildLayout.DataFromOtherAsset>();
            var allInstancesOfImplicitAssets = BuildLayoutHelpers.EnumerateBundles(report).SelectMany(b => b.Files).SelectMany(f => f.Assets).SelectMany(a => a.InternalReferencedOtherAssets);

            foreach (BuildLayout.DataFromOtherAsset asset in allInstancesOfImplicitAssets)
            {
                if (!guidToImplicitAssets.ContainsKey(asset.AssetGuid))
                {
                    guidToImplicitAssets.TryAdd(asset.AssetGuid, asset);
                }
            }
            return guidToImplicitAssets;
        }

        SortedDictionary<string, BuildReportHelperExplicitAsset> GetGUIDToExplicitAssets(BuildLayout report, SortedDictionary<string, BuildReportHelperDuplicateImplicitAsset> duplicateAssets)
        {
            var guidToExplicitAssets = new SortedDictionary<string, BuildReportHelperExplicitAsset>();
            foreach (BuildLayout.ExplicitAsset asset in BuildLayoutHelpers.EnumerateAssets(report))
            {
                var helperAsset = new BuildReportHelperExplicitAsset(asset, null, duplicateAssets);
                guidToExplicitAssets.TryAdd(asset.Guid, helperAsset);
            }
            return guidToExplicitAssets;
        }

        SortedDictionary<string, BuildReportHelperDuplicateImplicitAsset> GetGUIDToDuplicateAssets(BuildLayout report, SortedDictionary<string, BuildLayout.DataFromOtherAsset> guidToImplicitAssets)
        {
            var duplicateAssets = new SortedDictionary<string, BuildReportHelperDuplicateImplicitAsset>();
            foreach (BuildLayout.AssetDuplicationData dupData in report.DuplicatedAssets)
            {
                var helperDupAsset = new BuildReportHelperDuplicateImplicitAsset(guidToImplicitAssets[dupData.AssetGuid], dupData);
                duplicateAssets.TryAdd(dupData.AssetGuid, helperDupAsset);
            }
            return duplicateAssets;
        }
    }
}
#endif
