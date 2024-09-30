#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using UnityEditor.AddressableAssets.GUIElements;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class MissingReport : VisualElementsWrapper
    {
        public MissingReport(VisualElement rootView) : base(rootView) { }

        public Image Icon => GetElement<Image>();
        public Label MissingBuildHashLabel => GetElement<Label>();
        public Button SearchForBuildReportButton => GetElement<Button>();


        private static VisualTreeAsset s_Template;
        private static VisualTreeAsset Template
        {
            get
            {
                if (s_Template == null)
                    s_Template = ProfilerTemplates.MissingReport;
                return s_Template;
            }
        }

        public static MissingReport Create()
        {
            VisualElement root = Template.Instantiate();
            return new MissingReport(root);
        }
    }
}
#endif
