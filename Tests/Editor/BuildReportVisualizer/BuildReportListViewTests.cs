using NUnit.Framework;
using UnityEditor.AddressableAssets.BuildReportVisualizer;

namespace Tests.Editor.BuildReportVisualizer
{
    public class BuildReportListViewTests
    {
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
    }
}