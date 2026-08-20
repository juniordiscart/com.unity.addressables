using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders
{
    public partial class BundledAssetSchemaBuilder
    {
        public BuildContext BuildContext => m_BuildContext;
        public IBuildLogger Logger => m_Logger;
        public FileRegistry FileRegistry => m_FileRegistry;
        public BuildTarget BuildTarget => m_BuildTarget;
        public BuildTargetGroup BuildTargetGroup => m_BuildTargetGroup;
        public string RuntimeCatalogFilename => m_RuntimeCatalogFilename;
        public string PlayerVersion => m_PlayerVersion;
        public AddressablesContentState PreviousContentState => m_PreviousContentState;
    }
}

