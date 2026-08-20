using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;

namespace ThirdParty.AddressablesExtensions.Tests
{
    /// <summary>
    /// Third-party BuildScriptSchemaDriven subclass that adds a custom ISchemaBuilder
    /// by overriding CreateSchemaBuilders.
    /// </summary>
    internal class CustomSchemaBuildScript : BuildScriptSchemaDriven
    {
        public override ISchemaBuilder[] CreateSchemaBuilders()
        {
            return new ISchemaBuilder[]
            {
                new BundledAssetSchemaBuilder(),
                new MyTestSchemaBuilder(),
            };
        }
    }
}
