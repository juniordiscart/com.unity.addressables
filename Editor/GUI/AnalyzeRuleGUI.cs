using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UnityEditor.AddressableAssets.GUI
{
    [Serializable]
    class AnalyzeRuleGUI
    {
        [SerializeField]
        private TreeViewState m_TreeState;

        private AssetSettingsAnalyzeTreeView m_Tree;

        private const float k_ButtonHeight = 20f;

        private GUIContent m_AnalyzeSelectedRulesGUIContent = new GUIContent("Analyze Selected Rules", "Collect information about Addressables Groups based on the selected Rules");
        private GUIContent m_ClearRulesGUIContent = new GUIContent("Clear", "Clear information collected for the Analyze Rules");
        private GUIContent m_ClearSelectedRulesGUIContent = new GUIContent("Selection", "Clear information collected for the selected Rules");
        private GUIContent m_ClearAllRulesGUIContent = new GUIContent("All", "Clear information collected for the all Rules");
        private GUIContent m_FixSelectedRulesGUIContent = new GUIContent("Fix Selected Rules", "Make changes to Addressables Groups with errors found by the selected Fixable Rules");

        private GUIContent m_ExportJsonGUIContent = new GUIContent("Export Results", "Export a json file with the analyze results for all rules");
        private GUIContent m_ImportJsonGUIContent = new GUIContent("Import Results", "Import a json file with the analyze results for all rules, this will overwrite any existing results");

        internal void OnGUI(Rect rect)
        {
            if (m_Tree == null)
            {
                if (m_TreeState == null)
                    m_TreeState = new TreeViewState();

                m_Tree = new AssetSettingsAnalyzeTreeView(m_TreeState);
                m_Tree.Reload();
            }

            var treeRect = new Rect(rect.xMin, rect.yMin + k_ButtonHeight, rect.width, rect.height - k_ButtonHeight);
            m_Tree.OnGUI(treeRect);

            var buttonRect = new Rect(rect.xMin, rect.yMin, rect.width, rect.height);
            buttonRect.height = k_ButtonHeight;

            GUILayout.BeginArea(buttonRect);
            EditorGUI.BeginDisabledGroup(!m_Tree.SelectionContainsRuleContainer);

            var runRect = buttonRect;
            float activeWidth = 170;
            runRect.width = activeWidth;
            buttonRect.x += activeWidth;
            buttonRect.width -= activeWidth;
            if (UnityEngine.GUI.Button(runRect, m_AnalyzeSelectedRulesGUIContent, EditorStyles.toolbarButton))
            {
                EditorApplication.delayCall += () => m_Tree.RunAllSelectedRules();
            }

            var fixRect = buttonRect;
            activeWidth = 120;
            fixRect.width = activeWidth;
            buttonRect.x += activeWidth;
            buttonRect.width -= activeWidth;
            if (UnityEngine.GUI.Button(fixRect, m_FixSelectedRulesGUIContent, EditorStyles.toolbarButton))
            {
                EditorApplication.delayCall += () => m_Tree.FixAllSelectedRules();
            }

            EditorGUI.EndDisabledGroup();

            var clearRect = buttonRect;
            activeWidth = 80;
            clearRect.width = activeWidth;
            buttonRect.x += activeWidth;
            buttonRect.width -= activeWidth;
            if (EditorGUI.DropdownButton(clearRect, m_ClearRulesGUIContent, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                var menu = new GenericMenu();
                if (!m_Tree.SelectionContainsRuleContainer)
                    menu.AddDisabledItem(m_ClearSelectedRulesGUIContent, false);
                else
                    menu.AddItem(m_ClearSelectedRulesGUIContent, false, () => EditorApplication.delayCall += () => m_Tree.ClearAllSelectedRules());
                menu.AddItem(m_ClearAllRulesGUIContent, false, () => EditorApplication.delayCall += () => m_Tree.ClearAll());
                menu.DropDown(clearRect);
            }

            GUIStyle m_ToolbarButtonStyle = "RL FooterButton";
            GUIContent m_ManageLabelsButtonContent = EditorGUIUtility.TrIconContent("_Popup@2x", "Import/Export Analysis Results");
            Rect plusRect = buttonRect;
            plusRect.height = k_ButtonHeight;
            plusRect.width = plusRect.height;
            plusRect.x = (buttonRect.width - plusRect.width) + buttonRect.x;
            if (plusRect.x < buttonRect.x)
                plusRect.x = buttonRect.x;
            plusRect.y += 2;
            if (UnityEngine.GUI.Button(plusRect, m_ManageLabelsButtonContent, m_ToolbarButtonStyle))
            {
                var menu = new GenericMenu();
                menu.AddItem(m_ExportJsonGUIContent, false, () => EditorApplication.delayCall += () =>
                {
                    // select to save dialog
                    var path = EditorUtility.SaveFilePanel("Export analysis results to json", "",
                        "AddressablesAnalyseResults", "json");
                    AnalyzeSystem.SerializeData(path);
                });
                menu.AddItem(m_ImportJsonGUIContent, false, () => EditorApplication.delayCall += () =>
                {
                    var path = EditorUtility.OpenFilePanel("Import analysis results from json", "", "json");
                    if (!string.IsNullOrEmpty(path))
                        AnalyzeSystem.DeserializeData(path);
                });
                menu.DropDown(plusRect);
            }

            GUILayout.EndArea();

            //TODO
            //if (GUILayout.Button("Revert Selected"))
            //{
            //    m_Tree.RevertAllActiveRules();
            //}
        }
    }
}
