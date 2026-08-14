using System;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.AddressableAssets.Build.BuildPipelineTasks
{
    /// <summary>
    /// The BuildTask used to extract write data from the build.
    /// </summary>
    public class ExtractDataTask : IBuildTask
    {
        /// <summary>
        /// The ExtractDataTask version.
        /// </summary>
        public int Version
        {
            get { return 1; }
        }

        /// <summary>
        /// Get the injected dependency data of the task.
        /// </summary>
        public IDependencyData DependencyData
        {
            get { return m_DependencyData; }
        }

        /// <summary>
        /// Get the injected write data of the task.
        /// </summary>
        public IBundleWriteData WriteData
        {
            get { return m_WriteData; }
        }

        /// <summary>
        /// Get the injected build cache of the task.
        /// </summary>
        public IBuildCache BuildCache
        {
            get { return m_BuildCache; }
        }

        /// <summary>
        /// The build context of the task.
        /// </summary>
        public IBuildContext BuildContext
        {
            get { return m_BuildContext; }
        }

#if ENABLE_CONTENT_DIRECTORIES
        /// <summary>
        /// Gets the context for generating and managing build reports.
        /// </summary>
        public IBuildReportContext BuildReportContext
        {
            get => m_BuildReport;
        }
#endif

#pragma warning disable 649
        [InjectContext(ContextUsage.In, true)]
        [BuildOverwriteProtected]
        IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.In, true)]
        [BuildOverwriteProtected]
        IBundleWriteData m_WriteData;

        [InjectContext(ContextUsage.In, true)]
        [BuildOverwriteProtected]
        IBuildCache m_BuildCache;

        [InjectContext(ContextUsage.In)]
        [BuildOverwriteProtected]
        internal IBuildContext m_BuildContext;

#if ENABLE_CONTENT_DIRECTORIES
        [InjectContext(ContextUsage.In, true)]
        [BuildOverwriteProtected]
        IBuildReportContext m_BuildReport;
#endif

#pragma warning restore 649

        /// <summary>
        /// Runs the ExtractDataTask.  The data for this task is all injected context so no operations are performed in the Run step.
        /// </summary>
        /// <returns>Success.</returns>
        public ReturnCode Run()
        {
            return ReturnCode.Success;
        }
    }
}
