using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    [Serializable]
    class BuildReportListView : IAddressableView
    {
        BuildReportWindow m_Window;
        ListView m_ListView;

        VisualTreeAsset m_ReportListItemTreeAsset;

        [SerializeField]
        List<BuildReportListItem> m_BuildReportItems = new List<BuildReportListItem>();

        static Dictionary<BuildTarget, string> s_PlatformIconClasses = new Dictionary<BuildTarget, string>();

        internal List<BuildReportListItem> BuildReportItems => m_BuildReportItems;

        [Serializable]
        internal class BuildReportListItem
        {
            public int Id { get; }
            public string FilePath { get; }
            public BuildLayout Layout { get; set; }

            public BuildReportListItem(int id, string filePath, BuildLayout layout)
            {
                Id = id;
                FilePath = filePath;
                Layout = layout;
            }
        }

        public BuildReportListView(BuildReportWindow window, VisualTreeAsset reportListItemTreeAsset)
        {
            m_Window = window;
            m_ReportListItemTreeAsset = reportListItemTreeAsset;
        }

        public void CreateGUI(VisualElement rootVisualElement)
        {
            RefreshItemsFromProjectConfig();

            UQueryBuilder<ListView> listQuery = rootVisualElement.Query<ListView>(name: BuildReportUtility.ReportsList);
            m_ListView = listQuery.First();

            m_ListView.makeItem = () =>
            {
                var item = m_ReportListItemTreeAsset.Clone();
                item.Q<VisualElement>(BuildReportUtility.ReportsListItemContainerLefthandElements).style.marginTop = new StyleLength(new Length(2f, LengthUnit.Pixel));
                item.Q<VisualElement>(BuildReportUtility.ReportsListItemContainerRighthandElements).style.marginTop = new StyleLength(new Length(2f, LengthUnit.Pixel));
                item.style.unityTextAlign = TextAnchor.MiddleCenter;

                item.AddManipulator(new ContextualMenuManipulator((evt) =>
                {
                    evt.menu.AppendAction("Remove Report", RemoveReport, DropdownMenuAction.AlwaysEnabled, item.userData);
                    evt.menu.AppendAction("Remove All Reports", RemoveAllReports, DropdownMenuAction.AlwaysEnabled);
                }));

                return item;
            };
            m_ListView.bindItem = (e, i) => CreateItem(e, i);
            m_ListView.unbindItem = (e, i) => e.userData = null;
            m_ListView.itemsSource = m_BuildReportItems;
            m_ListView.selectionChanged -= OnItemSelected;
            m_ListView.selectionChanged += OnItemSelected;
        }

        internal void RefreshItemsFromProjectConfig()
        {
            m_BuildReportItems.Clear();

            List<string> filePaths = ProjectConfigData.BuildReportFilePaths;
            for (int i = 0; i < filePaths.Count; i++)
            {
                BuildLayout layout = null;
                string path = filePaths[i];
                if (File.Exists(path))
                {
                    layout = BuildLayout.Open(path);
                }

                m_BuildReportItems.Insert(0, new BuildReportListItem(i, path, layout));
            }
        }

        static BuildLayout LoadLayout(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                BuildLayout layout = JsonUtility.FromJson<BuildLayout>(json);
                return layout;
            }
            catch (Exception e)
            {
                Debug.Log($"Failed to read BuildReport from {filePath}, with Exception: {e}");
                throw;
            }
        }

        void CreateItem(VisualElement element, int index)
        {
            if (index < 0 || index >= m_BuildReportItems.Count)
                return;

            BuildReportListItem reportListItem = m_BuildReportItems[index];
            element.userData = reportListItem;
            var buildStatusImage = element.Q<Image>(BuildReportUtility.ReportsListItemBuildStatus);
            var buildPlatformImage = element.Q<Image>(BuildReportUtility.ReportsListItemBuildPlatform);
            var buildTimeStampLabel = element.Q<Label>(BuildReportUtility.ReportsListItemBuildTimestamp);
            var buildDurationLabel = element.Q<Label>(BuildReportUtility.ReportsListItemBuildDuration);

            if (reportListItem.Layout == null)
            {
                buildStatusImage.image = EditorGUIUtility.IconContent("CollabError").image as Texture2D;
                buildTimeStampLabel.text = $"Cannot read file";
            }
            else
            {
                buildStatusImage.image = string.IsNullOrEmpty(reportListItem.Layout.BuildError) ?
                    EditorGUIUtility.IconContent("CollabNew").image as Texture2D :
                    EditorGUIUtility.IconContent("CollabError").image as Texture2D;
                buildPlatformImage.ClearClassList();
                buildPlatformImage.AddToClassList("ReportsListItemPlatformIcon");
                buildPlatformImage.AddToClassList(GetPlatformIconClass(reportListItem.Layout.BuildTarget));
                buildTimeStampLabel.text = BuildReportUtility.TimeAgo.GetString(reportListItem.Layout.BuildStart);
                buildDurationLabel.text = TimeSpan.FromSeconds(reportListItem.Layout.Duration).ToString("g");
            }
        }

        static string GetPlatformIconClass(BuildTarget target)
        {
            if (!s_PlatformIconClasses.ContainsKey(target))
                s_PlatformIconClasses[target] = BuildReportUtility.GetIconClassName(target);
            return s_PlatformIconClasses[target];
        }

        internal void OnItemSelected(IEnumerable<object> items)
        {
            var item = items?.FirstOrDefault() as BuildReportListItem;
            if (item == null || !m_BuildReportItems.Contains(item))
                return;

            if (item.Layout == null)
            {
                Debug.LogError($"Unable to read '{item.FilePath}'");
                m_Window?.ClearViews();
            }
            else
            {
                m_Window?.Consume(LoadLayout(item.FilePath));
            }
        }

        internal void LoadNewestReport()
        {
            if (m_BuildReportItems.Count > 0)
            {
                if (File.Exists(m_BuildReportItems[0].FilePath))
                {
                    BuildLayout layout = BuildLayout.Open(m_BuildReportItems[0].FilePath, readFullFile: true);
                    if (layout != null)
                        m_Window.Consume(layout);
                    else
                        Debug.LogWarning($"Unable to load build report at {m_BuildReportItems[0].FilePath}.");
                }
            }
        }

        internal void AddReport(string filePath, BuildLayout layout)
        {
            BuildReportListItem item = m_BuildReportItems.Find(x => ArePathsEqual(x.FilePath, filePath));
            if (item == null)
                m_BuildReportItems.Insert(0, new BuildReportListItem(m_BuildReportItems.Count, filePath, layout));
            else
                item.Layout = layout;

            if (m_ListView != null)
                m_ListView.Rebuild();
        }

        internal void AddReportFromFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath) && Path.GetExtension(filePath).ToLower() == ".json")
            {
                AddReportFromFile(filePath, m_ListView, true, true);
                AddressableAnalytics.ReportUsageEvent(AddressableAnalytics.UsageEventType.BuildReportImportedManually);
            }
        }

        bool BuildLayoutIsValid(BuildLayout layout)
        {
            return BuildLayoutIsValid(layout.PackageVersion);
        }
        internal bool BuildLayoutIsValid(string packageVersion)
        {
            int startOfVersionIndex = packageVersion.IndexOf(":", StringComparison.Ordinal);
            string versionString = packageVersion.Substring(startOfVersionIndex + 1);
            var versionNumbers = versionString.Split(".");

            int versionNumber = 0;
            int majorVersionNumber = 0;
            int minorVersionNumber = 0;

            bool digitParsingSuccessful = int.TryParse(versionNumbers[0], out versionNumber)
                                       && int.TryParse(versionNumbers[1], out majorVersionNumber)
                                       && int.TryParse(versionNumbers[2], out minorVersionNumber);

            if (digitParsingSuccessful)
            {
                // 2.x.x
                var isNewerThanVersionOne = versionNumber > 1;
                // 1.22.x
                var isNewerThanOneDotTwentyOne = versionNumber == 1 && majorVersionNumber > 21;
                // 1.21.4
                var isNewerThanOneDotTwentyOneDotTwo =
                    versionNumber == 1 && majorVersionNumber == 21 && minorVersionNumber >= 3;

                return isNewerThanVersionOne || isNewerThanOneDotTwentyOne || isNewerThanOneDotTwentyOneDotTwo;
            }

            return false;
        }

        void AddReportFromFile(string filePath, ListView listView, bool logWarning, bool shouldRebuild)
        {
            string parsedFilePath = filePath.Replace("\\", "/");
            if (IndexOfFilePathInProjectConfig(parsedFilePath) < 0)
            {
                var layout = LoadLayout(filePath); // can consider adding error logs when file fails to load
                if (layout != null && BuildLayoutIsValid(layout))
                {
                    ProjectConfigData.AddBuildReportFilePath(parsedFilePath);
                    m_BuildReportItems.Insert(0, new BuildReportListItem(m_BuildReportItems.Count, parsedFilePath, layout));

                    if (listView != null && shouldRebuild)
                        listView.Rebuild();
                }
            }
            else if (logWarning)
                Debug.LogWarning($"Already added build report at '{parsedFilePath}'");
        }

        internal void AddReportsFromFolder(string filePath)
        {
            AddReportsFromFolder(filePath, m_ListView, true);
        }


        // Only rebuild when adding a bunch of files at once
        internal void AddReportsFromFolder(string folderPath, ListView listView, bool logWarning)
        {
            if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
            {
                foreach (string file in Directory.EnumerateFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly))
                {
                    AddReportFromFile(file, listView, logWarning, false);
                }
            }

            AddressableAnalytics.ReportUsageEvent(AddressableAnalytics.UsageEventType.BuildReportImportedManually);
            listView.Rebuild();
        }

        internal void RemoveReport(DropdownMenuAction action)
        {
            RemoveReport(action?.userData as BuildReportListItem);
        }

        internal void RemoveReport(BuildReportListItem reportListItem)
        {
            if (reportListItem == null)
                return;

            int index = m_BuildReportItems.IndexOf(reportListItem);
            if (index < 0)
                return;

            m_BuildReportItems.RemoveAt(index);

            int configIndex = IndexOfFilePathInProjectConfig(reportListItem.FilePath);
            if (configIndex >= 0)
                ProjectConfigData.RemoveBuildReportFilePathAtIndex(configIndex);

            m_Window?.ClearViews();
            m_ListView?.Rebuild();
        }

        internal void RemoveAllReports(DropdownMenuAction action)
        {
            ProjectConfigData.ClearBuildReportFilePaths();
            m_BuildReportItems.Clear();

            m_Window?.ClearViews();
            m_ListView?.Rebuild();
        }

        static int IndexOfFilePathInProjectConfig(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return -1;

            List<string> filePaths = ProjectConfigData.BuildReportFilePaths;
            for (int i = 0; i < filePaths.Count; i++)
            {
                if (ArePathsEqual(filePaths[i], filePath))
                    return i;
            }

            return -1;
        }

        static bool ArePathsEqual(string lhs, string rhs)
        {
            if (string.IsNullOrEmpty(lhs) || string.IsNullOrEmpty(rhs))
                return string.IsNullOrEmpty(lhs) && string.IsNullOrEmpty(rhs);

            return string.Equals(lhs.Replace('\\', '/'), rhs.Replace('\\', '/'), StringComparison.Ordinal);
        }
    }
}
