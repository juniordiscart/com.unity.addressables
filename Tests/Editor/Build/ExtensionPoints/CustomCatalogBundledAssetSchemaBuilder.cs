using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;

namespace ThirdParty.AddressablesExtensions.Tests
{
    /// <summary>
    /// Third-party BundledAssetSchemaBuilder subclass kept for backward compat.
    /// CreateCatalogBuilder now lives on the build script; this class has no overrides.
    /// </summary>
    internal class CustomCatalogBundledAssetSchemaBuilder : BundledAssetSchemaBuilder
    {
        internal static void ClearCallRecord() { }
    }
}
