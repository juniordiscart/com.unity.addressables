#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal static class ProfilerTemplates
    {
        private const string k_UxmlResourcePath = "Packages/com.unity.addressables/Editor/Diagnostics/Profiler/UXML";
        private static string MissingReportPath => k_UxmlResourcePath + "/MissingReport.uxml";
        public static VisualTreeAsset MissingReport => GetTemplate(MissingReportPath);
        private static string TreeViewPanePath => k_UxmlResourcePath + "/TreeViewPane.uxml";
        public static VisualTreeAsset TreeViewPane => GetTemplate(TreeViewPanePath);
        private static string DetailsPanePath => k_UxmlResourcePath + "/InspectorPane.uxml";
        public static VisualTreeAsset DetailsPane => GetTemplate(DetailsPanePath);
        private static string HelpDisplayPath => k_UxmlResourcePath + "/HelpDisplay.uxml";
        public static VisualTreeAsset HelpDisplay => GetTemplate(HelpDisplayPath);


        private static Dictionary<string, VisualTreeAsset> m_VisualTemplatesMap = new Dictionary<string, VisualTreeAsset>();

        private static VisualTreeAsset GetTemplate(string templateName)
        {
            VisualTreeAsset template;
            if (m_VisualTemplatesMap.TryGetValue(templateName, out template))
                return template;

            template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(templateName);
            if (template != null)
            {
                m_VisualTemplatesMap.Add(templateName, template);
                return template;
            }

            StringBuilder error = new StringBuilder($"Could not find template of path {templateName}.");
            Debug.LogException(new ArgumentOutOfRangeException(error.ToString()));
            return null;
        }
    }
}

#endif
