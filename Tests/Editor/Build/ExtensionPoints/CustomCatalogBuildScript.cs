using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.CatalogBuilders;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;

namespace ThirdParty.AddressablesExtensions.Tests
{
    /// <summary>
    /// Third-party BuildScriptSchemaDriven subclass that wires in a custom ICatalogBuilder
    /// by overriding CreateCatalogBuilder on the build script (the canonical extension point).
    /// </summary>
    internal class CustomCatalogBuildScript : BuildScriptSchemaDriven
    {
        internal static int CreateCatalogBuilderCallCount;

        internal static void ClearCallRecord() => CreateCatalogBuilderCallCount = 0;

        protected override ICatalogBuilder CreateCatalogBuilder(AddressableAssetSettings settings)
        {
            CreateCatalogBuilderCallCount++;
            return new MyTestCatalogBuilder();
        }
    }
}
