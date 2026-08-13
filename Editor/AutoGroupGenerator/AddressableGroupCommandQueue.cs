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
    /// Command queue that converts group layouts into Addressables groups.
    /// </summary>
    internal class AddressableGroupCommandQueue : CommandQueue
    {
        #region Fields
        private readonly DataContainer m_DataContainer;

        private int m_AddressableGroupCreated;

        private int m_AddressableGroupReused;

        Dictionary<string, AddressableAssetGroup> m_ExistingGroups;
        Dictionary<string, AddressableAssetGroupTemplate> m_Templates;
        #endregion

        #region Properties
        private AddressableAssetSettings AddressableSettings => AddressableAssetSettingsDefaultObject.Settings;
        #endregion

        #region Methods
        public AddressableGroupCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;

            Title = nameof(AddressableGroupCommandQueue);
        }

        public override void PreExecute()
        {
            ClearQueue();

            m_ExistingGroups = AddressableSettings.groups.ToDictionary(k => k.Name, v => v);
            m_Templates = AddressableSettings.GroupTemplateObjects.ToDictionary(k => k.name, v => (AddressableAssetGroupTemplate)v);

            AddCommand(StartAssetEditing);

            foreach (var pair in m_DataContainer.GroupLayout)
            {
                var groupName = pair.Key;
                var groupLayoutInfo = pair.Value;

                AddCommand(() => CreateGroupAndMoveAssets(groupName, groupLayoutInfo), groupName);
            }

            AddCommand(StopAssetEditing);
        }

        public override void PostExecute()
        {
            SaveOutputReportToFile();

            m_ExistingGroups = null;
            m_Templates = null;
        }

        private void StartAssetEditing()
        {
            AssetDatabase.Refresh();

            AssetDatabase.StartAssetEditing();

            m_DataContainer.AssetEditingInProgress = true;
        }

        private void CreateGroupAndMoveAssets(string groupName, GroupLayout groupLayout)
        {
            if (m_ExistingGroups.TryGetValue(groupName, out AddressableAssetGroup group))
            {
                m_AddressableGroupReused++;
            }
            else
            {

                group = CreateNewGroup(groupName, groupLayout.TemplateName);

                m_AddressableGroupCreated++;
            }

            foreach (var node in groupLayout.Nodes)
            {
                string assetGuid = node.Guid.ToString();

                if (string.IsNullOrEmpty(assetGuid))
                {
                    throw new Exception($"Asset with path '{node.AssetPath}' not found in project.");
                }


                AddressableAssetEntry entry = AddressableSettings.CreateOrMoveEntry(assetGuid, group, false, false);

                if (entry == null)
                {
                    throw new Exception($"Failed to add asset '{node.AssetPath}' to group '{group.name}'.");
                }
            }
        }

        private AddressableAssetGroup CreateNewGroup(string name, string templateName)
        {
            List<AddressableAssetGroupSchema> schemasToCopy = null;
            if (m_Templates.TryGetValue(templateName, out AddressableAssetGroupTemplate template))
                schemasToCopy = template.SchemaObjects;

            return AddressableSettings.CreateGroup(name, false, false,
                false, schemasToCopy, Type.EmptyTypes);
        }

        private void ApplyTemplateValuesToGroup(AddressableAssetGroup group, string templateName)
        {
            if (m_Templates.TryGetValue(templateName, out AddressableAssetGroupTemplate template))
                template.ApplyToAddressableAssetGroup(group);
        }

        private void StopAssetEditing()
        {
            AssetDatabase.StopAssetEditing();

            m_DataContainer.AssetEditingInProgress = false;

            AssetDatabase.Refresh();
        }

        void SaveOutputReportToFile()
        {
            if (!m_DataContainer.Settings.ProcessReport.HasFlag(ProcessStepReport.AddressableGroups))
                return;

            var summary = string.Empty;
            summary += $"{nameof(m_AddressableGroupCreated).ToReadableFormat()} = {m_AddressableGroupCreated}, ";
            summary += $"{nameof(m_AddressableGroupReused).ToReadableFormat()} = {m_AddressableGroupReused}";
            object data = string.Empty;

            JsonReport.SaveJsonReport(GetType(), summary, data);
        }
        #endregion
    }
}
