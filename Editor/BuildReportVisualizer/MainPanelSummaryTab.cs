#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.UIElements;
namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal class MainPanelSummaryTab : IAddressableView, IBuildReportConsumer
    {
        BuildLayout m_Report;
        public VisualElement tabRootElement;
        internal ScrollView scrollbarElement;

        BuildReportWindow m_Window;

        class SummaryRow
        {
            private TextElement m_LabelElement;
            private TextElement m_SizeElement;
            private string m_TypeName;

            public SummaryRow(VisualElement root, string typeName)
            {
                m_TypeName = typeName;
                m_LabelElement = root.Q<TextElement>(string.Format(BuildReportUtility.SummaryTabLabelElementNameFormat, typeName));
                m_SizeElement = root.Q<TextElement>(string.Format(BuildReportUtility.SummaryTabSizeElementNameFormat, typeName));
            }

            public void Display(AssetSummary info)
            {
                m_LabelElement.text = $"{m_TypeName} ({info.Count})";
                m_SizeElement.text = BuildReportUtility.GetDenominatedBytesString(info.SizeInBytes);
            }
            public void Display(BundleSummary info)
            {
                m_LabelElement.text = $"{m_TypeName} ({info.Count})";
                m_SizeElement.text = BuildReportUtility.GetDenominatedBytesString(info.TotalCompressedSize);
            }

            public void Reset()
            {
                m_LabelElement.text = $"{m_TypeName} (0)";
                m_SizeElement.text = "0 B";
            }
        }

        BuildReportHelperConsumer m_HelperConsumer;

        internal MainPanelSummaryTab(BuildReportWindow window, BuildReportHelperConsumer helperConsumer)
        {
            m_HelperConsumer = helperConsumer;
            m_Window = window;
        }

        public void Consume(BuildLayout buildReport)
        {
            m_Report = buildReport;
            ClearGUI();

            BuildLayoutSummary summary = BuildLayoutSummary.GetSummaryWithoutAssetTypes(buildReport);

            //This is a little overengineered, but if we do building multiple catalogs then this should help.
            HashSet<string> remoteCatalogNames = new HashSet<string>();
            HashSet<string> localCatalogNames = new HashSet<string>();

            foreach (var catalog in buildReport.AddressablesRuntimeSettings.CatalogLoadPaths)
            {
                string name = Path.GetFileName(catalog);
                if (name.EndsWith(".hash"))
                    remoteCatalogNames.Add(name.Replace(".hash", ".json"));
                else
                    localCatalogNames.Add(name);
            }

            SummaryRowBuilder generalInfo = new SummaryRowBuilder("General Information");
            if (localCatalogNames.Count > 0)
                generalInfo.With("Local Catalog(s)", string.Join(", ", localCatalogNames));
            if (remoteCatalogNames.Count > 0)
                generalInfo.With("Remote Catalog(s)", string.Join(", ", remoteCatalogNames));

            generalInfo.With("Local catalog build location", buildReport.LocalCatalogBuildPath)
                .With("Remote catalog build location", string.IsNullOrEmpty(buildReport.RemoteCatalogBuildPath) ? "No remote catalog built" : buildReport.RemoteCatalogBuildPath)
                .With("Profile", buildReport.AddressablesEditorSettings.ActiveProfile.Name)
                .With("Platform", buildReport.BuildTarget.ToString())
                .With("Player Build Version", buildReport.PlayerBuildVersion)
                .With("Build Duration", TimeSpan.FromSeconds(buildReport.Duration).ToString("g"))
                .With("Package Version", buildReport.PackageVersion)
                .With("Unity Editor Version", buildReport.UnityVersion);

            scrollbarElement.Add(generalInfo.Build());

            if (buildReport.DuplicatedAssets.Count > 0)
            {
                ulong duplicatedSize = 0;
                foreach (var dupeAsset in  m_HelperConsumer.GUIDToDuplicateAssets.Values)
                {
                    int duplicationCount = dupeAsset.Bundles.Count;
                    if (duplicationCount > 1)
                        duplicatedSize += (ulong) (duplicationCount - 1) * dupeAsset.Asset.SerializedSize;
                }

                SummaryRowBuilder duplicatedAssets = new SummaryRowBuilder("Potential Issues")
                    .With(new PotentialIssuesCard($"{buildReport.DuplicatedAssets.Count} Duplicate Assets were detected in the build.  \n\n" +
                    $"Removing duplicated Assets could result in up to {BuildReportUtility.GetDenominatedBytesString(duplicatedSize)} reduced build size.",
                    () => m_Window.NavigateToView(BuildReportWindow.PotentialIssuesType.DuplicatedAssetsView)));

                scrollbarElement.Add(duplicatedAssets.Build());
            }

            SummaryRowBuilder aggregateInfo = new SummaryRowBuilder("Aggregate Information")
                .With("Number of bundles", summary.BundleSummary.Count.ToString())
                .With("Total size of all bundles", BuildReportUtility.GetDenominatedBytesString(summary.BundleSummary.TotalCompressedSize))
                .With("Total number of assets", summary.TotalAssetCount.ToString())
                .With("Addressables", $"{summary.ExplicitAssetCount} ({String.Format("{0:0.##}", ((float)summary.ExplicitAssetCount/(float)summary.TotalAssetCount) * 100f)}%)")
                .With("Assets pulled into a build by an Addressable", $"{summary.ImplicitAssetCount} ({String.Format("{0:0.##}", ((float)summary.ImplicitAssetCount/(float)summary.TotalAssetCount) * 100f)}%)", FontStyle.Italic);

            scrollbarElement.Add(aggregateInfo.Build());

            tabRootElement.visible = true;
            tabRootElement.MarkDirtyRepaint();
            scrollbarElement.visible = true;
            scrollbarElement.MarkDirtyRepaint();
        }

        public void CreateGUI(VisualElement rootVisualElement)
        {
            tabRootElement = rootVisualElement.Q<VisualElement>(BuildReportUtility.SummaryTab);

            // clear out the style sheets defined in the template uxml file so they can be applied from here in the order of: 1. theming, 2. base
            tabRootElement.styleSheets.Clear();
            var themeStyle = AssetDatabase.LoadAssetAtPath(EditorGUIUtility.isProSkin ? BuildReportUtility.SummaryTabDarkUssPath : BuildReportUtility.SummaryTabLightUssPath, typeof(StyleSheet)) as StyleSheet;
            tabRootElement.styleSheets.Add(themeStyle);
            scrollbarElement = new ScrollView();
            scrollbarElement.verticalScrollerVisibility = ScrollerVisibility.Auto;
            tabRootElement.Add(scrollbarElement);

            var ribbonStyle = AssetDatabase.LoadAssetAtPath(BuildReportUtility.SummaryTabUssPath, typeof(StyleSheet)) as StyleSheet;
            tabRootElement.styleSheets.Add(ribbonStyle);

            tabRootElement.visible = false;
        }

        public void ClearGUI()
        {
            scrollbarElement.Clear();
            scrollbarElement.visible = false;
        }
    }
}
#endif
