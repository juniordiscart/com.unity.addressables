using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.BuildReportVisualizer;

namespace Tests.Editor.BuildReportVisualizer
{
    public class DuplicatedAssetsContentViewTests
    {
        static BuildLayout.Bundle CreateBundle(string name)
        {
            return new BuildLayout.Bundle { Name = name, FileSize = 1024 };
        }

        static BuildLayout.File CreateFile(BuildLayout.Bundle bundle)
        {
            return new BuildLayout.File
            {
                Name = $"CAB-{bundle.Name}",
                Bundle = bundle
            };
        }

        static BuildLayout.ExplicitAsset CreateExplicitAsset(string guid, string path, BuildLayout.Bundle bundle, BuildLayout.File file)
        {
            var asset = new BuildLayout.ExplicitAsset
            {
                Guid = guid,
                AssetPath = path,
                AddressableName = path,
                Bundle = bundle,
                File = file,
                SerializedSize = 100,
                StreamedSize = 0
            };
            file.Assets.Add(asset);
            return asset;
        }

        static BuildLayout.DataFromOtherAsset CreateImplicitAsset(string guid, string path, BuildLayout.File file)
        {
            return new BuildLayout.DataFromOtherAsset
            {
                AssetGuid = guid,
                AssetPath = path,
                File = file,
                SerializedSize = 500,
                StreamedSize = 0,
                Objects = new List<BuildLayout.ObjectData>
                {
                    new BuildLayout.ObjectData { LocalIdentifierInFile = 1 }
                }
            };
        }

        /// <summary>
        /// Builds a scenario where one implicit asset is duplicated across two bundles:
        /// bundleA has <paramref name="refsInBundleA"/> explicit assets referencing it,
        /// bundleB has one. The old bug would report DuplicationCount as
        /// refsInBundleA + 1 (referencing asset count) instead of 2 (distinct bundle count).
        /// </summary>
        static BuildReportHelperDuplicateImplicitAsset BuildTwoBundleScenario(int refsInBundleA,
            out BuildLayout.File fileA, out BuildLayout.File fileB)
        {
            var bundleA = CreateBundle("bundleA");
            var bundleB = CreateBundle("bundleB");
            fileA = CreateFile(bundleA);
            fileB = CreateFile(bundleB);

            const string implicitGuid = "implicit-asset-guid";
            var implicitInA = CreateImplicitAsset(implicitGuid, "Assets/Data/SharedAsset.asset", fileA);
            var implicitInB = CreateImplicitAsset(implicitGuid, "Assets/Data/SharedAsset.asset", fileB);

            for (int i = 0; i < refsInBundleA; i++)
            {
                var asset = CreateExplicitAsset($"guid-{i}", $"Assets/Prefabs/Prefab{i}.prefab", bundleA, fileA);
                asset.InternalReferencedOtherAssets.Add(implicitInA);
                implicitInA.ReferencingAssets.Add(asset);
            }

            var assetInB = CreateExplicitAsset("guid-b-0", "Assets/Prefabs/PrefabB0.prefab", bundleB, fileB);
            assetInB.InternalReferencedOtherAssets.Add(implicitInB);
            implicitInB.ReferencingAssets.Add(assetInB);

            var dupData = new BuildLayout.AssetDuplicationData
            {
                AssetGuid = implicitGuid,
                DuplicatedObjects = new List<BuildLayout.ObjectDuplicationData>
                {
                    new BuildLayout.ObjectDuplicationData
                    {
                        LocalIdentifierInFile = 1,
                        IncludedInBundleFiles = new List<BuildLayout.File> { fileA, fileB }
                    }
                }
            };

            return new BuildReportHelperDuplicateImplicitAsset(implicitInA, dupData);
        }

        [Test]
        public void DuplicationCount_ReflectsDistinctBundleCount_NotReferencingAssetCount()
        {
            const int refsInBundleA = 100;
            var helper = BuildTwoBundleScenario(refsInBundleA, out _, out _);
            var viewItem = new DuplicatedAssetsViewBuildReportDuplicatedAsset(helper);

            Assert.AreEqual(2, viewItem.DuplicationCount,
                "DuplicationCount should equal the number of distinct bundles (2), " +
                $"not the number of referencing assets ({refsInBundleA + 1})");

            Assert.AreEqual((ulong)(2 - 1) * 500, viewItem.SpaceSavedIfDeduplicated,
                "SpaceSavedIfDeduplicated should be (bundleCount - 1) * assetSize");
        }

        [Test]
        public void CalculateDuplicatedSize_UsesDistinctBundleCount()
        {
            const int refsInBundleA = 100;
            var helper = BuildTwoBundleScenario(refsInBundleA, out _, out _);

            ulong expected = (ulong)(2 - 1) * 500;
            ulong actual = MainPanelSummaryTab.CalculateDuplicatedSize(new[] { helper });

            Assert.AreEqual(expected, actual,
                $"Summary duplicated size should be based on 2 distinct bundles (expected {expected}), " +
                $"not on {refsInBundleA + 1} referencing assets");
        }
    }
}
