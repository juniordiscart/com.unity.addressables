using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace ThirdParty.AddressablesExtensions.Tests
{
    internal class MyTestSchemaBuilder : ISchemaBuilder
    {
        internal static readonly HashSet<string> InvokedHooks = new HashSet<string>();

        internal static void ClearInvocationRecord() => InvokedHooks.Clear();

        static void Record([CallerMemberName] string name = null)
        {
            if (!string.IsNullOrEmpty(name))
                InvokedHooks.Add(name);
        }

        public string Name => "My Test Schema Builder";

        public bool CanBuildSchema(AddressableAssetGroupSchema schema) => schema is MyTestSchema;



        public void Init(AddressableAssetsBuildContext aaContext, AddressablesDataBuilderInput builderInput, BuildContext buildContext, IDataBuilder dataBuilder)
        {
            Record();
        }

        public string ProcessGroupSchema(AddressableAssetsBuildContext aaContext, AddressableAssetGroupSchema schema)
        {
            Record();
            return string.Empty;
        }

        public void Build(AddressableAssetsBuildContext aaContext, AddressablesPlayerBuildResult addrResult)
        {
            Record();
        }

        public void GenerateTypeStrippingInfo(AddressableAssetsBuildContext aaContext, ContentCatalogData contentCatalog)
        {
            Record();
        }

        public Dictionary<string, List<ContentCatalogDataEntry>> GenerateCatalogLocations(AddressableAssetsBuildContext aaContext, AddressablesPlayerBuildResult addrResult)
        {
            Record();
            // Return one entry so GenerateTypeStrippingInfo and GenerateContentUpdate are also called
            return new Dictionary<string, List<ContentCatalogDataEntry>>
            {
                { "thirdparty-schema-test", new List<ContentCatalogDataEntry>() }
            };
        }

        public void GenerateContentUpdate(AddressableAssetsBuildContext aaContext, AddressablesPlayerBuildResult addrResult)
        {
            Record();
        }
    }
}
