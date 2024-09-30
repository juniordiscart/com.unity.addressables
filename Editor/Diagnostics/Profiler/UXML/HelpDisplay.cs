#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using UnityEditor.AddressableAssets.GUIElements;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class HelpDisplay : VisualElementsWrapper
    {
        public HelpDisplay(VisualElement rootView) : base(rootView)
        {
            MainContainer.EnableInClassList("d_help-area", EditorGUIUtility.isProSkin);
            MainContainer.EnableInClassList("help-area", !EditorGUIUtility.isProSkin);
        }
        public VisualElement MainContainer => GetElement<VisualElement>();
        public Label HelpLabel => GetElement<Label>();

        public DocumentationButton DocumentationLink => GetElement<DocumentationButton>();


        private static VisualTreeAsset s_Template;
        private static VisualTreeAsset Template
        {
            get
            {
                if (s_Template == null)
                    s_Template = ProfilerTemplates.HelpDisplay;
                return s_Template;
            }
        }

        public static HelpDisplay Create()
        {
            VisualElement root = Template.Instantiate();
            return new HelpDisplay(root);
        }
    }
}
#endif
