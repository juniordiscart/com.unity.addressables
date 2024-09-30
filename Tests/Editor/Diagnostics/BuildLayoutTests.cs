using System;
using System.Collections.Generic;
using System.IO;
using AddressableAssetsIntegrationTests;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.Animations;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests.Diagnostics
{
    public class BuildLayoutTests
    {
        private BuildLayout.ExplicitAsset CreateExplicitAssetWithParentBundle(BuildLayout.Bundle parent, ulong filesize = 1)
        {
            BuildLayout.ExplicitAsset asset = new BuildLayout.ExplicitAsset();
            asset.Bundle = parent;
            asset.SerializedSize = filesize;
            return asset;
        }

        private BuildLayout.Bundle CreateBundleWithFileSize(ulong filesize, string name = "default")
        {
            var bundle = new BuildLayout.Bundle();
            bundle.Name = name;
            bundle.FileSize = (ulong) filesize;
            return bundle;
        }

        private BuildLayout.Bundle CreateBundleWithFileSize(int assetCount, string name = "default")
        {
            var bundle = new BuildLayout.Bundle();
            bundle.AssetCount = assetCount;
            bundle.Name = name;
            bundle.FileSize = (ulong) assetCount;
            return bundle;
        }


        [Test]
        public void BuildLayoutBundle_CalculateEfficiency_CorrectlyWorksInSimpleCase()
        {
            BuildLayout.Bundle parent = CreateBundleWithFileSize(5);
            BuildLayout.Bundle child = CreateBundleWithFileSize(10);

            BuildLayout.ExplicitAsset parentAsset = CreateExplicitAssetWithParentBundle(parent);
            BuildLayout.ExplicitAsset childAsset = CreateExplicitAssetWithParentBundle(child);

            child.DependentBundles.Add(parent);

            parent.UpdateBundleDependency(parentAsset, childAsset);
            parent.SerializeBundleToBundleDependency();
            BuildLayoutGenerationTask.CalculateEfficiency(parent);

            float expectedEfficiency = .10f;

            Assert.AreEqual(expectedEfficiency, parent.BundleDependencies[0].ExpandedEfficiency);
        }

        [Test]
        public void BuildLayoutBundle_CalculateEfficiency_MultipleAssetDependencyCaseStillCreatesCorrectEfficiency()
        {
            BuildLayout.Bundle parent = CreateBundleWithFileSize(5);
            BuildLayout.Bundle child = CreateBundleWithFileSize(10);

            BuildLayout.ExplicitAsset parentAsset = CreateExplicitAssetWithParentBundle(parent);
            for (int i = 0; i < 10; i++)
            {
                var asset = CreateExplicitAssetWithParentBundle(child);
                parent.UpdateBundleDependency(parentAsset, asset);
            }

            parent.SerializeBundleToBundleDependency();
            BuildLayoutGenerationTask.CalculateEfficiency(parent);

            float expectedEfficiency = 1f;
            Assert.AreEqual(expectedEfficiency, parent.BundleDependencies[0].ExpandedEfficiency);
        }

        [Test]
        [TestCase(5, 10, 1, 5)]
        [TestCase(6, 12, 2, 7)]
        [TestCase(20, 43, 7, 15)]
        [TestCase(27, 68, 25, 60)]
        public void BuildLayoutBundle_CalculateEfficiency_ThreeLayeredTreeGeneratesCorrectEfficiencyAtAllLevels(
            int parentAssetCount,
            int childAssetCount,
            int parentDependencyCount,
            int childDependencyCount)
        {
            BuildLayout.Bundle grandparent = CreateBundleWithFileSize(1);
            BuildLayout.Bundle parent = CreateBundleWithFileSize(parentAssetCount);
            BuildLayout.Bundle child = CreateBundleWithFileSize(childAssetCount);

            BuildLayout.ExplicitAsset grandparentAsset = CreateExplicitAssetWithParentBundle(grandparent);
            List<BuildLayout.ExplicitAsset> parentAssets = new List<BuildLayout.ExplicitAsset>();
            for (int i = 0; i < parentDependencyCount; i++)
            {
                var parentAsset = CreateExplicitAssetWithParentBundle(parent);
                grandparent.UpdateBundleDependency(grandparentAsset, parentAsset);
                parentAssets.Add(parentAsset);
            }
            for (int i = 0; i < childDependencyCount; i++)
            {
                var childAsset = CreateExplicitAssetWithParentBundle(child);
                parent.UpdateBundleDependency(parentAssets[0], childAsset);
            }

            grandparent.SerializeBundleToBundleDependency();
            parent.SerializeBundleToBundleDependency();

            BuildLayoutGenerationTask.CalculateEfficiency(grandparent);

            float expectedGrandparentEfficiency = (float) parentDependencyCount / parentAssetCount;
            float expectedGrandparentExpandedEfficiency = (float) (childDependencyCount + parentDependencyCount) / (childAssetCount + parentAssetCount);
            float expectedParentEfficiency = (float) childDependencyCount / childAssetCount;
            Assert.AreEqual(expectedGrandparentEfficiency, grandparent.BundleDependencies[0].Efficiency, "Grandparent Efficiency not calculated correctly - this should not be affected by the children of parent.");
            Assert.AreEqual(expectedGrandparentExpandedEfficiency, grandparent.BundleDependencies[0].ExpandedEfficiency, "Grandparent ExpandedEfficiency not calculated correctly - this should take into account the total Efficiency of both the direct children Dependency Links as well as all BundleDependencies below the child.");
            Assert.AreEqual(expectedParentEfficiency, parent.BundleDependencies[0].ExpandedEfficiency, "Parent ExpandedEfficiency not calculated correctly - this should take into account the Efficiency of the direct child only.");
        }

        [Test]
        public void BuildLayoutBundle_CalculateEfficiency_AssetDependenciesAreNotDoubleCounted()
        {
            var parentBundle = CreateBundleWithFileSize(1);
            var childBundle = CreateBundleWithFileSize(1);

            var parentAsset = CreateExplicitAssetWithParentBundle(parentBundle);
            var childAsset = CreateExplicitAssetWithParentBundle(childBundle);

            parentBundle.UpdateBundleDependency(parentAsset, childAsset);
            parentBundle.SerializeBundleToBundleDependency();
            BuildLayoutGenerationTask.CalculateEfficiency(parentBundle);

            Assert.AreEqual(1f, parentBundle.BundleDependencies[0].ExpandedEfficiency, "ExpandedEfficiency not correctly calculated");
            Assert.AreEqual(1f, parentBundle.BundleDependencies[0].Efficiency, "Efficiency not correctly calculated");
        }

        [Test]
        [TestCase(3, 3, 1, 1 )]
        [TestCase(50, 5, 5, 2)]
        [TestCase(100, 100, 1, 50)]
        public void BuildLayoutBundle_CalculateEfficiency_MultiProngedShortTreeGeneratesCorrectEfficiency(
            int child1AssetCount,
            int child2AssetCount,
            int child1DependencyCount,
            int child2DependencyCount)
        {
            BuildLayout.Bundle parentBundle = CreateBundleWithFileSize(2);
            BuildLayout.Bundle childBundle1 = CreateBundleWithFileSize(child1AssetCount);
            BuildLayout.Bundle childBundle2 = CreateBundleWithFileSize(child2AssetCount);

            var parentAsset1 = CreateExplicitAssetWithParentBundle(parentBundle);
            var parentAsset2 = CreateExplicitAssetWithParentBundle(parentBundle);

            for (int i = 0; i < child1DependencyCount; i++)
            {
                var childAsset = CreateExplicitAssetWithParentBundle(childBundle1);
                parentBundle.UpdateBundleDependency(parentAsset1, childAsset);
            }

            for (int i = 0; i < child2DependencyCount; i++)
            {
                var childAsset = CreateExplicitAssetWithParentBundle(childBundle2);
                parentBundle.UpdateBundleDependency(parentAsset2, childAsset);
            }

            parentBundle.SerializeBundleToBundleDependency();
            BuildLayoutGenerationTask.CalculateEfficiency(parentBundle);

            float expectedChild1Efficiency = (float) child1DependencyCount / child1AssetCount;
            float expectedChild2Efficiency = (float) child2DependencyCount / child2AssetCount;
            Assert.AreEqual(expectedChild1Efficiency, parentBundle.BundleDependencies[0].ExpandedEfficiency);
            Assert.AreEqual(expectedChild2Efficiency, parentBundle.BundleDependencies[1].ExpandedEfficiency);
        }

        [Test]
        public void BuildLayoutBundle_CalculateEfficiency_DoesNotDoubleCountDependencies()
        {
            BuildLayout.Bundle parent = CreateBundleWithFileSize(2);
            BuildLayout.Bundle child = CreateBundleWithFileSize(2);

            BuildLayout.ExplicitAsset parentAsset1 = CreateExplicitAssetWithParentBundle(parent);
            BuildLayout.ExplicitAsset parentAsset2 = CreateExplicitAssetWithParentBundle(parent);
            BuildLayout.ExplicitAsset childAsset1 = CreateExplicitAssetWithParentBundle(child);

            parent.UpdateBundleDependency(parentAsset1, childAsset1);
            parent.UpdateBundleDependency(parentAsset2, childAsset1);

            parent.SerializeBundleToBundleDependency();
            BuildLayoutGenerationTask.CalculateEfficiency(parent);

            float expectedEfficiency = .5f;
            Assert.AreEqual(expectedEfficiency, parent.BundleDependencies[0].ExpandedEfficiency, "If multiple assets in a parent bundle depend on a single asset in the child, this reliance should not be double counted.");
            Assert.AreEqual(expectedEfficiency, parent.BundleDependencies[0].Efficiency, "If multiple assets in a parent bundle depend on a single asset in the child, this reliance should not be double counted.");
        }

        [Test]
        [TestCase(2, 1, 1, 1, 1)]
        [TestCase(2, 5, 5, 2, 3)]
        [TestCase(10, 3,3, 3,3)]
        [TestCase(30, 30, 30, 20, 20)]
        [TestCase(50, 50, 50, 50, 50)]
        public void BuildLayoutBundle_CalculateEfficiency_HigherLevelTreesCorrectlyPromulgateLowerLevelEfficiency(
            int parentAssetCount,
            int child1AssetCount,
            int child2AssetCount,
            int child1DependencyCount,
            int child2DependencyCount)
        {
            BuildLayout.Bundle grandparent = CreateBundleWithFileSize(1);
            BuildLayout.Bundle parent = CreateBundleWithFileSize(parentAssetCount);
            BuildLayout.Bundle childBundle1 = CreateBundleWithFileSize(child1AssetCount);
            BuildLayout.Bundle childBundle2 = CreateBundleWithFileSize(child2AssetCount);

            BuildLayout.ExplicitAsset grandparentAsset = CreateExplicitAssetWithParentBundle(grandparent);
            BuildLayout.ExplicitAsset parentAsset = CreateExplicitAssetWithParentBundle(parent);

            grandparent.UpdateBundleDependency(grandparentAsset, parentAsset);

            for (int i = 0; i < child1DependencyCount; i++)
            {
                var childAsset = CreateExplicitAssetWithParentBundle(childBundle1);
                parent.UpdateBundleDependency(parentAsset, childAsset);
            }

            for (int i = 0; i < child2DependencyCount; i++)
            {
                var childAsset = CreateExplicitAssetWithParentBundle(childBundle2);
                parent.UpdateBundleDependency(parentAsset, childAsset);
            }

            grandparent.SerializeBundleToBundleDependency();
            parent.SerializeBundleToBundleDependency();

            BuildLayoutGenerationTask.CalculateEfficiency(grandparent);


            float expectedGrandparentToParentEfficiency = (float) 1 / parentAssetCount;
            float expectedGrandparentToParentExpandedEfficiency = (float) (1 + child1DependencyCount + child2DependencyCount) / (parentAssetCount + child1AssetCount + child2AssetCount);
            float expectedParentToChild1Efficiency = (float) child1DependencyCount / child1AssetCount;
            float expectedParentToChild2Efficiency = (float) child2DependencyCount / child2AssetCount;

            Assert.AreEqual(expectedGrandparentToParentEfficiency, grandparent.BundleDependencies[0].Efficiency);
            Assert.AreEqual(expectedGrandparentToParentExpandedEfficiency, grandparent.BundleDependencies[0].ExpandedEfficiency);
            Assert.AreEqual(expectedParentToChild1Efficiency, parent.BundleDependencies[0].Efficiency);
            Assert.AreEqual(expectedParentToChild2Efficiency, parent.BundleDependencies[1].Efficiency);

        }

        [Test]
        [TestCase(5, 10, 10, 3, 5)]
        [TestCase(7, 20, 15, 5, 8)]
        [TestCase(11, 35, 40, 15, 20)]
        public void BuildLayoutBundle_CalculateEfficiency_CacheCorrectlyAvoidsDoubleComputation(
            int parentAssetCount,
            int child1AssetCount,
            int child2AssetCount,
            int child1DependencyCount,
            int child2DependencyCount)
        {
            BuildLayout.Bundle grandparent1 = CreateBundleWithFileSize(1);
            BuildLayout.Bundle grandparent2 = CreateBundleWithFileSize(1);
            BuildLayout.Bundle parent = CreateBundleWithFileSize(parentAssetCount);
            BuildLayout.Bundle child1 = CreateBundleWithFileSize(child1AssetCount);
            BuildLayout.Bundle child2 = CreateBundleWithFileSize(child2AssetCount);

            BuildLayout.ExplicitAsset grandparent1Asset = CreateExplicitAssetWithParentBundle(grandparent1);
            BuildLayout.ExplicitAsset grandparent2Asset = CreateExplicitAssetWithParentBundle(grandparent2);
            BuildLayout.ExplicitAsset parentAsset = CreateExplicitAssetWithParentBundle(parent);

            grandparent1.UpdateBundleDependency(grandparent1Asset, parentAsset);
            grandparent2.UpdateBundleDependency(grandparent2Asset, parentAsset);

            for (int i = 0; i < child1DependencyCount; i++)
            {
                var childAsset = CreateExplicitAssetWithParentBundle(child1);
                parent.UpdateBundleDependency(parentAsset, childAsset);
            }

            for (int i = 0; i < child2DependencyCount; i++)
            {
                var childAsset = CreateExplicitAssetWithParentBundle(child2);
                parent.UpdateBundleDependency(parentAsset, childAsset);
            }

            Dictionary<BuildLayout.Bundle.BundleDependency, BuildLayout.Bundle.EfficiencyInfo> bdCache = new Dictionary<BuildLayout.Bundle.BundleDependency, BuildLayout.Bundle.EfficiencyInfo>();

            grandparent1.SerializeBundleToBundleDependency();
            grandparent2.SerializeBundleToBundleDependency();
            parent.SerializeBundleToBundleDependency();

            BuildLayoutGenerationTask.CalculateEfficiency(grandparent1, bdCache);

            var ef1 = new BuildLayout.Bundle.EfficiencyInfo
            {
                totalAssetFileSize = (ulong) child1AssetCount,
                referencedAssetFileSize = (ulong) child1DependencyCount
            };

            var ef2 = new BuildLayout.Bundle.EfficiencyInfo
            {
                totalAssetFileSize = (ulong) child2AssetCount,
                referencedAssetFileSize = (ulong) child2DependencyCount
            };


            float bd1Efficiency = (float) child1DependencyCount / child1AssetCount;
            float bd2Efficiency = (float) child2DependencyCount / child2AssetCount;

            float gp1Efficiency = (float) (1 + child1DependencyCount + child2DependencyCount) / (parentAssetCount + child1AssetCount + child2AssetCount);

            Assert.IsTrue(bdCache.ContainsKey(parent.BundleDependencies[0]), "BundleDependencyCache not properly populated");
            Assert.AreEqual(bdCache[parent.BundleDependencies[0]], ef1);
            Assert.AreEqual(parent.BundleDependencies[0].ExpandedEfficiency, bd1Efficiency);

            Assert.IsTrue(bdCache.ContainsKey(parent.BundleDependencies[1]), "BundleDependencyCache not properly populated");
            Assert.AreEqual(bdCache[parent.BundleDependencies[1]], ef2);
            Assert.AreEqual(parent.BundleDependencies[1].ExpandedEfficiency, bd2Efficiency);

            Assert.AreEqual(grandparent1.BundleDependencies[0].ExpandedEfficiency, gp1Efficiency);

            // Create a new asset that is not cached - we do this to ensure that cached values are used and that we're not recalculating Efficiency
            // This will never happen in normal execution - this is just a workaround to make sure the cache is working properly.
            var uncachedAsset = CreateExplicitAssetWithParentBundle(child1);
            parent.UpdateBundleDependency(parentAsset, uncachedAsset);

            BuildLayoutGenerationTask.CalculateEfficiency(grandparent2, bdCache);

            Assert.IsTrue(bdCache.ContainsKey(parent.BundleDependencies[0]), "Key should not be removed");
            Assert.AreEqual(bdCache[parent.BundleDependencies[0]], ef1, "Efficiency should not change since cached value should be used");
            Assert.AreEqual(parent.BundleDependencies[0].ExpandedEfficiency, bd1Efficiency, "Efficiency should not change since cached value should be used.");
            Assert.AreEqual(grandparent2.BundleDependencies[0].ExpandedEfficiency, gp1Efficiency, "Efficiency of grandparent2 should be equal to grandparent1, since cache was used.");

            //Calculation without cache should be incorrect
            BuildLayoutGenerationTask.CalculateEfficiency(grandparent2);
            Assert.AreNotEqual(parent.BundleDependencies[0].ExpandedEfficiency, bd1Efficiency, "Efficiency should change since cached value is not used");
            Assert.AreNotEqual(grandparent2.BundleDependencies[0].ExpandedEfficiency, gp1Efficiency, "Efficiency of grandparent2 should not be equal to grandparent1 since cache isnt used");
        }

        [Test]
        [TestCase(5, 5, 5, 3, 2, 3, 4)]
        [TestCase(50, 2, 10, 10, 1, 2, 4)]
        [TestCase(1, 1, 1, 1, 1, 1, 1)]
        [TestCase(100, 100, 100, 20, 30, 40, 40)]
        public void BuildLayoutBundle_CalculateEfficiency_NonTreeStructureProperlyHandled(
            int bundleBAssetCount,
            int bundleCAssetCount,
            int bundleDAssetCount,
            int bundleBDependencyCount,
            int bundleBToBundleCDependencyCount,
            int bundleCDependencyCount,
            int bundleDDependencyCount)
        {
            BuildLayout.Bundle bundleA = CreateBundleWithFileSize(1);
            BuildLayout.Bundle bundleB = CreateBundleWithFileSize(bundleBAssetCount);
            BuildLayout.Bundle bundleC = CreateBundleWithFileSize(bundleCAssetCount);
            BuildLayout.Bundle bundleD = CreateBundleWithFileSize(bundleDAssetCount);

            var assetA = CreateExplicitAssetWithParentBundle(bundleA);
            var assetB = CreateExplicitAssetWithParentBundle(bundleB);
            var assetC = CreateExplicitAssetWithParentBundle(bundleC);

            bundleA.UpdateBundleDependency(assetA, assetB);

            for (int i = 0; i < bundleBDependencyCount - 1; i++)
            {
                var childAsset = CreateExplicitAssetWithParentBundle(bundleB);
                bundleA.UpdateBundleDependency(assetA, childAsset);
            }

            bundleA.UpdateBundleDependency(assetA, assetC);

            for (int i = 0; (i < bundleCDependencyCount - 1) || (i < bundleBToBundleCDependencyCount); i++)
            {
                var childAsset = CreateExplicitAssetWithParentBundle(bundleC);
                if (i < bundleCDependencyCount - 1)
                    bundleA.UpdateBundleDependency(assetA, childAsset);
                if (i < bundleBToBundleCDependencyCount)
                    bundleB.UpdateBundleDependency(assetB, childAsset);
            }


            for (int i = 0; i < bundleDDependencyCount; i++)
            {
                var childAsset = CreateExplicitAssetWithParentBundle(bundleD);
                bundleC.UpdateBundleDependency(assetC, childAsset);
            }

            bundleA.SerializeBundleToBundleDependency();
            bundleB.SerializeBundleToBundleDependency();
            bundleC.SerializeBundleToBundleDependency();
            bundleD.SerializeBundleToBundleDependency();

            BuildLayoutGenerationTask.CalculateEfficiency(bundleA);

            var cToDExpandedEfficiency = (float)bundleDDependencyCount / bundleDAssetCount;
            var BToCExpandedEfficiency = (float)(bundleDDependencyCount + bundleBToBundleCDependencyCount) / (bundleCAssetCount + bundleDAssetCount);
            var AToBExpandedEfficiency = (float) (bundleBDependencyCount + bundleDDependencyCount + bundleBToBundleCDependencyCount) / (bundleBAssetCount + bundleCAssetCount + bundleDAssetCount);

            Assert.AreEqual(bundleC.BundleDependencies[0].ExpandedEfficiency, cToDExpandedEfficiency);
            Assert.AreEqual(bundleB.BundleDependencies[0].ExpandedEfficiency, BToCExpandedEfficiency);
            Assert.AreEqual(bundleA.BundleDependencies[0].ExpandedEfficiency, AToBExpandedEfficiency);
        }

        [Test]
        [TestCase(20, 20, 20, 5, 5, 5, 5)]
        [TestCase(50, 30, 10, 15, 10, 8, 7)]
        [TestCase(100, 75, 35, 35, 7, 30, 25)]
        public void BuildLayoutBundle_CalculateEfficiency_HandlesCycleWithLeadInByCuttingOffLastNodeConnections(
            int bundleBAssetCount,
            int bundleCAssetCount,
            int bundleDAssetCount,
            int bundleAToBDependencyCount,
            int bundleBToCDependencyCount,
            int bundleCToDDependencyCount,
            int bundleDToBDependencyCount)
        {
            BuildLayout.Bundle bundleA = CreateBundleWithFileSize(1);
            BuildLayout.Bundle bundleB = CreateBundleWithFileSize(bundleBAssetCount, "B");
            BuildLayout.Bundle bundleC = CreateBundleWithFileSize(bundleCAssetCount, "C");
            BuildLayout.Bundle bundleD = CreateBundleWithFileSize(bundleDAssetCount, "D");

            var assetA = CreateExplicitAssetWithParentBundle(bundleA);
            var assetB = CreateExplicitAssetWithParentBundle(bundleB);
            var assetC = CreateExplicitAssetWithParentBundle(bundleC);
            var assetD = CreateExplicitAssetWithParentBundle(bundleD);

            bundleA.UpdateBundleDependency(assetA, assetB);
            bundleB.UpdateBundleDependency(assetB, assetC);
            bundleC.UpdateBundleDependency(assetC, assetD);
            bundleD.UpdateBundleDependency(assetD, assetB);

            for (int i = 0; i < bundleAToBDependencyCount - 1; i++)
            {
                var childAsset = CreateExplicitAssetWithParentBundle(bundleB);
                bundleA.UpdateBundleDependency(assetA, childAsset);
            }

            for (int i = 0; i < bundleBToCDependencyCount - 1; i++)
            {
                var childAsset = CreateExplicitAssetWithParentBundle(bundleC);
                bundleB.UpdateBundleDependency(assetB, childAsset);
            }

            for (int i = 0; i < bundleCToDDependencyCount - 1; i++)
            {
                var childAsset = CreateExplicitAssetWithParentBundle(bundleD);
                bundleC.UpdateBundleDependency(assetC, childAsset);
            }

            for (int i = 0; i < bundleDToBDependencyCount - 1; i++)
            {
                var childAsset = CreateExplicitAssetWithParentBundle(bundleB);
                bundleD.UpdateBundleDependency(assetD, childAsset);
            }

            bundleA.SerializeBundleToBundleDependency();
            bundleB.SerializeBundleToBundleDependency();
            bundleC.SerializeBundleToBundleDependency();
            bundleD.SerializeBundleToBundleDependency();

            BuildLayoutGenerationTask.CalculateEfficiency(bundleA);

            float bundleDToBExpandedEfficiency = (float) bundleDToBDependencyCount / bundleBAssetCount;
            float bundleCToDExpandedEfficiency = (float) (bundleCToDDependencyCount + bundleDToBDependencyCount) / (bundleDAssetCount + bundleBAssetCount);
            float bundleBToCExpandedEfficiency = (float) (bundleBToCDependencyCount + bundleCToDDependencyCount + bundleDToBDependencyCount) / (bundleCAssetCount + bundleDAssetCount + bundleBAssetCount);
            float bundleAToBExpandedEfficiency = (float) (bundleAToBDependencyCount + bundleBToCDependencyCount + bundleCToDDependencyCount + bundleDToBDependencyCount) / (bundleBAssetCount + bundleCAssetCount + bundleDAssetCount + bundleBAssetCount);

            Assert.AreEqual(bundleAToBExpandedEfficiency, bundleA.BundleDependencies[0].ExpandedEfficiency);
            Assert.AreEqual(bundleBToCExpandedEfficiency, bundleB.BundleDependencies[0].ExpandedEfficiency);
            Assert.AreEqual(bundleCToDExpandedEfficiency, bundleC.BundleDependencies[0].ExpandedEfficiency);
            Assert.AreEqual(bundleDToBExpandedEfficiency, bundleD.BundleDependencies[0].ExpandedEfficiency);
        }

        BuildLayout CreateTestLayout(DateTime buildDate, string error = null)
        {
            BuildLayout layout = new BuildLayout();

            layout.BuildTarget = BuildTarget.Android;
            layout.BuildResultHash = "testHash123";
            layout.BuildType = BuildType.UpdateBuild;
            layout.BuildStart = buildDate;
            layout.Duration = 10;
            layout.BuildError = error;

            layout.BuildScript = "DefaultBuildScript";

            BuildLayout.Bundle bundle = CreateBundleWithFileSize(2);
            bundle.Files = new List<BuildLayout.File>();
            BuildLayout.File file = new BuildLayout.File();
            bundle.Files.Add(file);
            file.Assets = new List<BuildLayout.ExplicitAsset>();
            file.Assets.Add(CreateExplicitAssetWithParentBundle(bundle));
            file.Assets.Add(CreateExplicitAssetWithParentBundle(bundle));

            BuildLayout.Group group = new BuildLayout.Group();
            group.Bundles = new List<BuildLayout.Bundle>();
            group.Bundles.Add(bundle);
            layout.Groups = new List<BuildLayout.Group>();
            layout.Groups.Add(group);
            layout.DefaultGroup = group;

            return layout;
        }

        [Test]
        public void BuildLayoutBundle_SaveAndLoad_SavesCorrectlyToDisk()
        {
            DateTime time = DateTime.Now;
            BuildLayout layout = CreateTestLayout(time);
            string filePath = $"{Application.dataPath}/testLayout.json";

            try
            {
                layout.WriteToFile(filePath, false);
                FileAssert.Exists(filePath, $"Failed to save build layout to {filePath}");
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [Test]
        [TestCase(50UL, 50UL, 20UL, 20UL, 10, 5)]
        [TestCase(30UL, 50UL, 10UL, 5UL, 3, 2)]
        [TestCase(1UL, 1UL, 1UL, 1UL, 1, 1)]
        [TestCase(30UL, 1UL, 30UL, 1UL, 1, 1)]
        [TestCase(5000UL, 5000UL, 500UL, 500UL, 30, 50)]
        public void BuildLayoutBundle_CalculateEfficiency_CorrectlyUsesFilesizeForEfficiency(
            ulong parentTotalFilesize, ulong childTotalFilesize,
            ulong parentDepFileSize, ulong childDepFilesize,
            int parentDependencyCount, int childDependencyCount)
        {
            BuildLayout.Bundle grandparent = CreateBundleWithFileSize(1);
            BuildLayout.Bundle parent = CreateBundleWithFileSize(parentTotalFilesize);
            BuildLayout.Bundle child = CreateBundleWithFileSize(childTotalFilesize);

            BuildLayout.ExplicitAsset grandparentAsset = CreateExplicitAssetWithParentBundle(grandparent);
            List<BuildLayout.ExplicitAsset> parentAssets = new List<BuildLayout.ExplicitAsset>();
            ulong expectedParentDepFileSize = 0;
            for (int i = 0; i < parentDependencyCount; i++)
            {
                var curFileSize = (ulong)parentDependencyCount / parentDepFileSize;
                expectedParentDepFileSize += curFileSize;
                var parentAsset = CreateExplicitAssetWithParentBundle(parent, curFileSize);
                grandparent.UpdateBundleDependency(grandparentAsset, parentAsset);
                parentAssets.Add(parentAsset);
            }

            ulong expectedChildDepFileSize = 0;
            for (int i = 0; i < childDependencyCount; i++)
            {
                var curFileSize = (ulong)childDependencyCount / childDepFilesize;
                expectedChildDepFileSize += curFileSize;
                var childAsset = CreateExplicitAssetWithParentBundle(child, curFileSize);
                parent.UpdateBundleDependency(parentAssets[0], childAsset);
            }

            grandparent.SerializeBundleToBundleDependency();
            parent.SerializeBundleToBundleDependency();

            BuildLayoutGenerationTask.CalculateEfficiency(grandparent);

            float expectedGrandparentEfficiency = (float) expectedParentDepFileSize / parentTotalFilesize;
            float expectedGrandparentExpandedEfficiency = (float) (expectedParentDepFileSize + expectedChildDepFileSize) / (parentTotalFilesize + childTotalFilesize);
            float expectedParentEfficiency = (float) expectedChildDepFileSize / childTotalFilesize;
            Assert.AreEqual(expectedGrandparentEfficiency, grandparent.BundleDependencies[0].Efficiency, "Grandparent Efficiency not calculated correctly - this should not be affected by the children of parent.");
            Assert.AreEqual(expectedGrandparentExpandedEfficiency, grandparent.BundleDependencies[0].ExpandedEfficiency, "Grandparent ExpandedEfficiency not calculated correctly - this should take into account the total Efficiency of both the direct children Dependency Links as well as all BundleDependencies below the child.");
            Assert.AreEqual(expectedParentEfficiency, parent.BundleDependencies[0].ExpandedEfficiency, "Parent ExpandedEfficiency not calculated correctly - this should take into account the Efficiency of the direct child only.");
        }

        [Test]
        public void BuildLayoutBundle_SaveAndLoad_ReadsCorrectDataForHeader()
        {
            DateTime time = DateTime.Now;
            BuildLayout layout = CreateTestLayout(time, "testError123");
            time = layout.BuildStart; // due to removing miliseconds in the layout
            string filePath = $"{Application.dataPath}/testLayout.json";

            try
            {
                layout.WriteToFile(filePath, false);
                FileAssert.Exists(filePath, $"Failed to save build layout to {filePath}");
                BuildLayout readLayout = BuildLayout.Open(filePath, false, false);
                Assert.AreEqual(DateTime.MinValue, readLayout.BuildStart, "StartDate was not expected to have read a valid date");

                // test all header values to ensure the values are as expect (not the same as default values)
                readLayout.ReadHeader();
                Assert.AreEqual(time, readLayout.Header.BuildStart, "Header Data written or Read incorrectly");
                Assert.AreEqual(10, readLayout.Header.Duration, "Header Data written or Read incorrectly");
                Assert.AreEqual("testError123", readLayout.Header.BuildError, "Header Data written or Read incorrectly");
                Assert.AreEqual(BuildTarget.Android, readLayout.Header.BuildTarget, "Header Data written or Read incorrectly");
                Assert.AreEqual(BuildType.UpdateBuild, readLayout.Header.BuildType, "Header Data written or Read incorrectly");
                Assert.AreEqual("testHash123", readLayout.Header.BuildResultHash, "Header Data written or Read incorrectly");

                // that the test groups have not been read
                Assert.AreEqual(0, readLayout.Groups.Count, "Expect to have not read any Groups yet with only Header read");
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [Test]
        public void BuildLayoutBundle_SaveAndLoad_ReadsCorrectDataForBody()
        {
            DateTime time = DateTime.Now;
            BuildLayout layout = CreateTestLayout(time);
            string filePath = $"{Application.dataPath}/testLayout.json";

            try
            {
                layout.WriteToFile(filePath, false);
                FileAssert.Exists(filePath, $"Failed to save build layout to {filePath}");
                BuildLayout readLayout = BuildLayout.Open(filePath, false, false);

                Assert.AreEqual(0, readLayout.Groups.Count, "Expect to have not read any Groups yet with no data yet read read");
                readLayout.ReadHeader();
                Assert.AreEqual(0, readLayout.Groups.Count, "Expect to have not read any Groups yet with only Header read");
                readLayout.ReadFull();
                Assert.AreEqual(1, readLayout.Groups.Count, "Expect to have 1 Groups after doing a full read read");
            }
            finally
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [Test]
        [TestCase(typeof(GameObject), AssetType.GameObject)]
        [TestCase(typeof(SceneAsset), AssetType.Scene)]
        [TestCase(typeof(AnimatorController), AssetType.AnimationController)]
        [TestCase(typeof(TestObject), AssetType.ScriptableObject)]
        [TestCase(typeof(ObjectReferenceMonoBehaviour), AssetType.MonoBehaviour)]
        public void GetAssetType_ReturnsCorrect(System.Type typeIn, AssetType expectedType)
        {
            AssetType actual = BuildLayoutHelpers.GetAssetType(typeIn);
            Assert.AreEqual(expectedType, actual);
        }
    }
}
