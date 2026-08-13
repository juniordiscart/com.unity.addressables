using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.AddressableAssets.GUI
{
    [Serializable]
    public class AddressableAssetEntryTreeViewState : TreeViewState
    {
        [SerializeField]
        public float[] columnWidths;

        [SerializeField]
        [Obsolete("Use sortOrderList instead.")]
        public String[] sortOrder;

        [SerializeField]
        public List<string> sortOrderList = new List<string>();
    }
}
