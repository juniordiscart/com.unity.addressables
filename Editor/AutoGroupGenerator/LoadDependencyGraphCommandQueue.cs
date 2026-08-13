using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Command queue that loads a serialized dependency graph from disk.
    /// </summary>
    internal class LoadDependencyGraphCommandQueue : CommandQueue
    {
        #region Fields
        private readonly DataContainer m_DataContainer;
        #endregion

        #region Methods
        public LoadDependencyGraphCommandQueue(DataContainer dataContainer)
        {
            m_DataContainer = dataContainer;
            Title = nameof(DependencyGraphCommandQueue);
        }

        public override void PreExecute()
        {
            ClearQueue();

            m_DataContainer.DependencyGraph = new DependencyGraph();

            AddCommand(LoadDependencyGraph, "Load DependencyGraph");
        }

        private void LoadDependencyGraph()
        {
            var stringData = FileUtils.LoadFromFile(Constants.FilePaths.DependencyGraphFilePath);

            var serializedData = JsonUtility.FromJson<DependencyGraph.SerializedData>(stringData);

            m_DataContainer.DependencyGraph = DependencyGraph.Deserialize(serializedData);
        }
        #endregion
    }
}
