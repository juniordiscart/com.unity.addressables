#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class AddressablesProfilerViewController : ProfilerModuleViewController
    {
        private ProfilerWindow m_ProfilerWindow;

        private static Dictionary<ProfilerWindow, AddressablesProfilerDetailsView> m_Views = new Dictionary<ProfilerWindow, AddressablesProfilerDetailsView>();

        private static readonly BuildLayoutsManager m_LayoutsManager = new BuildLayoutsManager();
        public static BuildLayoutsManager LayoutsManager => m_LayoutsManager;


        public AddressablesProfilerViewController(ProfilerWindow profilerWindow) : base(profilerWindow)
        {
            m_ProfilerWindow = profilerWindow;
        }

        protected override VisualElement CreateView()
        {
            m_LayoutsManager.LoadReports();

            AddressablesProfilerDetailsView view;
            if (!m_Views.TryGetValue(m_ProfilerWindow, out view))
            {
                view = new AddressablesProfilerDetailsView(m_ProfilerWindow);
                view.CreateView();
                m_Views[m_ProfilerWindow] = view;
            }
            else
            {
                view.OnReinitialise();
            }

            AddressableAnalytics.ReportUsageEvent(AddressableAnalytics.UsageEventType.ProfileModuleViewCreated);
            return view.RootVisualElement;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            base.Dispose(disposing);
            if (m_Views.TryGetValue(m_ProfilerWindow, out var view))
                view.Dispose();
        }
    }
}

#endif
