using UnityEditor.AddressableAssets.Settings;

namespace UnityEditor.AddressableAssets.GUI
{
    internal class UpgradeNotifications
    {
        private readonly string[] oldEntryNames = new string[]
        {
            "LocalBuildPath", "LocalLoadPath", "RemoteBuildPath", "RemoteLoadPath"
        };

        private readonly string[] newEntryNames = new string[]
        {
            AddressableAssetSettings.kLocalBuildPath, AddressableAssetSettings.kLocalLoadPath, AddressableAssetSettings.kRemoteBuildPath, AddressableAssetSettings.kRemoteLoadPath
        };

        internal bool NeedsPathPairMigration(AddressableAssetSettings settings)
        {
            for (int i = 0; i < oldEntryNames.Length; i++)
            {
                // basically we verify that the old variable exists and that the new variable does not yet exist
                if (settings.profileSettings.GetVariableNames().Contains(oldEntryNames[i]) &&
                    settings.profileSettings.ValidateNewVariableName(newEntryNames[i]))
                {
                    return true;
                }
            }

            return false;
        }

        internal void DoPathPairMigration(AddressableAssetSettings settings)
        {
            for (int i = 0; i < oldEntryNames.Length; i++)
            {
                if (settings.profileSettings.GetVariableNames().Contains(oldEntryNames[i]) &&
                    settings.profileSettings.ValidateNewVariableName(newEntryNames[i]))
                {
                    var profileVariable = settings.profileSettings.GetProfileDataByName(oldEntryNames[i]);
                    Undo.RecordObject(settings, "Profile Variable Renamed");
                    profileVariable.SetName(newEntryNames[i], settings.profileSettings);
                    AddressableAssetUtility.OpenAssetIfUsingVCIntegration(settings, true);
                }
            }
        }

        internal void ShowUpgradeNotifications(AddressableAssetSettings settings)
        {
            bool pathPairMigrationCheck = ProjectConfigData.UserHasBeenInformedAboutPathPairMigration;
            if (!pathPairMigrationCheck && NeedsPathPairMigration(settings))
            {
                bool doPathPairMigration = EditorUtility.DisplayDialog("Path Pair Migration",
                    "Addressables has migrated to a new path pair format. This is necessary for some advanced features like CCD integration." +
                    "We can automatically migrate path pairs like 'RemoteLoadPath' to the new format 'Remote.LoadPath'.  Would you like us to do that for you?", "Yes", "No");
                if (doPathPairMigration)
                    DoPathPairMigration(settings);
                ProjectConfigData.UserHasBeenInformedAboutPathPairMigration = true;
            }
        }

        internal static void Show(AddressableAssetSettings settings)
        {
            var upgradeNotifications = new UpgradeNotifications();
            upgradeNotifications.ShowUpgradeNotifications(settings);
        }
    }
}
