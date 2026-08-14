using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UnityEditor.AddressableAssets
{
    internal static class AddressablesPreferences
    {
        internal const string kBuildAddressablesWithPlayerBuildKey = "Addressables.BuildAddressablesWithPlayerBuild";

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
            public static readonly GUIContent playmodeSettings = EditorGUIUtility.TrTextContent("Playmode Settings");
            public static readonly GUIContent editorUsageWarning = EditorGUIUtility.TrTextContent("Warn on Editor usage",
                "Logs a warning when entering playmode if Addressables have been used outside playmode.");

            public static readonly GUIContent buildSettings = EditorGUIUtility.TrTextContent("Build Settings");

            public static readonly GUIContent buildLayoutReport = EditorGUIUtility.TrTextContent("Debug Build Layout",
                $"A debug build layout file will be generated as part of the build process. The file will put written to {BuildLayoutGenerationTask.m_LayoutFilePath}");

            internal static readonly GUIContent autoOpenAddressablesReport = EditorGUIUtility.TrTextContent("Open Addressables Report after build");

            public static readonly GUIContent buildLayoutReportFileFormat = EditorGUIUtility.TrTextContent("File Format", $"The file format of the debug build layout file.");

            public static readonly GUIContent playerBuildSettings = EditorGUIUtility.TrTextContent("Player Build Settings");

            public static readonly GUIContent enableAddressableBuildPreprocessPlayer = EditorGUIUtility.TrTextContent("Build Addressables on build Player",
                $"If enabled, will perform a new Addressables build before building a Player. Addressable Asset Settings value can override the user global preferences.");
        }

        static AddressablesPreferences()
        {
        }

        [SettingsProvider]
        static SettingsProvider CreateAddressableSettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/Addressables", SettingsScope.User, SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Properties>());
            provider.guiHandler = _ => OnGUI();
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
            GUILayout.Label(Properties.playmodeSettings, EditorStyles.boldLabel);

            var warnOnEditorUsage = Addressables.WarnOnAddressablesUsageOutsidePlaymode;
            var warnOnEditorUsageChange = EditorGUILayout.Toggle(Properties.editorUsageWarning, warnOnEditorUsage);
            if (warnOnEditorUsageChange != warnOnEditorUsage)
            {
                Addressables.WarnOnAddressablesUsageOutsidePlaymode = warnOnEditorUsageChange;
            }

            GUILayout.Space(15);

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

                ProjectConfigData.AutoOpenAddressablesReport = EditorGUILayout.Toggle(Properties.autoOpenAddressablesReport, ProjectConfigData.AutoOpenAddressablesReport);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(15);

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
        }
    }
}
