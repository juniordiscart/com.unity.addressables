using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.AddressableAssets.GUI
{
    /// <summary>
    /// Row showing analyze output for one rule. Result paths may merge into nested rows that share a prefix;
    /// inner nodes and leaves use this same item type.
    /// </summary>
    class AnalyzeResultsTreeViewItem : AnalyzeTreeViewItemBase
    {
        public MessageType severity { get; set; }
        public HashSet<AnalyzeRule.AnalyzeResult> results { get; }

        /// <summary>
        /// False for informational rows such as "No issues found"; used for partial-fix eligibility with severity.
        /// </summary>
        public bool IsError
        {
            get { return !AddressableAssetUtility.StringContains(displayName, "No issues found", StringComparison.Ordinal); }
        }

        public AnalyzeResultsTreeViewItem(int id, int depth, string displayName, MessageType type)
            : base(id, depth, displayName)
        {
            severity = type;
            results = new HashSet<AnalyzeRule.AnalyzeResult>();
        }

        public AnalyzeResultsTreeViewItem(int id, int depth, string displayName, MessageType type, AnalyzeRule.AnalyzeResult analyzeResult)
            : base(id, depth, displayName)
        {
            severity = type;
            results = new HashSet<AnalyzeRule.AnalyzeResult> { analyzeResult };
        }

        internal static Object GetResultObject(string resultName)
        {
            int li = resultName.LastIndexOf(AnalyzeRule.kDelimiter);
            if (li >= 0)
            {
                string assetPath = resultName.Substring(li + 1);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (!string.IsNullOrEmpty(guid))
                    return AssetDatabase.LoadMainAssetAtPath(assetPath);
            }

            return null;
        }

        internal void DoubleClicked()
        {
            var objects = new HashSet<Object>();
            foreach (var itemResult in results)
            {
                Object o = GetResultObject(itemResult.resultName);
                if (o != null)
                    objects.Add(o);
            }

            if (objects.Count > 0)
            {
                var objectArray = new Object[objects.Count];
                objects.CopyTo(objectArray);
                Selection.objects = objectArray;
                foreach (Object o in objects)
                    EditorGUIUtility.PingObject(o);
            }
        }
    }
}
