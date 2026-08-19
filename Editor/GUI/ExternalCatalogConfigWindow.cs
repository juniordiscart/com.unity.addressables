using System;
using System.Linq;
using UnityEngine;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace UnityEditor.AddressableAssets.GUI
{
    [CustomEditor(typeof(ExternalCatalogConfig))]
    public class ExternalCatalogConfigWindow : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(20f);

            if (GUILayout.Button("Assign Paths to Groups"))
            {
                EditorApplication.delayCall += SetExternalCatalogGroupPaths;
            }
        }

        private void SetExternalCatalogGroupPaths()
        {
            ExternalCatalogConfig buildScript = (ExternalCatalogConfig)target;
            BundledAssetGroupSchema[] schemas = buildScript.AssetGroups.Select(aag => aag.GetSchema<BundledAssetGroupSchema>()).Where(schema => schema != null).ToArray();

            Undo.RecordObjects(schemas, nameof(BundledAssetGroupSchema));
            Array.ForEach(schemas, schema =>
            {
                schema.LoadPath.SetVariableById(schema.settings, buildScript.RuntimeLoadPath.Id);
                schema.BuildPath.SetVariableById(schema.settings, buildScript.BuildPath.Id);
                EditorUtility.SetDirty(schema);
            });
        }
    }
}

