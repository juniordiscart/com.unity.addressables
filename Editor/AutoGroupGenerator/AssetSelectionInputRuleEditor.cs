using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Custom inspector for configuring <see cref="AssetSelectionInputRule"/> assets.
    /// </summary>
    [CustomEditor(typeof(AssetSelectionInputRule))]
    public class AssetSelectionInputRuleEditor : Editor
    {
        #region Fields
        private SerializedProperty m_ValidAssets;
        private SerializedProperty m_JsonAssetLists;
        #endregion

        #region Methods
        private void OnEnable()
        {
            m_ValidAssets = serializedObject.FindProperty("m_SelectedAssets");
            m_JsonAssetLists = serializedObject.FindProperty("m_JsonAssetLists");
        }

        /// <summary>
        /// Draws the custom inspector UI for the asset selection rule.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Selected Assets", EditorStyles.boldLabel);
            DrawObjectList(m_ValidAssets, allowSceneAssets: false, objectLabelWidth: 0f,
                validator: IsValidSelectedAsset,
                removeButtonTooltip: "Remove this asset");

            EditorGUILayout.Space();
            DrawAddAssetButton();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("JSON Input Lists", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Each TextAsset should contain a JSON array of asset paths, e.g.:\n" +
                "[\"Assets/Prefabs/Player.prefab\", \"Assets/Textures/UI/Btn.png\"]\n" +
                "Corrupt JSON files will be skipped with a warning.",
                MessageType.Info
            );

            DrawTextAssetList(m_JsonAssetLists, removeButtonTooltip: "Remove this JSON file");

            EditorGUILayout.Space();
            DrawAddJsonTextAssetField();

            EditorGUILayout.Space();

            SerializedProperty includeAddressables = serializedObject.FindProperty("m_IncludeCurrentAddressables");
            EditorGUILayout.PropertyField(includeAddressables);

            EditorGUILayout.Space();

            if (GUILayout.Button("Add Scenes in Build Profile"))
            {
                AddScenesInBuildToList();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawObjectList(SerializedProperty arrayProp, bool allowSceneAssets, float objectLabelWidth,
            System.Func<Object, bool> validator, string removeButtonTooltip)
        {
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                SerializedProperty element = arrayProp.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();

                element.objectReferenceValue = EditorGUILayout.ObjectField(
                    element.objectReferenceValue, typeof(Object), false);

                if (GUILayout.Button(new GUIContent("X", removeButtonTooltip), GUILayout.Width(20)))
                {
                    arrayProp.DeleteArrayElementAtIndex(i);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawTextAssetList(SerializedProperty arrayProp, string removeButtonTooltip)
        {
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                SerializedProperty element = arrayProp.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();

                element.objectReferenceValue = EditorGUILayout.ObjectField(
                    element.objectReferenceValue, typeof(TextAsset), false);

                if (GUILayout.Button(new GUIContent("X", removeButtonTooltip), GUILayout.Width(20)))
                {
                    arrayProp.DeleteArrayElementAtIndex(i);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawAddAssetButton()
        {
            Object newAsset = EditorGUILayout.ObjectField("Add Asset", null, typeof(Object), false);

            if (newAsset != null && IsValidSelectedAsset(newAsset))
            {
                m_ValidAssets.arraySize++;
                m_ValidAssets.GetArrayElementAtIndex(m_ValidAssets.arraySize - 1).objectReferenceValue = newAsset;
            }
        }

        private void DrawAddJsonTextAssetField()
        {
            TextAsset txt = EditorGUILayout.ObjectField("Add JSON (TextAsset)", null, typeof(TextAsset), false) as TextAsset;
            if (txt == null) return;

            string path = AssetDatabase.GetAssetPath(txt);
            if (string.IsNullOrEmpty(path)) return;

            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            if (ext != ".json" && ext != ".txt")
            {
                EditorGUILayout.HelpBox("Please assign a .json or .txt TextAsset.", MessageType.Warning);
                return;
            }

            int idx = m_JsonAssetLists.arraySize;
            m_JsonAssetLists.InsertArrayElementAtIndex(idx);
            m_JsonAssetLists.GetArrayElementAtIndex(idx).objectReferenceValue = txt;
        }

        private bool IsValidSelectedAsset(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return false;
            if (AssetDatabase.IsValidFolder(path)) return false;

            string extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
            string[] invalidExtensions = { ".cs", ".dll", ".asmdef", ".json", ".txt" };
            foreach (string ext in invalidExtensions)
            {
                if (extension == ext) return false;
            }
            return true;
        }

        private void AddScenesInBuildToList()
        {
            HashSet<string> existingPaths = new();

            for (int i = 0; i < m_ValidAssets.arraySize; i++)
            {
                Object obj = m_ValidAssets.GetArrayElementAtIndex(i).objectReferenceValue;
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path)) existingPaths.Add(path);
            }

            string[] scenePaths = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            foreach (string path in scenePaths)
            {
                if (existingPaths.Contains(path)) continue;

                Object sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (sceneAsset == null || !IsValidSelectedAsset(sceneAsset)) continue;

                int index = m_ValidAssets.arraySize;
                m_ValidAssets.InsertArrayElementAtIndex(index);
                m_ValidAssets.GetArrayElementAtIndex(index).objectReferenceValue = sceneAsset;
            }
        }
        #endregion
    }
}
