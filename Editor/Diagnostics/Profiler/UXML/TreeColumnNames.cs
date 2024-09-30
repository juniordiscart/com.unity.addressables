#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal static class TreeColumnNames
    {
        public const string TreeColumnName = "aap-tree-col-name";
        public const string TreeColumnType = "aap-tree-col-assetType";
        public const string TreeColumnAddressedCount = "aap-tree-col-addressedCount";
        public const string TreeColumnStatus = "aap-tree-col-status";
        public const string TreeColumnPercentage = "aap-tree-col-percentage";
        public const string TreeColumnBundleSource = "aap-tree-col-bundleSource";
        public const string TreeColumnReferencedBy = "aap-tree-col-referencedBy";
        public const string TreeColumnReferencesTo = "aap-tree-col-referencesTo";
    }
}

#endif
