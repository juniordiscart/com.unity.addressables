using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Base class for output rules that filter or transform group layouts.
    /// </summary>
    public abstract class OutputRule : ScriptableObject
    {
        #region Fields
        /// <summary>
        /// Template applied to generated group layouts.
        /// </summary>
        [SerializeField]
        protected AddressableAssetGroupTemplate m_Template;

        /// <summary>
        /// Data container providing input, output, and settings.
        /// </summary>
        protected DataContainer m_DataContainer;

        /// <summary>
        /// Cached selection of group layouts targeted by this rule.
        /// </summary>
        protected List<GroupLayout> m_Selection;
        #endregion

        #region Methods
        private void OnValidate()
        {
            if (m_Template == null)
            {
                m_Template = AddressableUtil.FindDefaultAddressableGroupTemplate();
            }
        }

        /// <summary>
        /// Initializes the rule with the shared data container.
        /// </summary>
        /// <param name="dataContainer">The data container to use.</param>
        public virtual void Initialize(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
        }

        /// <summary>
        /// Selects group layouts that match this rule's criteria.
        /// </summary>
        public virtual void Select()
        {
            m_Selection = m_DataContainer.GroupLayout.Values.Where(DoesMatchSelectionCriteria).ToList();
        }

        /// <summary>
        /// Applies rule-specific refinements to the selected layouts.
        /// </summary>
        public virtual void Refine()
        {
            ApplyTemplate(m_Selection);
        }

        /// <summary>
        /// Clears cached state after execution.
        /// </summary>
        public virtual void UnInit()
        {
            m_DataContainer = null;
            m_Selection = null;
        }

        /// <summary>
        /// Determines whether a group layout matches this rule.
        /// </summary>
        /// <param name="groupLayout">The layout to evaluate.</param>
        /// <returns>True if the layout matches.</returns>
        protected abstract bool DoesMatchSelectionCriteria(GroupLayout groupLayout);

        /// <summary>
        /// Merges multiple group layouts into a single layout.
        /// </summary>
        /// <param name="groupLayouts">Group layouts to merge.</param>
        /// <param name="mergedGroupName">Optional name for the merged layout.</param>
        protected void Merge(List<GroupLayout> groupLayouts, string mergedGroupName = null)
        {
            if (groupLayouts == null || groupLayouts.Count == 0)
            {
                return;
            }


            foreach (var groupLayout in groupLayouts)
            {
                if (!m_DataContainer.GroupLayout.ContainsKey(groupLayout.Name))
                {
                    Debug.LogError($"Cannot find group layout name = {groupLayout.Name}");

                    return;
                }
            }


            var keysToRemove = new List<string>();

            var allNodes = new HashSet<AssetNode>();

            var allSources = new HashSet<AssetNode>();

            foreach (var groupLayout in groupLayouts)
            {
                keysToRemove.Add(groupLayout.Name);

                allNodes.UnionWith(groupLayout.Nodes);

                allSources.UnionWith(groupLayout.Sources);
            }

            var hashOfAllSources = SubgraphCommandQueue.CalculateHashForSources(allSources);

            string groupLayoutName = !string.IsNullOrEmpty(mergedGroupName)
                ? mergedGroupName
                : $"Merged_Shared_Assets_{hashOfAllSources}";


            var mergedGroupLayout = new GroupLayout
            {
                Nodes = allNodes,
                Sources = allSources,
                HashOfSources = hashOfAllSources,
                Name = groupLayoutName,
            };

            foreach (var key in keysToRemove)
            {
                m_DataContainer.GroupLayout.Remove(key);
            }

            m_DataContainer.GroupLayout.Add(mergedGroupLayout.Name, mergedGroupLayout);

            Select();
        }

        /// <summary>
        /// Applies the configured template to the provided group layouts.
        /// </summary>
        /// <param name="groupLayouts">Layouts to update.</param>
        protected void ApplyTemplate(List<GroupLayout> groupLayouts)
        {
            AddressableAssetGroupTemplate template = m_Template != null ?
                m_Template : AddressableUtil.FindDefaultAddressableGroupTemplate();

            foreach (var groupLayout in groupLayouts)
            {
                groupLayout.TemplateName = template.name;
            }
        }

        /// <summary>
        /// Renames a group layout in the data container.
        /// </summary>
        /// <param name="groupLayout">Layout to rename.</param>
        /// <param name="newName">New name to apply.</param>
        protected void Rename(GroupLayout groupLayout, string newName)
        {
            if (groupLayout == null || string.IsNullOrEmpty(newName))
            {
                return;
            }


            var oldName = groupLayout.Name;

            if (!m_DataContainer.GroupLayout.ContainsKey(oldName))
            {
                Debug.LogError($"Cannot find group layout name = {oldName}");

                return;
            }


            if (oldName.Equals(newName))
            {
                return;
            }


            if (m_DataContainer.GroupLayout.ContainsKey(newName))
            {
                Debug.LogError($"{newName} is not available!");

                return;
            }


            m_DataContainer.GroupLayout.Remove(oldName);

            groupLayout.Name = newName;

            m_DataContainer.GroupLayout.Add(groupLayout.Name, groupLayout);
        }
        #endregion
    }
}
