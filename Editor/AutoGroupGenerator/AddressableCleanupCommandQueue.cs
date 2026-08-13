using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Command queue that synchronizes Addressables data with AutoGroupGenerator definitions.
    /// </summary>
    internal class AddressableCleanupCommandQueue : CommandQueue
    {
        #region Static Methods
        private static HashSet<string> GetAddressableScenePaths()
        {
            var paths = new HashSet<string>();

            var settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                return paths;
            }

            foreach (var entry in AddressableUtil.GetAddressableEntries())
            {
                if (entry.MainAssetType == typeof(SceneAsset))
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);

                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        paths.Add(assetPath);
                    }
                }
            }

            return paths;
        }
        #endregion

        #region Fields
        private readonly DataContainer m_DataContainer;

        private int m_EmptyGroupRemoved;

        private int m_UnnecessaryEntriesRemoved;

        HashSet<string> m_AddressableScenes;

        List<string> m_RemovedEntriesReport;
        List<string> m_RemovedGroupsReport;
        #endregion

        #region Properties
        private AddressableAssetSettings AddressableSettings => AddressableAssetSettingsDefaultObject.Settings;

        bool IsReportEnabled => m_DataContainer.Settings.ProcessReport.HasFlag(ProcessStepReport.Cleanup);
        #endregion

        #region Methods
        public AddressableCleanupCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;

            Title = nameof(AddressableCleanupCommandQueue);
        }

        public override void PreExecute()
        {
            ClearQueue();

            AddCommand(StartAssetEditing);

            AddCommand(RemoveUnusedEntries);

            AddCommand(RemoveEmptyAddressableGroups);

            AddCommand(RemoveAddressableScenesFromBuildProfile);

            AddCommand(SortGroups);

            AddCommand(StopAssetEditing);
        }

        public override void PostExecute()
        {
            SaveOutputReportToFile();

            m_RemovedEntriesReport = null;
            m_RemovedGroupsReport = null;
            m_AddressableScenes = null;
        }

        private void StartAssetEditing()
        {
            AssetDatabase.StartAssetEditing();

            m_DataContainer.AssetEditingInProgress = true;
        }

        private void RemoveUnusedEntries()
        {
            if (!m_DataContainer.Settings.RemoveUnnecessaryEntries)
            {
                return;
            }


            var allNodesInGroupLayouts = new HashSet<string>();

            foreach (var groupLayout in m_DataContainer.GroupLayout.Values)
            {
                foreach (var node in groupLayout.Nodes)
                {
                    allNodesInGroupLayouts.Add(node.Guid.ToString());
                }
            }

            var entriesToRemove = new List<string>();
            m_RemovedEntriesReport = new List<string>();

            foreach (var entry in AddressableUtil.GetAddressableEntries())
            {
                var entryGuid = entry.guid;

                if(!allNodesInGroupLayouts.Contains(entryGuid))
                {
                    entriesToRemove.Add(entryGuid);

                    if(IsReportEnabled)
                        m_RemovedEntriesReport.Add(entry.AssetPath);
                }
            }

            foreach (var guid in entriesToRemove)
            {
                AddressableSettings.RemoveAssetEntry(guid, false);

                m_UnnecessaryEntriesRemoved++;
            }
        }

        private void RemoveEmptyAddressableGroups()
        {
            if (!m_DataContainer.Settings.RemoveEmptyGroups)
            {
                return;
            }


            IEnumerable<AddressableAssetGroup> groupsToRemove = AddressableSettings.groups
                .Where(CanRemoveGroup).ToList();

            if (IsReportEnabled)
                m_RemovedGroupsReport = groupsToRemove.Select(g => g.Name).ToList();

            foreach (var group in groupsToRemove)
            {

                RemoveGroupQuick(group);

                m_EmptyGroupRemoved++;
            }
        }

        void RemoveGroupQuick(AddressableAssetGroup group)
        {
            if (group == null)
                return;

            group.ClearSchemas(true);
            AddressableSettings.groups.Remove(group);

            if (group != null)
            {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(group, out var guidOfGroup, out long localId))
                {
                    var groupPath = AssetDatabase.GUIDToAssetPath(guidOfGroup);
                    if (!string.IsNullOrEmpty(groupPath))
                        AssetDatabase.DeleteAsset(groupPath);
                }
            }
        }

        private bool CanRemoveGroup(AddressableAssetGroup group)
        {
            return group != null &&
                   group.entries.Count == 0 &&
                   !group.ReadOnly &&
                   group != AddressableSettings.DefaultGroup;
        }

        private void RemoveAddressableScenesFromBuildProfile()
        {
            if (!m_DataContainer.Settings.RemoveAddressableScenesFromBuildProfile)
            {
                return;
            }


            m_AddressableScenes = GetAddressableScenePaths();

            List<EditorBuildSettingsScene> originalScenes = EditorBuildSettings.scenes.ToList();

            int removedCount = 0;

            var updatedScenes = new List<EditorBuildSettingsScene>();

            foreach (EditorBuildSettingsScene scene in originalScenes)
            {
                if (!m_AddressableScenes.Contains(scene.path))
                {
                    updatedScenes.Add(scene);
                }
                else
                {


                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                EditorBuildSettings.scenes = updatedScenes.ToArray();
            }
        }

        private void SortGroups()
        {
            if (!m_DataContainer.Settings.SortAddressableGroups)
            {
                return;
            }


            AddressableSettings.groups.Sort(ComparisonLogic);

            int ComparisonLogic(AddressableAssetGroup a, AddressableAssetGroup b)
            {
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void StopAssetEditing()
        {
            AssetDatabase.StopAssetEditing();

            m_DataContainer.AssetEditingInProgress = false;

            AssetDatabase.Refresh();
        }

        void SaveOutputReportToFile()
        {
            if (!IsReportEnabled)
                return;

            var reporter = GetType();

            if (m_DataContainer.Settings.RemoveUnnecessaryEntries)
            {
                var summary = $"Unused Entries Removed ({m_RemovedEntriesReport.Count})";
                object data = new List<string>(m_RemovedEntriesReport);
                JsonReport.SaveJsonReport(reporter, reporter.Name + "_UnusedEntriesRemoved", summary, data);
            }

            if (m_DataContainer.Settings.RemoveEmptyGroups)
            {
                var summary = $"Empty Groups Removed ({m_RemovedGroupsReport.Count})";
                object data = new List<string>(m_RemovedGroupsReport);
                JsonReport.SaveJsonReport(reporter, reporter.Name + "_EmptyGroupsRemoved", summary, data);
            }

            if (m_DataContainer.Settings.RemoveAddressableScenesFromBuildProfile)
            {
                var summary = $"Scenes Removed from Build Profile ({m_AddressableScenes.Count})";
                object data = new List<string>(m_AddressableScenes);
                JsonReport.SaveJsonReport(reporter, reporter.Name + "_ScenesRemoved", summary, data);
            }
        }
        #endregion
    }
}
