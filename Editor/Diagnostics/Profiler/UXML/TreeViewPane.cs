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
