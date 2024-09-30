using System.Collections.Generic;

namespace UnityEditor.AddressableAssets.Build.Layout
{
    /// <summary>
    /// Data store for summary data about build content
    /// </summary>
    public struct AssetSummary
    {
        /// <summary>
        /// Type of Asset
        /// </summary>
        public AssetType AssetType;

        /// <summary>
        /// Number of Objects build of the defined AssetType
        /// </summary>
        public int Count;

        /// <summary>
        /// Total size of combined Objects
        /// </summary>
        public ulong SizeInBytes;

        internal void Append(AssetSummary other)
        {
            Count += other.Count;
            SizeInBytes += other.SizeInBytes;
        }
    }

    /// <summary>
    /// Data store for summary data about Bundle content
    /// </summary>
    public struct BundleSummary
    {
        /// <summary>
        /// Number of bundles built
        /// </summary>
        public int Count;

        /// <summary>
        /// Size in bytes of bundles uncompressed
        /// </summary>
        public ulong TotalUncompressedSize;

        /// <summary>
        /// Size in bytes of bundled compressed
        /// </summary>
        public ulong TotalCompressedSize;
    }

    /// <summary>
    /// Data store for Addressables build
    /// </summary>
    public class BuildLayoutSummary
    {
        /// <summary>
        /// Summary of bundles
        /// </summary>
        public BundleSummary BundleSummary = new BundleSummary();

        /// <summary>
        /// Summary for AssetTypes used
        /// </summary>
        public List<AssetSummary> AssetSummaries = new List<AssetSummary>();

        /// <summary>
        /// The total number of assets in a build, including implicit assets
        /// </summary>
        internal int TotalAssetCount = 0;

        /// <summary>
        /// The total number of explicitly added Addressable assets that were included in a build
        /// </summary>
        internal int ExplicitAssetCount = 0;

        /// <summary>
        /// The total number of implicitly added assets that were included in a build
        /// </summary>
        internal int ImplicitAssetCount = 0;

        /// <summary>
        /// Generates a summary of the content used in a BuildLayout
        /// </summary>
        /// <param name="layout">BuildLayout to get a summary for</param>
        /// <returns>Summary of the BuildLayout layout</returns>
        public static BuildLayoutSummary GetSummary(BuildLayout layout)
        {
            BuildLayoutSummary summary = new BuildLayoutSummary();
            Dictionary<AssetType, ulong> sizes = new Dictionary<AssetType, ulong>();
            foreach (var group in layout.Groups)
            {
                foreach (var bundle in group.Bundles)
                {
                    summary.BundleSummary.TotalCompressedSize += bundle.FileSize;
                    summary.BundleSummary.TotalUncompressedSize += bundle.UncompressedFileSize;
                    summary.BundleSummary.Count++;

                    foreach (var file in bundle.Files)
                    {
                        summary.TotalAssetCount += file.Assets.Count + file.OtherAssets.Count;
                        summary.ExplicitAssetCount += file.Assets.Count;
                        summary.ImplicitAssetCount += file.OtherAssets.Count;

                        foreach (var asset in file.Assets)
                            AppendObjectsToSummary(asset.MainAssetType, asset.SerializedSize + asset.StreamedSize, asset.Objects, summary.AssetSummaries);
                        foreach (var asset in file.OtherAssets)
                            AppendObjectsToSummary(asset.MainAssetType, asset.SerializedSize + asset.StreamedSize, asset.Objects, summary.AssetSummaries);
                    }
                }
            }

            return summary;
        }

        /// <summary>
        /// Generates a summary of the content used in a BuildLayout, minus the asset type data.
        /// </summary>
        /// <param name="layout"></param>
        /// <returns></returns>

        internal static BuildLayoutSummary GetSummaryWithoutAssetTypes(BuildLayout layout)
        {
            BuildLayoutSummary summary = new BuildLayoutSummary();
            foreach (var group in layout.Groups)
            {
                foreach (var bundle in group.Bundles)
                {
                    summary.BundleSummary.TotalCompressedSize += bundle.FileSize;
                    summary.BundleSummary.TotalUncompressedSize += bundle.UncompressedFileSize;
                    summary.BundleSummary.Count++;

                    foreach (var file in bundle.Files)
                    {
                        summary.TotalAssetCount += file.Assets.Count + file.OtherAssets.Count;
                        summary.ExplicitAssetCount += file.Assets.Count;
                        summary.ImplicitAssetCount += file.OtherAssets.Count;
                    }
                }
            }
            return summary;
        }

        private static void AppendObjectsToSummary(AssetType mainAssetType, ulong overallSize, List<BuildLayout.ObjectData> subObjects, List<AssetSummary> assetSummariesOut)
        {
            // for Scene Assets take the accumulation of Objects for overall Scene size
            if (mainAssetType == AssetType.Scene)
                AddObjectToSummary(AssetType.Scene, overallSize, assetSummariesOut);

            // for prefabs accumulate general objects like GameObject and Transform as Prefab
            else if (mainAssetType == AssetType.Prefab || mainAssetType == AssetType.Model)
            {
                ulong serializedSize = 0;
                ulong streamedSize = 0;
                ulong size = 0;

                foreach (var objectData in subObjects)
                {
                    if (objectData.AssetType == AssetType.Other ||
                        objectData.AssetType == AssetType.GameObject ||
                        objectData.AssetType == AssetType.Component ||
                        objectData.AssetType == AssetType.MonoBehaviour )
                    {
                        serializedSize += objectData.SerializedSize;
                        streamedSize += objectData.StreamedSize;
                    }
                    else
                    {
                        AddObjectToSummary(objectData.AssetType, objectData.SerializedSize + objectData.StreamedSize, assetSummariesOut);
                    }
                }

                size = serializedSize + streamedSize;
                if (size > 0)
                    AddObjectToSummary(AssetType.Prefab, size, assetSummariesOut);
            }
            else
            {
                foreach (BuildLayout.ObjectData objectData in subObjects)
                {
                    AddObjectToSummary(objectData.AssetType, objectData.SerializedSize + objectData.StreamedSize, assetSummariesOut);
                }
            }
        }

        private static void AddObjectToSummary(AssetType assetType, ulong size, List<AssetSummary> assetSummaries)
        {
            AssetSummary summary = new AssetSummary()
            {
                AssetType = assetType,
                Count = 1,
                SizeInBytes = size
            };

            for(int i=0; i<assetSummaries.Count; ++i)
            {
                if (assetSummaries[i].AssetType == assetType)
                {
                    summary.Count = assetSummaries[i].Count + 1;
                    summary.SizeInBytes = assetSummaries[i].SizeInBytes + size;
                    assetSummaries[i] = summary;
                    return;
                }
            }

            assetSummaries.Add(summary);
        }
    }
}
