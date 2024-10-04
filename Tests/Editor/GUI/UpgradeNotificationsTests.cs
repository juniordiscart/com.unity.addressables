using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor.GUI
{
    public class UpgradeNotificationsTests
    {
        private const string k_DefaultPath = "DefaultPath";
        private AddressableAssetSettings m_Settings;

        [SetUp]
        public void SetUp()
        {
            m_Settings = ScriptableObject.CreateInstance<AddressableAssetSettings>();
            m_Settings.profileSettings.CreateDefaultProfile();
            // we have to delete all the path variables so our tests can create them as necessary
            m_Settings.profileSettings.RemoveValue(m_Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kLocalBuildPath).Id);
            m_Settings.profileSettings.RemoveValue(m_Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kLocalLoadPath).Id);
            m_Settings.profileSettings.RemoveValue(m_Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kRemoteBuildPath).Id);
            m_Settings.profileSettings.RemoveValue(m_Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kRemoteLoadPath).Id);
        }
        [Test]
        public void UpgradeNotificationsTests_NeedsPathPairMigration_ReturnsTrue_WhenOldEntryExistsAndNewEntryDoesNotExist()
        {
            // Arrange
            m_Settings.profileSettings.CreateValue("LocalBuildPath", k_DefaultPath);
            m_Settings.profileSettings.CreateValue("LocalLoadPath", k_DefaultPath);
            m_Settings.profileSettings.CreateValue("RemoteBuildPath", k_DefaultPath);
            m_Settings.profileSettings.CreateValue("RemoteLoadPath", k_DefaultPath);
            var upgradeNotifications = new UpgradeNotifications();

            // Act
            var needsPathPairMigration = upgradeNotifications.NeedsPathPairMigration(m_Settings);

            // Assert
            Assert.IsTrue(needsPathPairMigration);
        }

        [Test]
        public void UpgradeNotificationsTests_NeedsPathPairMigration_ReturnsFalse_WhenOldEntryDoesNotExist()
        {
            // Arrange
            var upgradeNotifications = new UpgradeNotifications();

            // Act
            var needsPathPairMigration = upgradeNotifications.NeedsPathPairMigration(m_Settings);

            // Assert
            Assert.IsFalse(needsPathPairMigration);
        }

        [Test]
        public void UpgradeNotificationsTests_NeedsPathPairMigration_ReturnsFalse_WhenNewEntryExists()
        {
            // Arrange

            m_Settings.profileSettings.CreateDefaultProfile();
            m_Settings.profileSettings.CreateValue(AddressableAssetSettings.kLocalBuildPath, k_DefaultPath);
            m_Settings.profileSettings.CreateValue(AddressableAssetSettings.kLocalLoadPath, k_DefaultPath);
            m_Settings.profileSettings.CreateValue(AddressableAssetSettings.kRemoteBuildPath, k_DefaultPath);
            m_Settings.profileSettings.CreateValue(AddressableAssetSettings.kRemoteLoadPath, k_DefaultPath);
            var upgradeNotifications = new UpgradeNotifications();

            // Act
            var needsPathPairMigration = upgradeNotifications.NeedsPathPairMigration(m_Settings);

            // Assert
            Assert.IsFalse(needsPathPairMigration);
        }

        [Test]
        public void UpgradeNotificationsTests_DoPathPairMigration_RenamesOldEntryToNewEntry()
        {
            // Arrange
            m_Settings.profileSettings.CreateValue("LocalBuildPath", k_DefaultPath);
            m_Settings.profileSettings.CreateValue("LocalLoadPath", k_DefaultPath);
            m_Settings.profileSettings.CreateValue("RemoteBuildPath", k_DefaultPath);
            m_Settings.profileSettings.CreateValue("RemoteLoadPath", k_DefaultPath);
            Dictionary<string, string> existingVariableIds = new Dictionary<string, string>();
            foreach (var name in m_Settings.profileSettings.GetVariableNames())
            {
                existingVariableIds.Add(name, m_Settings.profileSettings.GetProfileDataByName(name).Id);
            }

            // Act
            var upgradeNotifications = new UpgradeNotifications();
            upgradeNotifications.DoPathPairMigration(m_Settings);

            // Assert, since we're mapping two arrays by Id we want to be sure the ids match as well as just having the appropriate names
            Assert.IsFalse(m_Settings.profileSettings.GetVariableNames().Contains("LocalBuildPath"));
            Assert.AreEqual(existingVariableIds["LocalBuildPath"], m_Settings.profileSettings.GetProfileDataByName("Local.BuildPath").Id);
            Assert.IsFalse(m_Settings.profileSettings.GetVariableNames().Contains("LocalLoadPath"));
            Assert.AreEqual(existingVariableIds["LocalLoadPath"], m_Settings.profileSettings.GetProfileDataByName("Local.LoadPath").Id);
            Assert.IsFalse(m_Settings.profileSettings.GetVariableNames().Contains("RemoteBuildPath"));
            Assert.AreEqual(existingVariableIds["RemoteBuildPath"], m_Settings.profileSettings.GetProfileDataByName("Remote.BuildPath").Id);
            Assert.IsFalse(m_Settings.profileSettings.GetVariableNames().Contains("RemoteLoadPath"));
            Assert.AreEqual(existingVariableIds["RemoteLoadPath"], m_Settings.profileSettings.GetProfileDataByName("Remote.LoadPath").Id);
        }

        [Test]
        public void UpgradeNotificationsTests_DoPathPairMigration_DoesNotRenameNewEntry()
        {
            // Arrange

            m_Settings.profileSettings.CreateValue(AddressableAssetSettings.kLocalBuildPath, k_DefaultPath);
            m_Settings.profileSettings.CreateValue(AddressableAssetSettings.kLocalLoadPath, k_DefaultPath);
            m_Settings.profileSettings.CreateValue(AddressableAssetSettings.kRemoteBuildPath,k_DefaultPath);
            m_Settings.profileSettings.CreateValue(AddressableAssetSettings.kRemoteLoadPath,k_DefaultPath);
            var upgradeNotifications = new UpgradeNotifications();

            // Act
            upgradeNotifications.DoPathPairMigration(m_Settings);

            // Assert
            Assert.IsTrue(m_Settings.profileSettings.GetVariableNames().Contains(AddressableAssetSettings.kLocalBuildPath));
            Assert.IsTrue(m_Settings.profileSettings.GetVariableNames().Contains(AddressableAssetSettings.kLocalLoadPath));
            Assert.IsTrue(m_Settings.profileSettings.GetVariableNames().Contains(AddressableAssetSettings.kRemoteBuildPath));
            Assert.IsTrue(m_Settings.profileSettings.GetVariableNames().Contains(AddressableAssetSettings.kRemoteLoadPath));
        }
    }
}
