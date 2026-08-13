using System.Collections.Generic;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Command queue that executes configured output rules.
    /// </summary>
    internal class OutputRuleCommandQueue : CommandQueue
    {
        #region Fields
        private readonly DataContainer m_DataContainer;
        #endregion

        #region Methods
        public OutputRuleCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;

            Title = nameof(OutputRuleCommandQueue);
        }

        public override void PreExecute()
        {
            ClearQueue();

            foreach (var outputRule in m_DataContainer.Settings.OutputRules)
            {
                var rule = outputRule;

                AddCommand(() => rule.Initialize(m_DataContainer));

                AddCommand(() => rule.Select());

                AddCommand(() => rule.Refine());

                AddCommand(() => rule.UnInit());
            }
        }

        public override void PostExecute()
        {
            SaveOutputReportToFile();
        }

        void SaveOutputReportToFile()
        {
            // Shares the GroupLayout report flag.
            if (!m_DataContainer.Settings.ProcessReport.HasFlag(ProcessStepReport.GroupLayout))
                return;

            var summary = $"(GroupLayout.Count after applying Output Rules = {m_DataContainer.GroupLayout.Count})";
            var data = new List<GroupLayout>(m_DataContainer.GroupLayout.Values);

            JsonReport.SaveJsonReport(GetType(), summary, data);
        }
        #endregion
    }
}
