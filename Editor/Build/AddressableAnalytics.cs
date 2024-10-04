using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets
{
    internal static class AddressableAnalytics
    {
        private const string VendorKey = "unity.addressables";
        private static HashSet<string> _registeredEvents = new HashSet<string>();

        private const string UsageEvent = "addressablesUsageEvent";
        private const string BuildEvent = "addressablesBuildEvent";

#if UNITY_2023_1_OR_NEWER
        private static PackageManager.PackageInfo packageInfo = PackageManager.PackageInfo.FindForPackageName("com.unity.addressables");
#endif
        private const string packageName = "com.unity.addressables";

        #if UNITY_2023_3_OR_NEWER
        [AnalyticInfo(vendorKey:VendorKey, eventName:BuildEvent)]
        public class AddressablesBuildEvent : IAnalytic
        {

            AnalyticsBuildData cachedBuildData;
            public AddressablesBuildEvent(BuildData bd)
            {
                cachedBuildData = new AnalyticsBuildData(bd);
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = cachedBuildData;
                error = null;
                if (data == null)
                    return false;
                return true;
            }
        }

        [AnalyticInfo(vendorKey:VendorKey, eventName:UsageEvent)]
        public class AddressablesUsageEvent : IAnalytic
        {
            AnalyticsUsageData cachedUsageData;

            public AddressablesUsageEvent(UsageData ud)
            {
                cachedUsageData = new AnalyticsUsageData(ud);
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = cachedUsageData;
                error = null;
                if (data == null)
                    return false;
                return true;
            }
        }

        [Serializable]
        internal class AnalyticsUsageData : IAnalytic.IData
        {
            public int UsageEventType;
            public bool IsUsingCCD;
            public int AutoRunRestrictionsOption;
            public string package;
            public string package_ver;

            public AnalyticsUsageData(UsageData ud)
            {
                UsageEventType = ud.UsageEventType;
                IsUsingCCD = ud.IsUsingCCD;
                AutoRunRestrictionsOption = ud.AutoRunRestrictionsOption;
                package = packageName;
                package_ver = packageInfo.version;
            }
        }

        [Serializable]
        internal class AnalyticsBuildData : IAnalytic.IData
        {
            public bool IsUsingCCD;
            public bool IsContentUpdateBuild;
            public bool DebugBuildLayoutEnabled;
            public bool AutoOpenBuildReportEnabled;
            public bool BuildAndRelease;
            public bool IsPlayModeBuild;
            public int BuildScript;
            public int NumberOfAddressableAssets;
            public int NumberOfLabels;
            public int NumberOfAssetBundles;
            public int NumberOfGroups;
            public int NumberOfGroupsUsingLZ4;
            public int NumberOfGroupsUsingLZMA;
            public int NumberOfGroupsUncompressed;
            public int NumberOfGroupsPackedTogether;
            public int NumberOfGroupsPackedTogetherByLabel;
            public int NumberOfGroupsPackedSeparately;
            public int MaxNumberOfAddressableAssetsInAGroup;
            public int MinNumberOfAddressableAssetsInAGroup;
            public int BuildTarget;

            public int NumberOfGroupsUsingEditorHosted;
            public int NumberOfGroupsUsingBuiltIn;
            public int NumberOfGroupsUsingCCD;
            public int NumberOfGroupsUsingRemoteCustomPaths;
            public int NumberOfGroupsUsingLocalCustomPaths;

            public int NumberOfAssetsInEditorHostedPaths;
            public int NumberOfAssetsInBuiltInPaths;
            public int NumberOfAssetsInCCDPaths;
            public int NumberOfAssetsInRemoteCustomPaths;
            public int NumberOfAssetsInLocalCustomPaths;

            public int IsIncrementalBuild;
            public int ErrorCode;
            public double TotalBuildTime;
            public string package;
            public string package_ver;

            public AnalyticsBuildData(BuildData bd)
            {
                IsUsingCCD = bd.IsUsingCCD;
                IsContentUpdateBuild = bd.IsContentUpdateBuild;
                IsPlayModeBuild = bd.IsPlayModeBuild;
                BuildScript = bd.BuildScript;
                BuildAndRelease = bd.BuildAndRelease;
#if UNITY_2022_2_OR_NEWER
                DebugBuildLayoutEnabled = bd.DebugBuildLayoutEnabled;
                AutoOpenBuildReportEnabled = bd.AutoOpenBuildReportEnabled;
#endif
                NumberOfLabels = bd.NumberOfLabels;
                IsIncrementalBuild = bd.IsIncrementalBuild;
                NumberOfAssetBundles = bd.NumberOfAssetBundles;
                NumberOfAddressableAssets = bd.NumberOfAddressableAssets;
                MinNumberOfAddressableAssetsInAGroup = bd.MinNumberOfAddressableAssetsInAGroup;
                MaxNumberOfAddressableAssetsInAGroup = bd.MaxNumberOfAddressableAssetsInAGroup;
                NumberOfGroups = bd.NumberOfGroups;
                TotalBuildTime = bd.TotalBuildTime;
                NumberOfGroupsUsingLZ4 = bd.NumberOfGroupsUsingLZ4;
                NumberOfGroupsUsingLZMA = bd.NumberOfGroupsUsingLZMA;
                NumberOfGroupsUncompressed = bd.NumberOfGroupsUncompressed;
                NumberOfGroupsPackedTogether = bd.NumberOfGroupsPackedTogether;
                NumberOfGroupsPackedTogetherByLabel = bd.NumberOfGroupsPackedTogetherByLabel;
                NumberOfGroupsPackedSeparately = bd.NumberOfGroupsPackedSeparately;
                NumberOfGroupsUsingBuiltIn = bd.NumberOfGroupsUsingBuiltIn;
                NumberOfGroupsUsingEditorHosted = bd.NumberOfGroupsUsingEditorHosted;
                NumberOfGroupsUsingRemoteCustomPaths = bd.NumberOfGroupsUsingRemoteCustomPaths;
                NumberOfGroupsUsingLocalCustomPaths = bd.NumberOfGroupsUsingLocalCustomPaths;
                NumberOfGroupsUsingCCD = bd.NumberOfGroupsUsingCCD;
                NumberOfAssetsInRemoteCustomPaths = bd.NumberOfAssetsInRemoteCustomPaths;
                NumberOfAssetsInLocalCustomPaths = bd.NumberOfAssetsInLocalCustomPaths;
                NumberOfAssetsInBuiltInPaths = bd.NumberOfAssetsInBuiltInPaths;
                NumberOfAssetsInEditorHostedPaths = bd.NumberOfAssetsInEditorHostedPaths;
                NumberOfAssetsInCCDPaths = bd.NumberOfAssetsInCCDPaths;
                BuildTarget = bd.BuildTarget;
                ErrorCode = bd.ErrorCode;
                package = packageName;
                package_ver = packageInfo.version;
            }
        }
        #endif

        [Serializable]
        internal struct BuildData
        {
            public bool IsUsingCCD;
            public bool IsContentUpdateBuild;
            public bool DebugBuildLayoutEnabled;
            public bool AutoOpenBuildReportEnabled;
            public bool BuildAndRelease;
            public bool IsPlayModeBuild;
            public int BuildScript;
            public int NumberOfAddressableAssets;
            public int NumberOfLabels;
            public int NumberOfAssetBundles;
            public int NumberOfGroups;
            public int NumberOfGroupsUsingLZ4;
            public int NumberOfGroupsUsingLZMA;
            public int NumberOfGroupsUncompressed;
            public int NumberOfGroupsPackedTogether;
            public int NumberOfGroupsPackedTogetherByLabel;
            public int NumberOfGroupsPackedSeparately;
            public int MaxNumberOfAddressableAssetsInAGroup;
            public int MinNumberOfAddressableAssetsInAGroup;
            public int BuildTarget;

            public int NumberOfGroupsUsingEditorHosted;
            public int NumberOfGroupsUsingBuiltIn;
            public int NumberOfGroupsUsingCCD;
            public int NumberOfGroupsUsingRemoteCustomPaths;
            public int NumberOfGroupsUsingLocalCustomPaths;

            public int NumberOfAssetsInEditorHostedPaths;
            public int NumberOfAssetsInBuiltInPaths;
            public int NumberOfAssetsInCCDPaths;
            public int NumberOfAssetsInRemoteCustomPaths;
            public int NumberOfAssetsInLocalCustomPaths;

            public int IsIncrementalBuild;
            public int ErrorCode;
            public double TotalBuildTime;
        }

        private static string GetSessionStateKeyByUsageEventType(UsageEventType uet)
        {
            switch (uet)
            {
                case UsageEventType.OpenAnalyzeWindow:
                    return "Addressables/Analyze";
                case UsageEventType.OpenGroupsWindow:
                    return "Addressables/Groups";
                case UsageEventType.OpenHostingWindow:
                    return "Addressables/Hosting";
                case UsageEventType.OpenProfilesWindow:
                    return "Addressables/Profiles";
                case UsageEventType.OpenEventViewerWindow:
                    return "Addressables/EventViewer";
                default:
                    return null;
            }
        }

        internal struct UsageData
        {
            public int UsageEventType;
            public bool IsUsingCCD;
            public int AutoRunRestrictionsOption;
        }

        internal enum UsageEventType
        {
            OpenGroupsWindow = 0,
            OpenProfilesWindow = 1,
            OpenEventViewerWindow = 2,
            OpenAnalyzeWindow = 3,
            OpenHostingWindow = 4,
            RunBundleLayoutPreviewRule = 5,
            RunCheckBundleDupeDependenciesRule = 6,
            RunCheckResourcesDupeDependenciesRule = 7,
            RunCheckSceneDupeDependenciesRule = 8,
            InstallCCDManagementPackage = 9,
            ContentUpdateCancelled = 10,
            ContentUpdateHasChangesInUpdateRestrictionWindow = 11,
            ContentUpdateContinuesWithoutChanges = 12,
            RunContentUpdateBuild = 13,
            CannotLocateBinFile = 14,
            BuildFailedDueToModifiedStaticEntries = 15,
            BuildInterruptedDueToStaticModifiedEntriesInUpdate = 16,
            OpenBuildReportManually = 17,
            BuildReportSelectedExploreTab = 18,
            BuildReportSelectedPotentialIssuesTab = 19,
            BuildReportSelectedSummaryTab = 20,
            BuildReportViewByAssetBundle = 21,
            BuildReportViewByAssets = 22,
            BuildReportViewByLabels = 23,
            BuildReportViewByGroup = 24,
            BuildReportViewByDuplicatedAssets = 25,
            BuildReportImportedManually = 26,
            BuildReportOpenRefsTo = 27,
            BuildReportOpenRefsBy = 28,
            BuildReportDrillDownRefsTo = 29,
            BuildReportDrillDownRefsBy = 30,
            BuildReportDetailsSelectInGroup = 31,
            BuildReportDetailsSelectInEditor = 32,
            BuildReportDetailsSelectInBundle = 33,
            BuildReportDetailsClose = 34,
            BuildReportDetailsOpen = 35,
            ProfileModuleViewCreated = 36
        }

        internal enum BuildScriptType
        {
            PackedMode = 0,
            PackedPlayMode = 1,
            FastMode = 2,
            // 3
            CustomBuildScript = 4
        }

        internal enum PathType
        {
            BuiltIn = 0,
            EditorHosted = 1,
            CCD = 2,
            Custom = 3,
            Automatic = 4
        }

        internal enum BuildType
        {
            Inconclusive = -1,
            CleanBuild = 0,
            IncrementalBuild = 1
        }

        internal enum ErrorType
        {
            NoError = 0,
            GenericError = 1
        }

        internal enum AnalyticsContentUpdateRestriction
        {
            NotApplicable = -1,
            ListUpdatedAssetsWithRestrictions = 0,
            FailBuild = 1,
            Disabled = 2
        }

        private static bool EventIsRegistered(string eventName)
        {
            return _registeredEvents.Contains(eventName);
        }

        //Check if the build cache exists so we know if a build is incremental or clean. May return inconclusive if reflection fails
        internal static BuildType DetermineBuildType()
        {
            try
            {
                FieldInfo cachePathField = typeof(BuildCache).GetField("k_CachePath", BindingFlags.Static | BindingFlags.NonPublic);
                if (cachePathField == null)
                    return BuildType.Inconclusive;
                string cachePath = (string)cachePathField.GetValue(null);
                if (cachePath != null && Directory.Exists(cachePath))
                    return BuildType.IncrementalBuild;
                if (cachePath != null)
                    return BuildType.CleanBuild;
                return BuildType.Inconclusive;
            }
            catch
            {
                return BuildType.Inconclusive;
            }
        }

        internal static BuildScriptType DetermineBuildScriptType(IDataBuilder buildScript)
        {
            if (buildScript == null)
                return BuildScriptType.CustomBuildScript;

            var type = buildScript.GetType();
            if (type == typeof(BuildScriptPackedMode))
                return BuildScriptType.PackedMode;
            if (type == typeof(BuildScriptFastMode))
                return BuildScriptType.FastMode;
            if (type == typeof(BuildScriptPackedPlayMode))
                return BuildScriptType.PackedPlayMode;
            return BuildScriptType.CustomBuildScript;
        }

        internal static ErrorType ParseError(string error)
        {
            if (String.IsNullOrEmpty(error))
                return ErrorType.NoError;
            return ErrorType.GenericError;
        }

        internal static BuildData GenerateBuildData(AddressablesDataBuilderInput builderInput, AddressableAssetBuildResult result, BuildType buildType)
        {
            AddressableAssetSettings currentSettings = builderInput.AddressableSettings;
            bool isContentUpdateBuild = builderInput.IsContentUpdateBuild;
            bool isBuildAndRelease = builderInput.IsBuildAndRelease;

            bool usingCCD = false;

            string error = result.Error;
            bool isPlayModeBuild = result is AddressablesPlayModeBuildResult;
            double totalBuildDurationSeconds = result.Duration;
            int numberOfAssetBundles = -1;

            if (result is AddressablesPlayerBuildResult buildRes)
            {
                numberOfAssetBundles = buildRes.AssetBundleBuildResults.Count;
            }

#if ENABLE_CCD
            usingCCD = true;
#endif

            ErrorType errorCode = ParseError(error);

            if (isPlayModeBuild)
            {
                BuildScriptType playModeBuildScriptType = DetermineBuildScriptType(currentSettings.ActivePlayModeDataBuilder);
                BuildData playModeBuildData = new BuildData()
                {
                    IsPlayModeBuild = true,
                    BuildScript = (int)playModeBuildScriptType
                };

                return playModeBuildData;
            }

            BuildScriptType buildScriptType = DetermineBuildScriptType(currentSettings.ActivePlayerDataBuilder);
            int numberOfAddressableAssets = 0;

            int numberOfGroupsUncompressed = 0;
            int numberOfGroupsUsingLZMA = 0;
            int numberOfGroupsUsingLZ4 = 0;

            int numberOfGroupsPackedSeparately = 0;
            int numberOfGroupsPackedTogether = 0;
            int numberOfGroupsPackedTogetherByLabel = 0;

            int minNumberOfAssetsInAGroup = -1;
            int maxNumberOfAssetsInAGroup = -1;

            int numberOfGroupsUsingEditorHosted = 0;
            int numberOfGroupsUsingBuiltIn = 0;
            int numberOfGroupsUsingCCD = 0;
            int numberOfGroupsUsingRemoteCustomPaths = 0;
            int numberOfGroupsUsingLocalCustomPaths = 0;

            int numberOfAssetsInEditorHostedPaths = 0;
            int numberOfAssetsInBuiltInPaths = 0;
            int numberOfAssetsInCCDPaths = 0;
            int numberOfAssetsInRemoteCustomPaths = 0;
            int numberOfAssetsInLocalCustomPaths = 0;


            List<ProfileGroupType> groupTypes = ProfileGroupType.CreateGroupTypes(currentSettings.profileSettings.GetProfile(currentSettings.activeProfileId), currentSettings);
            var dataSourceSettings = ProfileDataSourceSettings.GetSettings();
            Dictionary<string, PathType> prefixToTypeMap = new Dictionary<string, PathType>();

            foreach (var groupType in groupTypes)
            {
                ProfileGroupType groupTypeArchetype = dataSourceSettings.FindGroupType(groupType);
                if (groupTypeArchetype == null)
                    prefixToTypeMap.Add(groupType.GroupTypePrefix, PathType.Custom);
                else if (groupTypeArchetype.GroupTypePrefix == "Built-In")
                    prefixToTypeMap.Add(groupType.GroupTypePrefix, PathType.BuiltIn);
                else if (groupTypeArchetype.GroupTypePrefix == "Editor Hosted")
                    prefixToTypeMap.Add(groupType.GroupTypePrefix, PathType.EditorHosted);
                else if (groupTypeArchetype.GroupTypePrefix.StartsWith("CCD", StringComparison.Ordinal))
                    prefixToTypeMap.Add(groupType.GroupTypePrefix, PathType.CCD);
                else if (groupTypeArchetype.GroupTypePrefix.StartsWith("Automatic", StringComparison.Ordinal))
                    prefixToTypeMap.Add(groupType.GroupTypePrefix, PathType.CCD);
            }

            HashSet<string> vars = currentSettings.profileSettings.GetAllVariableIds();

            foreach (var group in currentSettings.groups)
            {
                if (group == null)
                    continue;

                numberOfAddressableAssets += group.entries.Count;

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                    continue;

                int selected = schema.DetermineSelectedIndex(groupTypes, -1, currentSettings, vars);

                PathType pathType;
                if (selected == -1)
                    pathType = PathType.Custom;
                else
                    pathType = prefixToTypeMap[groupTypes[selected].GroupTypePrefix];

                if (pathType == PathType.Custom)
                {
                    if (ResourceManagerConfig.IsPathRemote(schema.LoadPath.GetValue(currentSettings)))
                    {
                        numberOfGroupsUsingRemoteCustomPaths += 1;
                        numberOfAssetsInRemoteCustomPaths += group.entries.Count;
                    }
                    else
                    {
                        numberOfGroupsUsingLocalCustomPaths += 1;
                        numberOfAssetsInLocalCustomPaths += group.entries.Count;
                    }
                }

                if (pathType == PathType.BuiltIn)
                {
                    numberOfGroupsUsingBuiltIn += 1;
                    numberOfAssetsInBuiltInPaths += group.entries.Count;
                }

                if (pathType == PathType.EditorHosted)
                {
                    numberOfGroupsUsingEditorHosted += 1;
                    numberOfAssetsInEditorHostedPaths += group.entries.Count;
                }

                if (pathType == PathType.CCD)
                {
                    numberOfGroupsUsingCCD += 1;
                    numberOfAssetsInCCDPaths += group.entries.Count;
                }

                if (pathType == PathType.Automatic)
                {
                    numberOfGroupsUsingCCD += 1;
                    numberOfAssetsInCCDPaths += group.entries.Count;
                }

                var bundleMode = schema.BundleMode;
                var compressionType = schema.Compression;

                switch (compressionType)
                {
                    case BundledAssetGroupSchema.BundleCompressionMode.Uncompressed:
                        numberOfGroupsUncompressed += 1;
                        break;
                    case BundledAssetGroupSchema.BundleCompressionMode.LZ4:
                        numberOfGroupsUsingLZ4 += 1;
                        break;
                    case BundledAssetGroupSchema.BundleCompressionMode.LZMA:
                        numberOfGroupsUsingLZMA += 1;
                        break;
                }

                switch (bundleMode)
                {
                    case BundledAssetGroupSchema.BundlePackingMode.PackSeparately:
                        numberOfGroupsPackedSeparately += 1;
                        break;
                    case BundledAssetGroupSchema.BundlePackingMode.PackTogether:
                        numberOfGroupsPackedTogether += 1;
                        break;
                    case BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel:
                        numberOfGroupsPackedTogetherByLabel += 1;
                        break;
                }

                if (group.entries.Count > maxNumberOfAssetsInAGroup)
                    maxNumberOfAssetsInAGroup = group.entries.Count;
                if (minNumberOfAssetsInAGroup == -1 || group.entries.Count < minNumberOfAssetsInAGroup)
                    minNumberOfAssetsInAGroup = group.entries.Count;
            }

            BuildData data = new BuildData()
            {
                IsUsingCCD = usingCCD,
                IsContentUpdateBuild = isContentUpdateBuild,
                IsPlayModeBuild = false,
                BuildScript = (int)buildScriptType,
                BuildAndRelease = isBuildAndRelease,
                DebugBuildLayoutEnabled = ProjectConfigData.GenerateBuildLayout,
                AutoOpenBuildReportEnabled = ProjectConfigData.AutoOpenAddressablesReport && ProjectConfigData.GenerateBuildLayout,
                NumberOfLabels = currentSettings.labelTable.Count,
                IsIncrementalBuild = (int)buildType,
                NumberOfAssetBundles = numberOfAssetBundles,
                NumberOfAddressableAssets = numberOfAddressableAssets,
                MinNumberOfAddressableAssetsInAGroup = minNumberOfAssetsInAGroup,
                MaxNumberOfAddressableAssetsInAGroup = maxNumberOfAssetsInAGroup,
                NumberOfGroups = currentSettings.groups.Count,
                TotalBuildTime = totalBuildDurationSeconds,
                NumberOfGroupsUsingLZ4 = numberOfGroupsUsingLZ4,
                NumberOfGroupsUsingLZMA = numberOfGroupsUsingLZMA,
                NumberOfGroupsUncompressed = numberOfGroupsUncompressed,
                NumberOfGroupsPackedTogether = numberOfGroupsPackedTogether,
                NumberOfGroupsPackedTogetherByLabel = numberOfGroupsPackedTogetherByLabel,
                NumberOfGroupsPackedSeparately = numberOfGroupsPackedSeparately,
                NumberOfGroupsUsingBuiltIn = numberOfGroupsUsingBuiltIn,
                NumberOfGroupsUsingEditorHosted = numberOfGroupsUsingEditorHosted,
                NumberOfGroupsUsingRemoteCustomPaths = numberOfGroupsUsingRemoteCustomPaths,
                NumberOfGroupsUsingLocalCustomPaths = numberOfGroupsUsingLocalCustomPaths,
                NumberOfGroupsUsingCCD = numberOfGroupsUsingCCD,
                NumberOfAssetsInRemoteCustomPaths = numberOfAssetsInRemoteCustomPaths,
                NumberOfAssetsInLocalCustomPaths = numberOfAssetsInLocalCustomPaths,
                NumberOfAssetsInBuiltInPaths = numberOfAssetsInBuiltInPaths,
                NumberOfAssetsInEditorHostedPaths = numberOfAssetsInEditorHostedPaths,
                NumberOfAssetsInCCDPaths = numberOfAssetsInCCDPaths,
                BuildTarget = (int)EditorUserBuildSettings.activeBuildTarget,
                ErrorCode = (int)errorCode
            };

            return data;
        }

        internal static void ReportBuildEvent(AddressablesDataBuilderInput builderInput, AddressableAssetBuildResult result, BuildType buildType)
        {
            if (!EditorAnalytics.enabled)
                return;

            BuildData data = GenerateBuildData(builderInput, result, buildType);

            // This will probably cause problems somewhere, but whatever.
#if UNITY_2023_3_OR_NEWER
            AddressablesBuildEvent evt = new AddressablesBuildEvent(data);
            EditorAnalytics.SendAnalytic(evt);
#else
            #pragma warning disable CS0618 // Type or member is obsolete
            if (!EventIsRegistered(BuildEvent))
                if (!RegisterEvent(BuildEvent))
                    return;
            EditorAnalytics.SendEventWithLimit(BuildEvent, data);
            #pragma warning restore CS0618 // Type or member is obsolete
#endif
        }

        internal static UsageData GenerateUsageData(UsageEventType eventType, AnalyticsContentUpdateRestriction restriction = AnalyticsContentUpdateRestriction.NotApplicable)
        {
            bool usingCCD = false;

#if ENABLE_CCD
            usingCCD = true;
#endif

            var data = new UsageData()
            {
                UsageEventType = (int)eventType,
                IsUsingCCD = usingCCD,
                AutoRunRestrictionsOption = (int)restriction
            };

            return data;
        }

        internal static void ReportUsageEvent(UsageEventType eventType, bool limitEventOncePerSession = false, int contentUpdateRestriction = -1)
        {
            if (!EditorAnalytics.enabled)
                return;

            var sessionStateKey = GetSessionStateKeyByUsageEventType(eventType);

            if (limitEventOncePerSession && sessionStateKey != null && SessionState.GetBool(sessionStateKey, false))
                return;

            if (!SessionState.GetBool(sessionStateKey, false))
                SessionState.SetBool(sessionStateKey, true);

            UsageData data = GenerateUsageData(eventType, (AnalyticsContentUpdateRestriction) contentUpdateRestriction);
#if UNITY_2023_3_OR_NEWER
            AddressablesUsageEvent evt = new AddressablesUsageEvent(data);
            EditorAnalytics.SendAnalytic(evt);
#else
            #pragma warning disable CS0618 // Type or member is obsolete
            if (!EventIsRegistered(UsageEvent))
                if (!RegisterEvent(UsageEvent))
                    return;
            EditorAnalytics.SendEventWithLimit(UsageEvent, data);
            #pragma warning restore CS0618 // Type or member is obsolete
#endif
        }

        private static bool RegisterEvent(string eventName)
        {
            bool eventSuccessfullyRegistered = false;
            #pragma warning disable CS0618 // Type or member is obsolete
            AnalyticsResult registerEvent = EditorAnalytics.RegisterEventWithLimit(eventName, 100, 100, VendorKey);
            #pragma warning restore CS0618 // Type or member is obsolete
            if (registerEvent == AnalyticsResult.Ok)
            {
                _registeredEvents.Add(eventName);
                eventSuccessfullyRegistered = true;
            }

            return eventSuccessfullyRegistered;
        }
    }
}
