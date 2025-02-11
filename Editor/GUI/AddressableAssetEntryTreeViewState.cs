using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Editor.GUI
{
    [Serializable]
    public class AddressableAssetEntryTreeViewState : TreeViewState
    {
        [SerializeField]
        public float[] columnWidths;

        [SerializeField]
        public string[] sortOrder;
    }
}
