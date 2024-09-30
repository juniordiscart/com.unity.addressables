using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace UnityEditor.AddressableAssets
{
    internal static class AddressablesPreferences
    {
#if UNITY_2021_2_OR_NEWER
        internal const string kBuildAddressablesWithPlayerBuildKey = "Addressables.BuildAddressablesWithPlayerBuild";
#endif
        private class GUIScope : UnityEngine.GUI.Scope
        {
            float m_LabelWidth;

            public GUIScope(float layoutMaxWidth)
            {
                m_LabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 250;
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical();
                GUILayout.Space(15);
            }

            public GUIScope() : this(500)
            {
            }

            protected override void CloseScope()
            {
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = m_LabelWidth;
            }
        }

        internal class Properties
        {
            public static readonly GUIContent buildSettings = EditorGUIUtility.TrTextContent("Build Settings");

            public static readonly GUIContent buildLayoutReport = EditorGUIUtility.TrTextContent("Debug Build Layout",
                $"A debug build layout file will be generated as part of the build process. The file will put written to {BuildLayoutGenerationTask.m_LayoutFilePath}");

            #if UNITY_2022_2_OR_NEWER
            internal static readonly GUIContent autoOpenAddressablesReport = EditorGUIUtility.TrTextContent("Open Addressables Report after build");
            #endif
            public static readonly GUIContent buildLayoutReportFileFormat = EditorGUIUtility.TrTextContent("File Format", $"The file format of the debug build layout file.");
#if UNITY_2021_2_OR_NEWER
            public static readonly GUIContent playerBuildSettings = EditorGUIUtility.TrTextContent("Player Build Settings");

            public static readonly GUIContent enableAddressableBuildPreprocessPlayer = EditorGUIUtility.TrTextContent("Build Addressables on build Player",
                $"If enabled, will perform a new Addressables build before building a Player. Addressable Asset Settings value can override the user global preferences.");
#endif

#if !ENABLE_ADDRESSABLE_PROFILER && UNITY_2021_2_OR_NEWER
            public static readonly GUIContent installProfilingCoreHelp = EditorGUIUtility.TrTextContent("In order to run Addressables profiler using the debug layout. Profiling.Core package is required for etc. Install now?",
                $"Profiling.Core is needed to transmit and display data from the runtime.");
            public static readonly GUIContent installProfilingCoreButton = EditorGUIUtility.TrTextContent("Install now",
                $"Enables Profiling.Core package in this project");
#endif
        }

        static AddressablesPreferences()
        {
        }

        [SettingsProvider]
        static SettingsProvider CreateAddressableSettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/Addressables", SettingsScope.User, SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Properties>());
            provider.guiHandler = sarchContext => OnGUI();
            return provider;
        }

        static void OnGUI()
        {
            using (new GUIScope())
            {
                DrawProperties();
            }
        }

        static void DrawProperties()
        {
            GUILayout.Label(Properties.buildSettings, EditorStyles.boldLabel);

            ProjectConfigData.GenerateBuildLayout = EditorGUILayout.Toggle(Properties.buildLayoutReport, ProjectConfigData.GenerateBuildLayout);
            if (ProjectConfigData.GenerateBuildLayout)
            {
                EditorGUI.indentLevel++;
                ProjectConfigData.ReportFileFormat buildLayoutReportFileFormat = ProjectConfigData.BuildLayoutReportFileFormat;
                int formatOldIndex = (int)buildLayoutReportFileFormat;
                int formatNewIndex = EditorGUILayout.Popup(Properties.buildLayoutReportFileFormat, formatOldIndex, new[] {"TXT and JSON", "JSON"});
                if (formatNewIndex != formatOldIndex)
                    ProjectConfigData.BuildLayoutReportFileFormat = (ProjectConfigData.ReportFileFormat)formatNewIndex;
                EditorGUI.indentLevel--;

#if UNITY_2022_2_OR_NEWER
                EditorGUI.indentLevel++;
                ProjectConfigData.AutoOpenAddressablesReport = EditorGUILayout.Toggle(Properties.autoOpenAddressablesReport, ProjectConfigData.AutoOpenAddressablesReport);
                EditorGUI.indentLevel--;
#endif
            }

#if !ENABLE_ADDRESSABLE_PROFILER && UNITY_2021_2_OR_NEWER
            GUILayout.Space(15);
            EditorGUILayout.HelpBox(Properties.installProfilingCoreHelp);
            var rect = EditorGUILayout.GetControlRect();
            if (UnityEngine.GUI.Button(rect, Properties.installProfilingCoreButton))
            {
                UnityEditor.PackageManager.Client.Add("com.unity.profiling.core@1.0.2");
            }
#endif

            GUILayout.Space(15);

#if UNITY_2021_2_OR_NEWER
            bool buildWithPlayerValue = EditorPrefs.GetBool(kBuildAddressablesWithPlayerBuildKey, true);

            GUILayout.Label(Properties.playerBuildSettings, EditorStyles.boldLabel);
            int index = buildWithPlayerValue ? 0 : 1;
            int val = EditorGUILayout.Popup(Properties.enableAddressableBuildPreprocessPlayer, index,
                new[] {"Build Addressables on Player Build", "Do Not Build Addressables on Player Build"});
            if (val != index)
            {
                bool newValue = val == 0 ? true : false;
                EditorPrefs.SetBool(kBuildAddressablesWithPlayerBuildKey, newValue);
                buildWithPlayerValue = newValue;
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    if (settings.BuildAddressablesWithPlayerBuild == AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer &&
                        buildWithPlayerValue == false)
                    {
                        EditorGUILayout.TextField(" ", "Enabled in AddressableAssetSettings (priority)");
                    }
                    else if (settings.BuildAddressablesWithPlayerBuild == AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer &&
                             buildWithPlayerValue)
                    {
                        EditorGUILayout.TextField(" ", "Disabled in AddressableAssetSettings (priority)");
                    }
                }
            }
#endif
        }
    }
}
