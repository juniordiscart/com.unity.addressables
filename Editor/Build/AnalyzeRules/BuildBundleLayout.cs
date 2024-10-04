using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Build.AnalyzeRules
{
    class BuildBundleLayout : BundleRuleBase
    {
        /// <summary>
        /// Result data for assets included in the bundle layout
        /// </summary>
        protected internal struct BuildBundleLayoutResultData
        {
            public string AssetBundleName;
            public string AssetPath;
            public bool Explicit;
        }

        /// <inheritdoc />
        public override bool CanFix
        {
            get { return false; }
        }

        /// <inheritdoc />
        public override string ruleName
        {
            get { return "Bundle Layout Preview"; }
        }

        private List<BuildBundleLayoutResultData> m_ResultData = null;

        /// <summary>
        /// Results of the build Layout.
        /// </summary>
        protected IEnumerable<BuildBundleLayoutResultData> BuildBundleLayoutResults
        {
            get
            {
                if (m_ResultData == null)
                {
                    if (ExtractData == null)
                    {
                        Debug.LogError("RefreshAnalysis needs to be called before getting results");
                        return new List<BuildBundleLayoutResultData>(0);
                    }

                    m_ResultData = new List<BuildBundleLayoutResultData>(512);

                    foreach (var bundleBuild in AllBundleInputDefs)
                    {
                        foreach (string assetName in bundleBuild.assetNames)
                        {
                            m_ResultData.Add(new BuildBundleLayoutResultData()
                            {
                                AssetBundleName = bundleBuild.assetBundleName,
                                AssetPath = assetName,
                                Explicit = true
                            });
                        }
                    }

                    List<string> assets = new List<string>();
                    foreach (KeyValuePair<string, string> fileToBundle in ExtractData.WriteData.FileToBundle)
                    {
                        assets.Clear();
                        string assetBundleName = fileToBundle.Value;

                        var implicitGuids = GetImplicitGuidsForBundle(fileToBundle.Key);
                        foreach (GUID guid in implicitGuids)
                        {
                            string assetPath = AssetDatabase.GUIDToAssetPath(guid.ToString());
                            if (AddressableAssetUtility.IsPathValidForEntry(assetPath))
                                m_ResultData.Add(new BuildBundleLayoutResultData()
                                {
                                    AssetBundleName = assetBundleName,
                                    AssetPath = assetPath,
                                    Explicit = false
                                });
                        }
                    }
                }

                return m_ResultData;
            }
        }

        /// <inheritdoc />
        public override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
        {
            ClearAnalysis();

            if (!BuildUtility.CheckModifiedScenesAndAskToSave())
            {
                Debug.LogError("Cannot run Analyze with unsaved scenes");
                m_Results.Add(new AnalyzeResult {resultName = ruleName + "Cannot run Analyze with unsaved scenes"});
                return m_Results;
            }

            CalculateInputDefinitions(settings);
            var context = GetBuildContext(settings);
            RefreshBuild(context);
            ConvertBundleNamesToGroupNames(context);
            foreach (BuildBundleLayoutResultData result in BuildBundleLayoutResults)
            {
                m_Results.Add(new AnalyzeResult
                {
                    resultName = result.AssetBundleName + kDelimiter
                                                        + (result.Explicit ? "Explicit" : "Implicit") + kDelimiter + result.AssetPath
                });
            }

            if (m_Results.Count == 0)
                m_Results.Add(noErrors);

            AddressableAnalytics.ReportUsageEvent(AddressableAnalytics.UsageEventType.RunBundleLayoutPreviewRule);
            return m_Results;
        }

        /// <inheritdoc />
        public override void ClearAnalysis()
        {
            m_ResultData = null;
            base.ClearAnalysis();
        }

        [InitializeOnLoad]
        class RegisterBuildBundleLayout
        {
            static RegisterBuildBundleLayout()
            {
                AnalyzeSystem.RegisterNewRule<BuildBundleLayout>();
            }
        }
    }
}
