using System;
using System.IO;
using System.Text;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// Makes a new build of Addressables using the BuildScript selectable in the menu
    /// </summary>
    public class AddressablesBuildMenuNewBuild : AddressableAssetsSettingsGroupEditor.IAddressablesBuildMenu
    {
        /// <inheritdoc />
        public virtual string BuildMenuPath
        {
            get => "New Build";
        }

        /// <inheritdoc />
        public virtual bool SelectableBuildScript
        {
            get => true;
        }

        /// <inheritdoc />
        public virtual int Order
        {
            get => -20;
        }

        /// <inheritdoc />
        public virtual bool OnPrebuild(AddressablesDataBuilderInput input)
        {
            return true;
        }

        /// <inheritdoc />
        public virtual bool OnPostbuild(AddressablesDataBuilderInput input, AddressablesPlayerBuildResult result)
        {
            return true;
        }
    }

    /// <summary>
    /// Opens a file browser to search for the addressables_content_state.bin from from a previous build to build and update for.
    /// </summary>
    public class AddressablesBuildMenuUpdateAPreviousBuild : AddressableAssetsSettingsGroupEditor.IAddressablesBuildMenu
    {
        /// <inheritdoc />
        public virtual string BuildMenuPath
        {
            get => "Update a Previous Build";
        }

        /// <inheritdoc />
        public virtual bool SelectableBuildScript
        {
            get => false;
        }

        /// <inheritdoc />
        public virtual int Order
        {
            get => -10;
        }

        /// <inheritdoc />
        public virtual bool OnPrebuild(AddressablesDataBuilderInput input)
        {
            AddressableAssetSettings settings = input.AddressableSettings == null ? AddressableAssetSettingsDefaultObject.Settings : input.AddressableSettings;
            if (settings == null)
                return false;

            return OnUpdateBuild(input);
        }

        /// <inheritdoc />
        public virtual bool OnPostbuild(AddressablesDataBuilderInput input, AddressablesPlayerBuildResult result)
        {
            return true;
        }

        bool OnUpdateBuild(AddressablesDataBuilderInput input)
        {
            AddressableAssetSettings settings = input.AddressableSettings;

            AddressableAnalytics.UsageEventType eventType = AddressableAnalytics.UsageEventType.RunContentUpdateBuild;
            bool doContentUpdate = true;
            bool buildCancelled = false;
            bool continueWithoutPreviousState = false;

            bool hasUpdatedContentSinceUpdate = Convert.ToBoolean(EditorPrefs.GetInt(ContentUpdateScript.FirstTimeUpdatePreviousBuild, 0));
            if (!hasUpdatedContentSinceUpdate)
            {
                int firstTimeSelection = EditorUtility.DisplayDialogComplex("New Content Update",
                    "Starting in 1.20.0+ \"Update Previous Build\" now automatically checks the previous addressables_content_state.bin for update restrictions. " +
                    "This automatic behavior, and the file location, can be adjusted in the Addressable Asset Settings.", "Continue", "Cancel build", "Open Settings");
                EditorPrefs.SetInt(ContentUpdateScript.FirstTimeUpdatePreviousBuild, 1);
                switch (firstTimeSelection)
                {
                    //continue
                    case 0:
                        //do nothing
                        break;

                    //Cancel build
                    case 1:
                        Debug.Log("Update Content build cancelled by user.");
                        buildCancelled = true;
                        break;

                    //Open settings
                    case 2:
                        //Same code that gets executed when clicking Inspect System Settings from the tools dropdown
                        EditorApplication.ExecuteMenuItem("Window/General/Inspector");
                        EditorGUIUtility.PingObject(AddressableAssetSettingsDefaultObject.Settings);
                        Selection.activeObject = AddressableAssetSettingsDefaultObject.Settings;
                        doContentUpdate = false;
                        break;
                }
            }

            if (buildCancelled)
                return false;

            //Attempt content update build
            var path = ContentUpdateScript.GetContentStateDataPath(false);

            if (ResourceManagerConfig.ShouldPathUseWebRequest(path))
                path = ContentUpdateScript.DownloadBinFileToTempLocation(path);

            if (!string.IsNullOrEmpty(path) && doContentUpdate)
            {
                if (!File.Exists(path))
                {
                    AddressableAnalytics.ReportUsageEvent(AddressableAnalytics.UsageEventType.CannotLocateBinFile, false, (int)settings.CheckForContentUpdateRestrictionsOption);

                    //Unable to find Content Update .bin
                    bool selectBin = EditorUtility.DisplayDialog("Unable to Check for Update Restrictions", $"The addressable_content_state.bin file could " +
                                                                                                            $"not be found at {path}", "Select .bin file", "Cancel content update");

                    if (selectBin)
                        path = ContentUpdateScript.GetContentStateDataPath(true);
                    else
                    {
                        Debug.Log("Update a Previous Build cancelled by user.");
                        buildCancelled = true;
                    }
                }

                if (buildCancelled)
                    return false;

                if (doContentUpdate)
                {
                    var checkForRestrictionsBehavior = settings.CheckForContentUpdateRestrictionsOption;
                    //If we're in batch mode and the setting is "list restrictions" we just need to fail the build.
                    if (checkForRestrictionsBehavior == CheckForContentUpdateRestrictionsOptions.ListUpdatedAssetsWithRestrictions &&
                        UnityEditorInternal.InternalEditorUtility.inBatchMode)
                        checkForRestrictionsBehavior = CheckForContentUpdateRestrictionsOptions.FailBuild;
                    if (continueWithoutPreviousState)
                        checkForRestrictionsBehavior = CheckForContentUpdateRestrictionsOptions.Disabled;

                    //Check for content update restrictions
                    switch (checkForRestrictionsBehavior)
                    {
                        case CheckForContentUpdateRestrictionsOptions.Disabled:
                            //do nothing
                            break;
                        case CheckForContentUpdateRestrictionsOptions.FailBuild:
                            var modifiedEntries = ContentUpdateScript.GatherModifiedEntriesWithDependencies(settings, path);
                            if (modifiedEntries.Count > 0)
                            {
                                AddressableAnalytics.ReportUsageEvent(AddressableAnalytics.UsageEventType.BuildFailedDueToModifiedStaticEntries, false,
                                    (int)CheckForContentUpdateRestrictionsOptions.FailBuild);
                                StringBuilder sb = new StringBuilder();
                                sb.AppendLine("Modified entries in Cannot Change Post Release Groups were detected. The following changes were detected:");
                                foreach (var entry in modifiedEntries)
                                    sb.AppendLine(entry.Key.AssetPath);
                                doContentUpdate = false;
                                Debug.LogError(sb.ToString());
                            }

                            break;
                        case CheckForContentUpdateRestrictionsOptions.ListUpdatedAssetsWithRestrictions:
                            var modifiedEntriesList = ContentUpdateScript.GatherModifiedEntriesWithDependencies(settings, path);
                            if (modifiedEntriesList.Count > 0)
                            {
                                eventType = AddressableAnalytics.UsageEventType.BuildInterruptedDueToStaticModifiedEntriesInUpdate;
                                doContentUpdate = false;
                                ContentUpdatePreviewWindow.ShowUpdatePreviewWindow(settings, modifiedEntriesList, () =>
                                {
                                    var cacheData = ContentUpdateScript.LoadContentState(path);
                                    if (cacheData != null)
                                    {
                                        input.PlayerVersion = cacheData.playerVersion;
                                        input.PreviousContentState = cacheData;
                                    }

                                    AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult rst, input);
                                    OnPostbuild(input, rst);
                                });
                            }

                            break;
                    }

                    AddressableAnalytics.ReportUsageEvent(eventType, false, (int)checkForRestrictionsBehavior);

                    if (doContentUpdate)
                    {
                        var cacheData = ContentUpdateScript.LoadContentState(path);
                        if (cacheData != null)
                        {
                            input.PlayerVersion = cacheData.playerVersion;
                            input.PreviousContentState = cacheData;
                        }
                    }
                }
            }

            return doContentUpdate;
        }
    }
}
