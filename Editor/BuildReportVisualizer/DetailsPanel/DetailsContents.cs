#if UNITY_2022_2_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal class DetailsContents
    {
        public List<DetailsListItem> DrillableItems;
        public string Title;
        public string AssetPath;

        public DetailsContents(string title, string assetPath)
        {
            Title = title;
            AssetPath = assetPath;
            DrillableItems = new List<DetailsListItem>();
        }

        public DetailsContents(string title, string assetPath, List<DetailsListItem> items)
        {
            Title = title;
            AssetPath = assetPath;
            DrillableItems = items;
        }
    }
}
#endif
