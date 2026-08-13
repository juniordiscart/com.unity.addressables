using System.Collections.Generic;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Command queue that aggregates input assets from configured rules.
    /// </summary>
    internal class InputAssetsCommandQueue : CommandQueue
    {
        #region Fields
        private readonly DataContainer m_DataContainer;

        HashSet<string> m_IncludedAssets;
        #endregion

        #region Methods
        public InputAssetsCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;

            Title = nameof(InputAssetsCommandQueue);
        }

        public override void PreExecute()
        {
            m_DataContainer.InputAssets = new HashSet<string>();

            ClearQueue();

            foreach (var inputRule in m_DataContainer.Settings.InputRules)
            {
                var rule = inputRule;

                AddCommand(() => AddInputAssets(rule));
            }
        }

        public override void PostExecute()
        {
            SaveOutputReportToFile();

            m_IncludedAssets = null;
        }

        private void AddInputAssets(InputRule inputRule)
        {
            m_IncludedAssets = inputRule.GetIncludedAssets();

            m_DataContainer.InputAssets.UnionWith(m_IncludedAssets);
        }

        void SaveOutputReportToFile()
        {
            if (!m_DataContainer.Settings.ProcessReport.HasFlag(ProcessStepReport.InputAssets))
                return;

            var summary = $"(data.Count={m_IncludedAssets.Count})";
            var data = new List<string>(m_IncludedAssets);

            JsonReport.SaveJsonReport(GetType(), summary, data);
        }
        #endregion
    }
}
