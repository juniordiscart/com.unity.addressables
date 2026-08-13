using UnityEditor.IMGUI.Controls;

namespace UnityEditor.AddressableAssets.GUI.Adapters
{
    /// <summary>
    /// Adapter for Unity's <see cref="TreeViewState"/> (or <see cref="TreeViewState{T}"/> in newer versions),
    /// used to maintain the selection, expansion, and scroll state of a tree view in the Addressables GUI.
    /// </summary>
    public class TreeViewStateAdapter :
#if UNITY_6000_2_OR_NEWER
        TreeViewState<int>
#else
        TreeViewState
#endif
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TreeViewStateAdapter"/> with default state.
        /// </summary>
        public TreeViewStateAdapter() : base()
        {
        }
    }
}
