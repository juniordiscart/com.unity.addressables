using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Identifies the last processing step to execute.
    /// </summary>
    public enum LastProcessingStep
    {
        /// <summary>
        /// No processing steps are executed.
        /// </summary>
        None = 0,
        /// <summary>
        /// Stops after collecting input assets.
        /// </summary>
        InputAssets = 10,
        /// <summary>
        /// Stops after generating the dependency graph.
        /// </summary>
        GenerateDependencyGraph = 11,
        /// <summary>
        /// Stops after identifying subgraphs and their source relationships.
        /// </summary>
        GenerateSubGraphs = 12,
        /// <summary>
        /// Stops after generating group layouts.
        /// </summary>
        GenerateGroupLayout = 13,
        /// <summary>
        /// Stops after creating addressable groups.
        /// </summary>
        GenerateAddressableGroups = 14,
        /// <summary>
        /// Stops after cleanup operations that refine addressable output.
        /// </summary>
        Cleanup = 15,
        /// <summary>
        /// Executes all steps in the pipeline.
        /// </summary>
        All = 100
    }

    /// <summary>
    /// Flags controlling which processing steps emit reports.
    /// </summary>
    [Flags]
    public enum ProcessStepReport
    {
        /// <summary>
        /// Write a report for the input assets step.
        /// </summary>
        InputAssets = 1 << 0,
        /// <summary>
        /// Write a report for the dependency graph step.
        /// </summary>
        DependencyGraph = 1 << 1,
        /// <summary>
        /// Write a report for the subgraph step.
        /// </summary>
        SubGraphs = 1 << 2,
        /// <summary>
        /// Write a report for the group layout step.
        /// </summary>
        GroupLayout = 1 << 3,
        /// <summary>
        /// Write a report for the addressable groups step.
        /// </summary>
        AddressableGroups = 1 << 4,
        /// <summary>
        /// Write a report for cleanup operations.
        /// </summary>
        Cleanup = 1 << 5
    }

    /// <summary>
    /// Defines verbosity levels for logging output.
    /// </summary>
    public enum LogLevelID
    {
        /// <summary>
        /// Log only errors to reduce output during batch runs.
        /// </summary>
        OnlyErrors = 0,

        /// <summary>
        /// Log informational messages for normal pipeline monitoring.
        /// </summary>
        Info       = 1,

        /// <summary>
        /// Log developer-focused diagnostics.
        /// </summary>
        Developer = 2
    }

    /// <summary>
    /// ScriptableObject containing configuration for the AutoGroupGenerator workflows.
    /// </summary>
    /// <seealso cref="AutoGroupGeneratorWindow"/>
    /// <seealso cref="InputRule"/>
    /// <seealso cref="OutputRule"/>
    [CreateAssetMenu(menuName = Constants.ContextMenus.Root + "Settings")]
    public class AutoGroupGeneratorSettings : ScriptableObject
    {
        #region Fields
        /// <summary>
        /// Input rules used to select assets to process.
        /// </summary>
        [Header("Rules")]
        public List<InputRule> InputRules = new List<InputRule>();

        /// <summary>
        /// Output rules used to post-process generated layouts.
        /// </summary>
        public List<OutputRule> OutputRules = new List<OutputRule>();

        [Header("Cleanup")]
        [SerializeField]
        [Tooltip("Remove any unused addressable asset entries")]
        private bool m_RemoveUnnecessaryEntries = false;

        [SerializeField]
        private bool m_RemoveEmptyGroups = true;

        [SerializeField]
        private bool m_RemoveAddressableScenesFromBuildProfile = true;

        [SerializeField]
        private bool m_SortAddressableGroups = true;

        [Header("Process")]
        [SerializeField]
        private bool m_ScanForUnsupportedFiles = false;

        [SerializeField]
        private LastProcessingStep m_LastProcessingStep = LastProcessingStep.All;

        private bool m_SaveGraphOnDisk = false;

        [Header("Reports")]
        [SerializeField]
        ProcessStepReport m_ProcessReport = 0;

        [SerializeField]
        private LogLevelID m_LogLevel = LogLevelID.OnlyErrors;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the last processing step to execute.
        /// </summary>
        public LastProcessingStep LastProcessingStep => m_LastProcessingStep;

        /// <summary>
        /// Gets the configured logging verbosity.
        /// </summary>
        public LogLevelID LogLevel => m_LogLevel;

        /// <summary>
        /// Gets a value indicating whether to persist the dependency graph to disk.
        /// </summary>
        public bool SaveGraphOnDisk => m_SaveGraphOnDisk;

        /// <summary>
        /// Gets a value indicating whether to remove empty addressable groups.
        /// </summary>
        public bool RemoveEmptyGroups => m_RemoveEmptyGroups;

        /// <summary>
        /// Gets a value indicating whether to remove Addressable entries that are no longer needed.
        /// </summary>
        public bool RemoveUnnecessaryEntries => m_RemoveUnnecessaryEntries;

        /// <summary>
        /// Gets a value indicating whether to remove Addressable scenes from the build profile.
        /// </summary>
        public bool RemoveAddressableScenesFromBuildProfile => m_RemoveAddressableScenesFromBuildProfile;

        /// <summary>
        /// Gets a value indicating whether addressable groups should be sorted.
        /// </summary>
        public bool SortAddressableGroups => m_SortAddressableGroups;

        /// <summary>
        /// Gets a value indicating whether to scan assets for unsupported file types.
        /// </summary>
        public bool ScanForUnsupportedFiles => m_ScanForUnsupportedFiles;

        /// <summary>
        /// Gets the report flags for processing steps.
        /// </summary>
        public ProcessStepReport ProcessReport => m_ProcessReport;

        #endregion

        #region Methods
        /// <summary>
        /// Validates the current settings configuration.
        /// </summary>
        public void Validate()
        {
        }
        #endregion
    }
}
