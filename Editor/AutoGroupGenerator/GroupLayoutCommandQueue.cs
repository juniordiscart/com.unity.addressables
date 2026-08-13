using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Command queue that creates group layout definitions from subgraphs.
    /// </summary>
    internal class GroupLayoutCommandQueue : CommandQueue
    {
        #region Static Methods
        public static string GetDefaultGroupName(Subgraph subgraph)
        {
            return subgraph.HashOfSources.ToString();
        }
        #endregion

        #region Fields
        private readonly DataContainer m_DataContainer;

        private AddressableAssetGroupTemplate m_DefaultTemplate;
        #endregion

        #region Methods
        public GroupLayoutCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;

            Title = nameof(GroupLayoutCommandQueue);
        }

        public override void PreExecute()
        {
            m_DataContainer.GroupLayout = new Dictionary<string, GroupLayout>();

            m_DefaultTemplate = AddressableUtil.FindDefaultAddressableGroupTemplate();

            ClearQueue();

            foreach (var pair in m_DataContainer.Subgraphs)
            {
                var hash = pair.Key;
                var subgraph = pair.Value;

                AddCommand(() => CreateGroupLayout(subgraph), hash.ToString());
            }
        }

        public override void PostExecute()
        {
            SaveOutputReportToFile();
        }

        private void CreateGroupLayout(Subgraph subgraph)
        {
            var groupLayoutInfo = new GroupLayout
            {
                Nodes = subgraph.Nodes,
                Sources = subgraph.Sources,
                HashOfSources = subgraph.HashOfSources,
                Name = GetDefaultGroupName(subgraph),
                TemplateName = m_DefaultTemplate.Name,
            };

            if (groupLayoutInfo.Nodes.Count == 0)
            {
                throw new Exception($"group node count == 0!");
            }


            m_DataContainer.GroupLayout.Add(groupLayoutInfo.Name, groupLayoutInfo);
        }

        void SaveOutputReportToFile()
        {
            if (!m_DataContainer.Settings.ProcessReport.HasFlag(ProcessStepReport.GroupLayout))
                return;

            var summary = $"(GroupLayout.Count = {m_DataContainer.GroupLayout.Count})";
            var data = new List<GroupLayout>(m_DataContainer.GroupLayout.Values);

            JsonReport.SaveJsonReport(GetType(), summary, data);
        }
        #endregion
    }
}
