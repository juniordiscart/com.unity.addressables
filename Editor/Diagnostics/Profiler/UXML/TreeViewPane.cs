#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using UnityEditor.AddressableAssets.GUIElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class TreeViewPane : VisualElementsWrapper
    {
        public TreeViewPane(VisualElement rootView) : base(rootView)
        {
        }

        public ToolbarMenu ViewMenu => GetElement<ToolbarMenu>();

        public ToolbarMenu SearchMenu => GetElement<ToolbarMenu>();
        public ToolbarSearchField SearchField => GetElement<ToolbarSearchField>();

        public MultiColumnTreeView TreeView => GetElement<MultiColumnTreeView>();
    }
}

#endif
