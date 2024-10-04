using Unity.Profiling;
using Unity.Profiling.Editor;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    [System.Serializable]
    [ProfilerModuleMetadata("Addressable Assets")]
    internal class AddressablesProfilerModule : ProfilerModule
    {
        private static readonly ProfilerCounterDescriptor[] Descriptors = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor("Asset Bundles", ProfilerCategory.Loading),
            new ProfilerCounterDescriptor("Assets", ProfilerCategory.Loading),
            new ProfilerCounterDescriptor("Scenes", ProfilerCategory.Loading),
            new ProfilerCounterDescriptor("Catalogs", ProfilerCategory.Loading)
        };

        private static readonly string[] AutoEnabledCategoryNames = new string[]
        {
            "Loading",
        };

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new AddressablesProfilerViewController(ProfilerWindow);
        }

        public AddressablesProfilerModule() : base(Descriptors, ProfilerModuleChartType.Line, AutoEnabledCategoryNames) {}
    }
}
