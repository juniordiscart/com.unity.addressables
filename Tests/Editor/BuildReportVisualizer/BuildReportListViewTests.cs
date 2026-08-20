using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.AddressableAssets.BuildReportVisualizer;
using UnityEditor.AddressableAssets.Settings;

namespace Tests.Editor.BuildReportVisualizer
{
    public class BuildReportListViewTests
    {
        List<string> m_OriginalBuildReportFilePaths;

        [SetUp]
        public void Setup()
        {
            m_OriginalBuildReportFilePaths = new List<string>(ProjectConfigData.BuildReportFilePaths);
            ProjectConfigData.ClearBuildReportFilePaths();
        }

        [TearDown]
        public void TearDown()
        {
            ProjectConfigData.ClearBuildReportFilePaths();
            foreach (string path in m_OriginalBuildReportFilePaths)
                ProjectConfigData.AddBuildReportFilePath(path);
        }

        static string[] AddReportPaths(int count)
        {
            var paths = new string[count];
            for (int i = 0; i < count; i++)
            {
                paths[i] = $"Test/Reports/report_{i}_{Guid.NewGuid():N}.json";
                ProjectConfigData.AddBuildReportFilePath(paths[i]);
            }

            return paths;
        }

        static BuildReportListView CreateListViewWithReports(int count, out string[] paths)
        {
            paths = AddReportPaths(count);
            var listView = new BuildReportListView(null, null);
            listView.RefreshItemsFromProjectConfig();
            return listView;
        }

        [Test]
        [TestCase("1.19.11", false)]
        [TestCase("1.21.2", false)]
        [TestCase("1.21.3", true)]
        [TestCase("1.21.21", true)]
        [TestCase("1.22.3", true)]
        [TestCase("2.0.1", true)]
        [TestCase("2.3.16", true)]
        public void TestValidBuildLayout(string version, bool isValid)
        {
            var listView = new BuildReportListView(null, null);
            Assert.AreEqual(isValid, listView.BuildLayoutIsValid(version));
        }

        [Test]
        public void RefreshItemsFromProjectConfig_OrdersItemsNewestFirst()
        {
            BuildReportListView listView = CreateListViewWithReports(3, out string[] paths);

            Assert.AreEqual(3, listView.BuildReportItems.Count);
            Assert.AreEqual(paths[2], listView.BuildReportItems[0].FilePath);
            Assert.AreEqual(paths[1], listView.BuildReportItems[1].FilePath);
            Assert.AreEqual(paths[0], listView.BuildReportItems[2].FilePath);
        }

        [Test]
        public void RemoveReport_RemovesTheSelectedReportFromProjectConfig()
        {
            BuildReportListView listView = CreateListViewWithReports(3, out string[] paths);

            listView.RemoveReport(listView.BuildReportItems[0]);

            Assert.AreEqual(2, listView.BuildReportItems.Count);
            CollectionAssert.AreEqual(new[] {paths[1], paths[0]}, listView.BuildReportItems.Select(x => x.FilePath));
            CollectionAssert.AreEqual(new[] {paths[0], paths[1]}, ProjectConfigData.BuildReportFilePaths);
        }

        [Test]
        public void RemoveReport_RemovesTheOldestReportFromProjectConfig()
        {
            BuildReportListView listView = CreateListViewWithReports(3, out string[] paths);

            listView.RemoveReport(listView.BuildReportItems[2]);

            CollectionAssert.AreEqual(new[] {paths[2], paths[1]}, listView.BuildReportItems.Select(x => x.FilePath));
            CollectionAssert.AreEqual(new[] {paths[1], paths[2]}, ProjectConfigData.BuildReportFilePaths);
        }

        [Test]
        public void RemoveReport_RemovesEveryReportWhenCalledRepeatedly()
        {
            BuildReportListView listView = CreateListViewWithReports(3, out string[] _);

            while (listView.BuildReportItems.Count > 0)
                listView.RemoveReport(listView.BuildReportItems[0]);

            Assert.IsEmpty(listView.BuildReportItems);
            Assert.IsEmpty(ProjectConfigData.BuildReportFilePaths);
        }

        [Test]
        public void RemoveReport_MatchesFilePathsWithDifferentDirectorySeparators()
        {
            var listView = new BuildReportListView(null, null);
            ProjectConfigData.AddBuildReportFilePath("Test/Reports/report.json");
            listView.RefreshItemsFromProjectConfig();

            var itemWithBackslashes = new BuildReportListView.BuildReportListItem(0, "Test\\Reports\\report.json", null);
            listView.BuildReportItems[0] = itemWithBackslashes;

            listView.RemoveReport(itemWithBackslashes);

            Assert.IsEmpty(listView.BuildReportItems);
            Assert.IsEmpty(ProjectConfigData.BuildReportFilePaths);
        }

        [Test]
        public void RemoveReport_DoesNothingForUnknownOrNullItem()
        {
            BuildReportListView listView = CreateListViewWithReports(2, out string[] paths);

            Assert.DoesNotThrow(() => listView.RemoveReport((BuildReportListView.BuildReportListItem)null));
            Assert.DoesNotThrow(() => listView.RemoveReport(new BuildReportListView.BuildReportListItem(0, "Test/Reports/not_listed.json", null)));

            Assert.AreEqual(2, listView.BuildReportItems.Count);
            CollectionAssert.AreEqual(paths, ProjectConfigData.BuildReportFilePaths);
        }

        [Test]
        public void RemoveAllReports_ClearsItemsAndProjectConfig()
        {
            BuildReportListView listView = CreateListViewWithReports(3, out string[] _);

            listView.RemoveAllReports(null);

            Assert.IsEmpty(listView.BuildReportItems);
            Assert.IsEmpty(ProjectConfigData.BuildReportFilePaths);
        }

        [Test]
        public void OnItemSelected_DoesNotThrowForEmptyOrNullSelection()
        {
            BuildReportListView listView = CreateListViewWithReports(1, out string[] _);

            Assert.DoesNotThrow(() => listView.OnItemSelected(Enumerable.Empty<object>()));
            Assert.DoesNotThrow(() => listView.OnItemSelected(null));
        }

        [Test]
        public void OnItemSelected_DoesNotThrowForItemThatIsNoLongerListed()
        {
            BuildReportListView listView = CreateListViewWithReports(1, out string[] _);
            BuildReportListView.BuildReportListItem removedItem = listView.BuildReportItems[0];
            listView.RemoveReport(removedItem);

            Assert.DoesNotThrow(() => listView.OnItemSelected(new object[] {removedItem}));
        }

        [Test]
        public void RemoveBuildReportFilePathAtIndex_IgnoresOutOfRangeIndices()
        {
            string[] paths = AddReportPaths(1);

            Assert.DoesNotThrow(() => ProjectConfigData.RemoveBuildReportFilePathAtIndex(-1));
            Assert.DoesNotThrow(() => ProjectConfigData.RemoveBuildReportFilePathAtIndex(1));
            Assert.DoesNotThrow(() => ProjectConfigData.RemoveBuildReportFilePathAtIndex(int.MaxValue));

            CollectionAssert.AreEqual(paths, ProjectConfigData.BuildReportFilePaths);
        }
    }
}
