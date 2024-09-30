#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using UnityEditor.AddressableAssets.GUIElements;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class InspectorPane : VisualElementsWrapper
    {
        public InspectorPane(VisualElement rootView) : base(rootView)
        {
        }

        public VisualElement MainContainer => GetElement<VisualElement>();

        public AssetLabel SelectedAsset => GetElement<AssetLabel>();

        public Foldout SelectionDetailsFoldout => GetElement<Foldout>();

        public Foldout HelpFoldout => GetElement<Foldout>();

        public Foldout ReferencesFoldout => GetElement<Foldout>();
        public Ribbon ReferencesTypeSelection => GetElement<Ribbon>();
        public RibbonButton ReferencesToButton => GetElement<RibbonButton>();
        public RibbonButton ReferencedByButton => GetElement<RibbonButton>();

        public TreeView ReferencesTree => GetElement<TreeView>();

        public Button SelectInEditor => GetElement<Button>();
        public Button SelectInGroups => GetElement<Button>();

        public Foldout PreviewFoldout => GetElement<Foldout>();
        public Image PreviewImage => GetElement<Image>();
    }
}

#endif
