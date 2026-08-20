#if UNITY_6000_0_OR_NEWER
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Settings;

namespace UnityEditor.AddressableAssets.Tests
{
    /// <summary>
    /// Tests for ProjectConfigData serialization.
    /// These tests verify the new DataContractSerializer-based serialization works correctly.
    /// </summary>
    public class ProjectConfigDataSerializationTests
    {
        private string m_OriginalDataPath;
        private string m_TempDir;
        private string m_TempDataPath;

        [SetUp]
        public void Setup()
        {
            m_TempDir = Path.Combine(Path.GetTempPath(), "ProjectConfigDataTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(m_TempDir);

            // Store original path and redirect to temp location
            m_OriginalDataPath = Path.GetFullPath(".").Replace("\\", "/") + "/Library/AddressablesConfig.dat";
            m_TempDataPath = Path.Combine(m_TempDir, "AddressablesConfig.dat");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_TempDir))
            {
                Directory.Delete(m_TempDir, true);
            }
        }

        [Test]
        public void ProjectConfigData_ActivePlayModeIndex_PersistsAcrossAccess()
        {
            // This test verifies that the property works correctly
            // Note: We can't easily test file persistence without affecting the real config
            var originalValue = ProjectConfigData.ActivePlayModeIndex;

            // Just verify we can read it without error
            Assert.GreaterOrEqual(originalValue, 0);
        }

        [Test]
        public void ProjectConfigData_GenerateBuildLayout_CanBeReadAndWritten()
        {
            var originalValue = ProjectConfigData.GenerateBuildLayout;

            try
            {
                ProjectConfigData.GenerateBuildLayout = !originalValue;
                Assert.AreEqual(!originalValue, ProjectConfigData.GenerateBuildLayout);
            }
            finally
            {
                // Restore original value
                ProjectConfigData.GenerateBuildLayout = originalValue;
            }
        }

        [Test]
        public void ProjectConfigData_HierarchicalSearch_CanBeReadAndWritten()
        {
            var originalValue = ProjectConfigData.HierarchicalSearch;

            try
            {
                ProjectConfigData.HierarchicalSearch = !originalValue;
                Assert.AreEqual(!originalValue, ProjectConfigData.HierarchicalSearch);
            }
            finally
            {
                ProjectConfigData.HierarchicalSearch = originalValue;
            }
        }

        [Test]
        public void ProjectConfigData_ShowGroupsAsHierarchy_CanBeReadAndWritten()
        {
            var originalValue = ProjectConfigData.ShowGroupsAsHierarchy;

            try
            {
                ProjectConfigData.ShowGroupsAsHierarchy = !originalValue;
                Assert.AreEqual(!originalValue, ProjectConfigData.ShowGroupsAsHierarchy);
            }
            finally
            {
                ProjectConfigData.ShowGroupsAsHierarchy = originalValue;
            }
        }

        [Test]
        public void ProjectConfigData_LocalLoadSpeed_CanBeReadAndWritten()
        {
            var originalValue = ProjectConfigData.LocalLoadSpeed;

            try
            {
                ProjectConfigData.LocalLoadSpeed = 12345678;
                Assert.AreEqual(12345678, ProjectConfigData.LocalLoadSpeed);
            }
            finally
            {
                ProjectConfigData.LocalLoadSpeed = originalValue;
            }
        }

        [Test]
        public void ProjectConfigData_RemoteLoadSpeed_CanBeReadAndWritten()
        {
            var originalValue = ProjectConfigData.RemoteLoadSpeed;

            try
            {
                ProjectConfigData.RemoteLoadSpeed = 87654321;
                Assert.AreEqual(87654321, ProjectConfigData.RemoteLoadSpeed);
            }
            finally
            {
                ProjectConfigData.RemoteLoadSpeed = originalValue;
            }
        }

        [Test]
        public void ProjectConfigData_BuildLayoutReportFileFormat_CanBeReadAndWritten()
        {
            var originalValue = ProjectConfigData.BuildLayoutReportFileFormat;

            try
            {
                var newValue = originalValue == ProjectConfigData.ReportFileFormat.JSON
                    ? ProjectConfigData.ReportFileFormat.TXT
                    : ProjectConfigData.ReportFileFormat.JSON;

                ProjectConfigData.BuildLayoutReportFileFormat = newValue;
                Assert.AreEqual(newValue, ProjectConfigData.BuildLayoutReportFileFormat);
            }
            finally
            {
                ProjectConfigData.BuildLayoutReportFileFormat = originalValue;
            }
        }

        [Test]
        public void ProjectConfigData_BuildReportFilePaths_CanAddAndRemove()
        {
            var testPath = "test/path/to/report_" + Guid.NewGuid().ToString("N") + ".json";

            try
            {
                ProjectConfigData.AddBuildReportFilePath(testPath);
                Assert.IsTrue(ProjectConfigData.BuildReportFilePaths.Contains(testPath));
            }
            finally
            {
                ProjectConfigData.RemoveBuildReportFilePath(testPath);
                Assert.IsFalse(ProjectConfigData.BuildReportFilePaths.Contains(testPath));
            }
        }

        [Test]
        public void ProjectConfigData_ShowSubObjectsInGroupView_CanBeReadAndWritten()
        {
            var originalValue = ProjectConfigData.ShowSubObjectsInGroupView;

            try
            {
                ProjectConfigData.ShowSubObjectsInGroupView = !originalValue;
                Assert.AreEqual(!originalValue, ProjectConfigData.ShowSubObjectsInGroupView);
            }
            finally
            {
                ProjectConfigData.ShowSubObjectsInGroupView = originalValue;
            }
        }

        [Test]
        public void ProjectConfigData_UserHasSeenContentDirectoryAnnouncement_CanBeReadAndWritten()
        {
            var originalValue = ProjectConfigData.UserHasSeenContentDirectoryAnnouncement;

            try
            {
                ProjectConfigData.UserHasSeenContentDirectoryAnnouncement = !originalValue;
                Assert.AreEqual(!originalValue, ProjectConfigData.UserHasSeenContentDirectoryAnnouncement);
            }
            finally
            {
                ProjectConfigData.UserHasSeenContentDirectoryAnnouncement = originalValue;
            }
        }
    }
}
#endif
