using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Editor window that orchestrates AutoGroupGenerator processing workflows.
    /// </summary>
    internal class AutoGroupGeneratorWindow : EditorWindow, IHasCustomMenu
    {
        #region Constants
        private const string k_BoxStyleName = "Box";

        private const float k_Space = 5f;

        const string k_CreateSettingsButtonLabel = "Create Settings File";

        private const string k_QuickButtonLabel = "Generate Addressable Groups Automatically";

        private const int k_QuickButtonWidth = 300;

        private const int k_QuickButtonHeight = 30;
        #endregion

        #region Static Methods
        [MenuItem(Constants.Menus.AutoGroupGeneratorMenuPath, priority = Constants.Menus.AutoGroupGeneratorMenuPriority)]
        public static void ShowWindow()
        {
            var window = GetWindow<AutoGroupGeneratorWindow>("AutoGroupGenerator");

            window.minSize = new Vector2(400, 200);
        }

        private static bool ToolSettingsExists()
        {
            string[] allSettings = AssetDatabaseUtil.FindAssetGuidsForType<AutoGroupGeneratorSettings>();

            return allSettings.Length > 0;
        }
        #endregion

        #region Fields
        private EditorPersistentValue<string> m_SettingsAssetPath = new (null, "EPK_AAG_SettingsPath");

        private AutoGroupGeneratorSettings m_Settings;

        private DataContainer m_DataContainer;

        private bool m_IsProcessing = false;

        private bool m_IsCancelled = false;

        private double m_LastTime;
        #endregion

        #region Methods
        private void OnEnable()
        {
            var assetPath = m_SettingsAssetPath.Value;

            if (!string.IsNullOrEmpty(assetPath))
            {
                m_Settings = AssetDatabase.LoadAssetAtPath<AutoGroupGeneratorSettings>(assetPath);
            }
        }

        private void OnDisable()
        {
            if (m_Settings != null)
            {

                string assetPath = AssetDatabase.GetAssetPath(m_Settings);

                m_SettingsAssetPath.Value = assetPath;
            }
            else
            {

                m_SettingsAssetPath.ClearPersistentData();
            }
        }

        private void LoadSettingsFileInEditor()
        {
            m_Settings = AssetDatabase.LoadAssetAtPath<AutoGroupGeneratorSettings>(m_DataContainer.SettingsFilePath);
        }

        private void OnGUI()
        {
            bool settingFileExists = ToolSettingsExists();

            GUILayoutOption buttonMinWidth = GUILayout.MinWidth(k_QuickButtonWidth);

            GUILayoutOption buttonHeight = GUILayout.Height(k_QuickButtonHeight);

            GUILayout.BeginVertical(k_BoxStyleName);

            GUILayout.Space(k_Space);

            if (settingFileExists)
            {
                Action drawSettings = () =>
                {
                    m_Settings = (AutoGroupGeneratorSettings)EditorGUILayout.ObjectField(m_Settings, typeof(AutoGroupGeneratorSettings), false, buttonMinWidth);
                };

                DrawCentered(drawSettings, k_QuickButtonWidth);
            }

            GUILayout.Space(k_Space);

            Action drawQuickButton = () =>
            {
                if (!settingFileExists)
                {
                    if (GUILayout.Button(k_CreateSettingsButtonLabel, buttonMinWidth, buttonHeight))
                    {
                        RunBlockingLoop(InitializeSettingsButtonCommands());
                    }
                }
                else
                {

                    if (GUILayout.Button(k_QuickButtonLabel, buttonMinWidth, buttonHeight))
                    {
                        RunAutoGroupGeneratorTool(false);
                    }
                }

            };

            DrawCentered(drawQuickButton, k_QuickButtonWidth);

            GUILayout.Space(k_Space);

            GUILayout.EndVertical();
        }

        public void RunAutoGroupGeneratorTool(bool runInBackground)
        {
            if (m_IsProcessing)
                return;

            RunBlockingLoop(InitializeCommands());
        }

        private void InitializeDataContainer()
        {
            m_DataContainer = new DataContainer
            {
                Settings = m_Settings,
                SettingsFilePath = AssetDatabase.GetAssetPath(m_Settings),
            };

            m_DataContainer.Logger = new Logger(m_DataContainer);
        }

        private List<CommandQueue> InitializeSettingsButtonCommands()
        {
            InitializeDataContainer();

            var commandQueues = new List<CommandQueue>
            {
                new SettingsFilesCommandQueue(m_DataContainer),
            };

            var loadFileInEditor = new CommandQueue();
            loadFileInEditor.AddCommand(LoadSettingsFileInEditor);

            loadFileInEditor.AddCommand(() => EditorGUIUtility.PingObject(m_Settings));
            commandQueues.Add(loadFileInEditor);

            return commandQueues;
        }

        private List<CommandQueue> InitializeCommands()
        {
            InitializeDataContainer();

            var commandQueues = new List<CommandQueue>
            {
                new SettingsFilesCommandQueue(m_DataContainer),
                new CommandQueue(LoadSettingsFileInEditor, nameof(LoadSettingsFileInEditor)),
            };

            if (m_Settings.LastProcessingStep >= LastProcessingStep.InputAssets)
                commandQueues.Add(new InputAssetsCommandQueue(m_DataContainer));

            if (m_Settings.LastProcessingStep >= LastProcessingStep.GenerateDependencyGraph)
                commandQueues.Add(new DependencyGraphCommandQueue(m_DataContainer));

            if (m_Settings.LastProcessingStep >= LastProcessingStep.GenerateSubGraphs)
                commandQueues.Add(new SubgraphCommandQueue(m_DataContainer));

            if (m_Settings.LastProcessingStep >= LastProcessingStep.GenerateGroupLayout)
            {
                commandQueues.Add(new GroupLayoutCommandQueue(m_DataContainer));
                commandQueues.Add(new OutputRuleCommandQueue(m_DataContainer));
            }

            if (m_Settings.LastProcessingStep >= LastProcessingStep.GenerateAddressableGroups)
                commandQueues.Add(new AddressableGroupCommandQueue(m_DataContainer));

            if (m_Settings.LastProcessingStep >= LastProcessingStep.Cleanup)
                commandQueues.Add(new AddressableCleanupCommandQueue(m_DataContainer));

            return commandQueues;
        }

        private IEnumerator RunAsyncLoop(List<CommandQueue> commandQueues)
        {
            m_IsProcessing = true;

            m_IsCancelled = false;

            double lastUpdateTime = 0;

            const double editorUpdateInterval = 0.25;

            for (int i = 0; i < commandQueues.Count; i++)
            {
                m_LastTime = EditorApplication.timeSinceStartup;

                var currentQueue = commandQueues[i];

                currentQueue.PreExecute();

                float progressStart = (float)i / commandQueues.Count;

                float progressEnd = (float)(i + 1) / commandQueues.Count;

                int progress = 0;

                int totalCount = currentQueue.RemainingCommandCount;

                var progressBarTitle = currentQueue.Title;

                var progressBarInfo = "Processing ...";

                var progressId = Progress.Start(progressBarTitle);

                Progress.RegisterCancelCallback(progressId, CancelCallback);

                while (currentQueue.RemainingCommandCount > 0)
                {
                    var info = string.Empty;

                    bool error = false;

                    Exception exception = null;

                    try
                    {
                        info = currentQueue.ExecuteNextCommand();
                    }
                    catch (Exception e)
                    {
                        error = true;

                        exception = e;
                    }

                    if (m_IsCancelled)
                    {
                        m_DataContainer.Logger.LogInfo(this, $"Cancelled!");

                        Progress.UnregisterCancelCallback(progressId);

                        m_IsProcessing = false;

                        StopAssetEditingIfNeeded();

                        Progress.Remove(progressId);

                        yield break;
                    }

                    if (error)
                    {
                        Debug.LogException(exception);

                        m_IsProcessing = false;

                        StopAssetEditingIfNeeded();

                        Progress.Remove(progressId);

                        yield break;
                    }

                    progress++;

                    progressBarInfo = string.IsNullOrEmpty(info) ? progressBarInfo : info;

                    var currentTime = EditorApplication.timeSinceStartup;

                    if (currentTime - lastUpdateTime > editorUpdateInterval)
                    {
                        lastUpdateTime = currentTime;

                        var percentage = progressStart + ((float)progress / totalCount) * (progressEnd - progressStart);

                        Progress.Report(progressId, percentage, progressBarInfo);

                        yield return null;
                    }
                }

                currentQueue.PostExecute();

                m_DataContainer.Logger.LogDev(this,
                    $"Time Taken for {currentQueue.Title} = {Math.Round(EditorApplication.timeSinceStartup - m_LastTime)}s");

                Progress.UnregisterCancelCallback(progressId);

                Progress.Remove(progressId);
            }

            m_IsProcessing = false;

            bool CancelCallback()
            {
                m_IsCancelled = true;

                return true;
            }
        }

        private void RunBlockingLoop(List<CommandQueue> commandQueues)
        {
            m_IsProcessing = true;

            m_IsCancelled = false;

            try
            {
                for (int i = 0; i < commandQueues.Count; i++)
                {
                    m_LastTime = EditorApplication.timeSinceStartup;

                    var currentQueue = commandQueues[i];

                    currentQueue.PreExecute();

                    float progressStart = (float)i / commandQueues.Count;

                    float progressEnd = (float)(i + 1) / commandQueues.Count;

                    int progress = 0;

                    int totalCount = currentQueue.RemainingCommandCount;

                    var progressBarTitle = currentQueue.Title;

                    var progressBarInfo = "Processing ...";

                    while (currentQueue.RemainingCommandCount > 0)
                    {
                        var info = currentQueue.ExecuteNextCommand();

                        progress++;

                        progressBarInfo = string.IsNullOrEmpty(info) ? progressBarInfo : info;

                        if (EditorUtility.DisplayCancelableProgressBar(progressBarTitle, progressBarInfo,
                                progressStart + ((float)progress / totalCount) * (progressEnd - progressStart)))
                        {
                            m_IsCancelled = true;

                            break;
                        }
                    }

                    if (m_IsCancelled)
                    {
                        m_DataContainer.Logger.LogInfo(this, $"Cancelled!");

                        break;
                    }

                    currentQueue.PostExecute();

                    m_DataContainer.Logger.LogDev(this,
                        $"Time Taken for {currentQueue.Title} = {Math.Round(EditorApplication.timeSinceStartup - m_LastTime)}s");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();

                m_IsProcessing = false;

                StopAssetEditingIfNeeded();
            }
        }

        private void DrawCentered(Action drawAction, float elementWidth)
        {
            float padding = (position.width - elementWidth) / 2;

            GUILayout.BeginHorizontal();

            GUILayout.Space(padding);

            drawAction.Invoke();

            GUILayout.Space(padding);

            GUILayout.EndHorizontal();
        }

        private void StopAssetEditingIfNeeded()
        {
            if (m_DataContainer.AssetEditingInProgress)
            {
                AssetDatabase.StopAssetEditing();

                m_DataContainer.AssetEditingInProgress = false;
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Locate Saved Files"), false, EditorUtil.LocatePersistentDataFolder);
        }

        #endregion
    }
}
