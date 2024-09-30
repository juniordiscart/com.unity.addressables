#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2021_2_OR_NEWER
#if !UNITY_2022_2_OR_NEWER

using Unity.Profiling.Editor;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class AddressablesProfilerUnsupported : ProfilerModuleViewController
    {
        private const string k_UxmlResourcePath = "Packages/com.unity.addressables/Editor/Diagnostics/Profiler/UXML";
        private static string UnsupportedPath => k_UxmlResourcePath + "/Unsupported.uxml";

        private readonly ProfilerWindow m_ProfilerWindow;
        private VisualElement m_MainView;

        public AddressablesProfilerUnsupported(ProfilerWindow profilerWindow) : base(profilerWindow)
        {
            m_ProfilerWindow = profilerWindow;
        }

        protected override VisualElement CreateView()
        {
            VisualElement root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UnsupportedPath).Instantiate();
            return root;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            m_ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            base.Dispose(disposing);
        }

        void OnSelectedFrameIndexChanged(long selectedFrameIndex)
        {
            ReloadData(selectedFrameIndex);
        }

        void ReloadData(long selectedFrameIndex)
        {
        }
    }
}
#endif
#endif
