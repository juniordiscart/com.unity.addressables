using UnityEngine;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    using Debug = UnityEngine.Debug;
    /// <summary>
    /// Build scripts used for player builds and running with bundles in the editor.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildScriptPacked.asset", menuName = "Addressables/Content Builders/Default Build Script")]
    public class BuildScriptPackedMode : BuildScriptSchemaDriven
    {
        /// <inheritdoc />
        public override string Name
        {
            get { return "Default Build Script"; }
        }

        /// <inheritdoc/>
        public override ISchemaBuilder[] CreateSchemaBuilders()
        {
            return new ISchemaBuilder[] {
                new BundledAssetSchemaBuilder(),
            };
        }

    }
}