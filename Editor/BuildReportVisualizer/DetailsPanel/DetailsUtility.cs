#if UNITY_2022_2_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEngine;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal static class DetailsUtility
    {
        public static BuildLayout.ExplicitAsset GetAsset(object item)
        {
            if (item is IAddressablesBuildReportAsset)
                return (item as IAddressablesBuildReportAsset).ExplicitAsset;
            else if (item is BuildLayout.ExplicitAsset)
                return item as BuildLayout.ExplicitAsset;
            return null;
        }

        public static BuildLayout.DataFromOtherAsset GetOtherAssetData(object item)
        {
            if (item is IAddressablesBuildReportAsset)
                return (item as IAddressablesBuildReportAsset).DataFromOtherAsset;
            else if (item is BuildLayout.DataFromOtherAsset)
                return item as BuildLayout.DataFromOtherAsset;
            return null;
        }

        public static bool IsBundle(object item)
        {
            if (item is IAddressablesBuildReportBundle || item is BuildLayout.Bundle)
                return true;

            return false;
        }

        public static BuildLayout.Bundle GetBundle(object item)
        {
            BuildLayout.Bundle bundle = null;

            if (item is IAddressablesBuildReportBundle)
                bundle = (item as IAddressablesBuildReportBundle).Bundle;
            else if (item is BuildLayout.Bundle)
                bundle = item as BuildLayout.Bundle;

            return bundle;
        }
    }
}
#endif
