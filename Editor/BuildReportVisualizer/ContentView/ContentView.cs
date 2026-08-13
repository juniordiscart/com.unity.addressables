using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal abstract class ContentView : IAddressableView, IBuildReportConsumer
    {
        protected BuildLayout m_Report;

        internal ToolbarSearchField m_SearchField;
        internal string m_SearchValue;

        protected MultiColumnTreeView m_TreeView = null;
        public MultiColumnTreeView ContentTreeView
        {
            get { return m_TreeView; }
        }

        protected IList<IAddressablesBuildReportItem> m_TreeItems = null;
        public IList<IAddressablesBuildReportItem> TreeItems
        {
            get { return m_TreeItems; }
        }

        // Async search support
        private CancellationTokenSource m_SearchCancellationTokenSource;
        private CancellationTokenSource m_DebounceCancellationTokenSource;
        private string m_PendingSearchValue = null;
        private string m_LastSearchValue = null;
        private const int k_SearchDebounceDelayMs = 500;

        // Loading and no results containers that replace the tree view
        protected VisualElement m_LoadingContainer = null;
        protected VisualElement m_NoResultsContainer = null;
        protected Label m_NoResultsLabel = null;
        protected VisualElement m_ContentViewElement = null;

        // Lazy icon loader
        private IconLazyLoad m_IconLazyLoader = new IconLazyLoad();

        protected object m_CachedFullTree = null;

        public struct TreeDataReportItem
        {
            public int Id;
            public IAddressablesBuildReportItem ReportItem;

            public TreeDataReportItem(int id, IAddressablesBuildReportItem reportItem)
            {
                Id = id;
                ReportItem = reportItem;
            }

        }
        protected Dictionary<Hash128, TreeDataReportItem> m_DataHashtoReportItem = null;
        public Dictionary<Hash128, TreeDataReportItem> DataHashtoReportItem
        {
            get { return m_DataHashtoReportItem; }
        }

        public Action<IEnumerable<object>> ItemsSelected;

        internal abstract ContentViewColumnData[] ColumnDataForView { get; }

        public abstract void Consume(BuildLayout buildReport);

        public abstract void CreateGUI(VisualElement rootVisualElement);

        public virtual void ClearGUI()
        {
            // Cancel any pending searches to prevent them from completing after switching views
            m_SearchCancellationTokenSource?.Cancel();
            m_SearchCancellationTokenSource = null;
            m_DebounceCancellationTokenSource?.Cancel();
            m_DebounceCancellationTokenSource = null;
            m_PendingSearchValue = null;

            // Unregister search field callback to prevent multiple views from responding to the same search
            UnregisterSearchCallback();

            m_IconLazyLoader?.ClearWorkQueue();
            m_NoResultsContainer?.RemoveFromHierarchy();
            m_LoadingContainer?.RemoveFromHierarchy();
            m_TreeView?.RemoveFromHierarchy();
        }

        /// <summary>
        /// Register search field callback - must be implemented by subclasses
        /// </summary>
        protected virtual void RegisterSearchCallback()
        {
            if (m_SearchField != null)
            {
                m_SearchField.RegisterValueChangedCallback(OnSearchValueChanged);
                m_SearchValue = m_SearchField.value;
            }
        }

        /// <summary>
        /// Unregister search field callback - must be implemented by subclasses
        /// </summary>
        protected virtual void UnregisterSearchCallback()
        {
            if (m_SearchField != null)
                m_SearchField.UnregisterValueChangedCallback(OnSearchValueChanged);
        }

        /// <summary>
        /// Callback for search field value changes. Schedules a debounced search.
        /// </summary>
        /// <param name="evt">The change event containing the new search value</param>
        protected void OnSearchValueChanged(ChangeEvent<string> evt)
        {
            if (m_TreeItems == null)
                return;
            ScheduleDebouncedSearch(evt.newValue);
        }

        internal BuildReportHelperConsumer m_HelperConsumer;
        DetailsView m_DetailsView;
        VisualTreeAsset m_TreeViewItem;
        VisualTreeAsset m_TreeViewNavigableItem;

        internal ContentView(BuildReportHelperConsumer helperConsumer, DetailsView detailsView)
        {
            m_HelperConsumer = helperConsumer;
            m_DetailsView = detailsView;
            m_TreeViewItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildReportUtility.TreeViewItemFilePath);
            m_TreeViewNavigableItem = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildReportUtility.TreeViewNavigableItemFilePath);
        }

        public abstract IList<IAddressablesBuildReportItem> CreateTreeViewItems(BuildLayout report);

        // Expresses bundle data as a flat list of TreeViewItemData objects.
        protected IList<TreeViewItemData<IAddressablesBuildReportItem>> CreateTreeRootsFlatList(IList<IAddressablesBuildReportItem> items, Dictionary<Hash128, TreeDataReportItem> dataHashToReportItem)
        {
            int id = 0;
            var roots = new List<TreeViewItemData<IAddressablesBuildReportItem>>(items.Count);

            foreach (IAddressablesBuildReportItem item in items)
            {
                dataHashToReportItem.Add(BuildReportUtility.ComputeDataHash(item.Name, ""), new TreeDataReportItem(id, item));
                roots.Add(new TreeViewItemData<IAddressablesBuildReportItem>(id++, item));
            }
            return roots;
        }

        internal static void SetCallbacksForColumns(Columns columns, ContentViewColumnData[] columnNameToWidth)
        {
            foreach (ContentViewColumnData data in columnNameToWidth)
            {
                Column col = columns[data.Name];
                col.makeCell = () => new Label();
                col.bindCell = data.BindCellCallback;
                col.makeHeader = () => new Label();
                col.bindHeader = data.BindHeaderCallback;
            }
        }

        private IOrderedEnumerable<IAddressablesBuildReportItem> OrderByType(string columnName, Type t)
        {
            if (t == typeof(int))
                return m_TreeItems.OrderBy(item => int.Parse(item.GetSortContent(columnName)));
            if (t == typeof(ulong))
                return m_TreeItems.OrderBy(item => ulong.Parse(item.GetSortContent(columnName)));
            return null;
        }

        readonly Dictionary<string, Type> m_NumericColumnNames = new Dictionary<string, Type>()
        {
            {BuildReportUtility.AssetsContentViewColSizePlusRefs, typeof(ulong)},
            {BuildReportUtility.AssetsContentViewColSizeUncompressed, typeof(ulong)},
            {BuildReportUtility.AssetsContentViewColBundleSize, typeof(ulong)},
            {BuildReportUtility.AssetsContentViewColRefsBy, typeof(int)},
            {BuildReportUtility.AssetsContentViewColRefsTo, typeof(int)},
            {BuildReportUtility.BundlesContentViewColSizePlusRefs, typeof(ulong)},
            {BuildReportUtility.BundlesContentViewColSizeUncompressed, typeof(ulong)},
            {BuildReportUtility.BundlesContentViewBundleSize, typeof(ulong)},
            {BuildReportUtility.BundlesContentViewColRefsBy, typeof(int)},
            {BuildReportUtility.BundlesContentViewColRefsTo, typeof(int)},
            {BuildReportUtility.GroupsContentViewColSizePlusRefs, typeof(ulong)},
            {BuildReportUtility.GroupsContentViewColSizeUncompressed, typeof(ulong)},
            {BuildReportUtility.GroupsContentViewColBundleSize, typeof(ulong)},
            {BuildReportUtility.GroupsContentViewColRefsBy, typeof(int)},
            {BuildReportUtility.GroupsContentViewColRefsTo, typeof(int)},
            {BuildReportUtility.LabelsContentViewColSizePlusRefs, typeof(ulong)},
            {BuildReportUtility.LabelsContentViewColSizeUncompressed, typeof(ulong)},
            {BuildReportUtility.LabelsContentViewColSizeBundle, typeof(ulong)},
            {BuildReportUtility.LabelsContentViewColRefsBy, typeof(int)},
            {BuildReportUtility.LabelsContentViewColRefsTo, typeof(int)},
            {BuildReportUtility.DuplicatedAssetsContentViewSpaceSaved, typeof(ulong)},
            {BuildReportUtility.DuplicatedAssetsContentViewDuplicationCount, typeof(int)},
            {BuildReportUtility.DuplicatedAssetsContentViewColSize, typeof(ulong)}
        };

        public void CreateTreeViewHeader(VisualElement element, string colName, bool isAssetColumn)
        {
            (element as Label).text = ContentTreeView.columns[colName].title;
            if (isAssetColumn)
                element.AddToClassList(BuildReportUtility.TreeViewAssetHeader);
            else
                element.AddToClassList(BuildReportUtility.TreeViewHeader);
        }

        public void CreateTreeViewCell(VisualElement element, int index, string colName, bool isNameColumn, Type type)
        {
            IAddressablesBuildReportItem itemData = null;
            if (type == typeof(AssetsContentView))
               itemData = ContentTreeView.GetItemDataForIndex<AssetsViewBuildReportItem>(index);
            if (type == typeof(BundlesContentView))
                itemData = ContentTreeView.GetItemDataForIndex<BundlesViewBuildReportItem>(index);
            if (type == typeof(LabelsContentView))
                itemData = ContentTreeView.GetItemDataForIndex<LabelsViewBuildReportItem>(index);
            if (type == typeof(GroupsContentView))
                itemData = ContentTreeView.GetItemDataForIndex<GroupsViewBuildReportItem>(index);
            if (type == typeof(DuplicatedAssetsContentView))
                itemData = ContentTreeView.GetItemDataForIndex<DuplicatedAssetsViewBuildReportItem>(index);
            if (isNameColumn)
            {
                ShowEntryIcon(element, itemData, m_TreeViewItem, colName);
                element.AddToClassList(BuildReportUtility.TreeViewIconElement);
            }
            else
            {
                (element as Label).text = itemData.GetCellContent(colName);
                element.AddToClassList(BuildReportUtility.TreeViewElement);
            }
        }

        protected bool EntryAppearsInSearch(IAddressablesBuildReportItem item, string searchValue)
        {
            if (string.IsNullOrEmpty(searchValue))
                return true;
            if (item.Name.Contains(searchValue, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        /// <summary>
        /// Schedules a debounced search to avoid excessive searching while the user is typing
        /// </summary>
        /// <param name="searchValue">The search string to filter items by</param>
        internal async void ScheduleDebouncedSearch(string searchValue)
        {
            // Cancel any pending debounce
            m_DebounceCancellationTokenSource?.Cancel();
            m_DebounceCancellationTokenSource = new CancellationTokenSource();

            m_PendingSearchValue = searchValue;

            try
            {
                // Wait for the debounce delay
                await Task.Delay(k_SearchDebounceDelayMs, m_DebounceCancellationTokenSource.Token);

                // After delay, perform the search
                PerformDebouncedSearchAsync();
            }
            catch (TaskCanceledException)
            {
                // Debounce was cancelled by a new keystroke, this is expected
            }
        }

        private async void PerformDebouncedSearchAsync()
        {
            if (m_PendingSearchValue == null)
                return;

            string searchToExecute = m_PendingSearchValue;
            m_PendingSearchValue = null;

            // Cancel any existing search
            m_SearchCancellationTokenSource?.Cancel();
            m_SearchCancellationTokenSource = new CancellationTokenSource();

            try
            {
                await PerformSearchAsync(searchToExecute, m_SearchCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Search was cancelled, this is expected
            }
        }

        protected abstract Task PerformSearchAsync(string searchValue, CancellationToken cancellationToken);

        internal async Task PerformSearchAsyncImpl<T>(string searchValue, CancellationToken cancellationToken) where T : IAddressablesBuildReportItem
        {
            ShowLoadingIndicator();

            //We're searching for the same thing again, so just show the tree as-is
            if (m_LastSearchValue == searchValue)
            {
                ShowTree();
                return;
            }

            m_SearchValue = searchValue;

            // Clear the icon work queue - we'll only load icons for filtered results
            m_IconLazyLoader.ClearWorkQueue();

            // If no search value, show cached full tree
            if (string.IsNullOrEmpty(searchValue))
            {
                //delay one frame to allow UI to update
                EditorApplication.delayCall += () =>
                {
                    // Check if search was cancelled while waiting for delayCall
                    if (cancellationToken.IsCancellationRequested)
                    {
                        HideLoadingIndicator();
                        return;
                    }

                    if (m_CachedFullTree is List<TreeViewItemData<T>> cachedTree)
                    {
                        m_TreeView.SetRootItems(cachedTree);
                        m_TreeView.Rebuild();
                    }
                    ShowTree();
                };
                return;
            }

            // Filter cached tree instead of rebuilding
            var allResults = new List<TreeViewItemData<T>>();
            await Task.Run(() =>
            {
                if (m_CachedFullTree is List<TreeViewItemData<T>> cachedTree)
                {
                    //foreach (var rootItem in cachedTree)
                    Parallel.ForEach(cachedTree, (rootItem, loopState) =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                            loopState.Break();

                        var filteredItem = FilterTreeItem(rootItem, searchValue);
                        if (filteredItem.HasValue)
                        {
                            lock (allResults)
                            {
                                allResults.Add(filteredItem.Value);
                            }
                        }
                    });
                }
            }, cancellationToken);

            // Final update with all results
            if (!cancellationToken.IsCancellationRequested)
            {
                m_TreeView.SetRootItems(allResults);
                m_TreeView.Rebuild();

                // Check if search returned no results
                if (allResults.Count == 0 && !string.IsNullOrEmpty(searchValue))
                {
                    ShowNoResultsMessage(searchValue);
                }
                else
                {
                    ShowTree();
                }
            }
            m_LastSearchValue = searchValue;
        }

        private TreeViewItemData<T>? FilterTreeItem<T>(TreeViewItemData<T> item, string searchValue) where T : IAddressablesBuildReportItem
        {
            bool itemMatches = EntryAppearsInSearch(item.data, searchValue);
            var filteredChildren = new List<TreeViewItemData<T>>();

            // Recursively filter children
            if (item.hasChildren)
            {
                foreach (var child in item.children)
                {
                    var filteredChild = FilterTreeItem<T>(child, searchValue);
                    if (filteredChild.HasValue)
                    {
                        filteredChildren.Add(filteredChild.Value);
                    }
                }
            }

            // Include this item if it matches OR has matching children
            if (itemMatches || filteredChildren.Count > 0)
            {
                return new TreeViewItemData<T>(item.id, item.data, filteredChildren);
            }

            return null;
        }

        /// <summary>
        /// Shows the loading indicator UI and hides the tree view and no results message
        /// </summary>
        internal void ShowLoadingIndicator()
        {
            if (m_ContentViewElement == null)
                return;

            HideTree();
            HideNoResultsMessage();

            // Remove ALL loading containers from the DOM to prevent duplicates (handles race conditions from multiple views)
            var existingContainers = m_ContentViewElement.Query<VisualElement>(name: "LoadingContainer").ToList();
            foreach (var container in existingContainers)
                container.RemoveFromHierarchy();

            // Add this view's loading container
            if (m_LoadingContainer != null)
            {
                m_ContentViewElement.Add(m_LoadingContainer);
                // Reset rotation for the spinner
                var spinner = m_LoadingContainer.Q<VisualElement>("LoadingSpinner");
                if (spinner != null)
                    spinner.transform.rotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Shows the tree view and hides the loading indicator and no results message
        /// </summary>
        internal void ShowTree()
        {
            if (m_TreeView == null)
                return; // should probably warn/error

            HideLoadingIndicator();
            HideNoResultsMessage();

            // Remove ALL MultiColumnTreeView elements from ContentView (handles multiple views' searches completing)
            var existingTreeViews = m_ContentViewElement.Query<MultiColumnTreeView>().ToList();
            foreach (var treeView in existingTreeViews)
                treeView.RemoveFromHierarchy();

            // Add this view's tree view
            m_ContentViewElement.Add(m_TreeView);
        }

        /// <summary>
        /// Hides the tree view by removing it from the content view
        /// </summary>
        internal void HideTree()
        {
            // Remove ALL tree views from ContentView (handles race conditions)
            if (m_ContentViewElement != null)
            {
                var existingTreeViews = m_ContentViewElement.Query<MultiColumnTreeView>().ToList();
                foreach (var treeView in existingTreeViews)
                    treeView.RemoveFromHierarchy();
            }
        }

        /// <summary>
        /// Hides the loading indicator by removing it from the content view
        /// </summary>
        protected void HideLoadingIndicator()
        {
            if (m_ContentViewElement == null)
                return;

            // Remove ALL loading containers from ContentView (handles race conditions)
            var existingContainers = m_ContentViewElement.Query<VisualElement>(name: "LoadingContainer").ToList();
            foreach (var container in existingContainers)
                container.RemoveFromHierarchy();
        }

        /// <summary>
        /// Shows the "no results" message UI and hides the tree view and loading indicator
        /// </summary>
        /// <param name="searchValue">The search string that returned no results</param>
        internal void ShowNoResultsMessage(string searchValue)
        {
            if (m_ContentViewElement == null)
                return;

            HideTree();
            HideLoadingIndicator();

            // Remove ALL no results containers from the DOM to prevent duplicates (handles race conditions from multiple views)
            var existingContainers = m_ContentViewElement.Query<VisualElement>(name: "NoResultsContainer").ToList();
            foreach (var container in existingContainers)
                container.RemoveFromHierarchy();

            // Update the label text with the search string
            if (m_NoResultsLabel != null && !string.IsNullOrEmpty(searchValue))
            {
                m_NoResultsLabel.text = $"No results for \"{searchValue}\"";
            }

            // Add this view's no results container
            if (m_NoResultsContainer != null)
            {
                m_ContentViewElement.Add(m_NoResultsContainer);
            }
        }

        /// <summary>
        /// Hides the "no results" message by removing it from the content view
        /// </summary>
        protected void HideNoResultsMessage()
        {
            if (m_ContentViewElement == null)
                return;

            // Remove ALL no results containers from ContentView (handles race conditions)
            var existingContainers = m_ContentViewElement.Query<VisualElement>(name: "NoResultsContainer").ToList();
            foreach (var container in existingContainers)
                container.RemoveFromHierarchy();
        }

        // Helper class for building trees with incremental UI updates
        protected class TreeBuilder<T> where T : IAddressablesBuildReportItem
        {
            private List<TreeViewItemData<T>> m_Results = new List<TreeViewItemData<T>>();

            public void Add(TreeViewItemData<T> item)
            {
                m_Results.Add(item);
            }

            public List<TreeViewItemData<T>> GetResults()
            {
                lock (m_Results)
                {
                    return m_Results;
                }
            }
        }

        /// <summary>
        /// Loads and displays a tree view by building the tree data and optionally performing finalization steps
        /// </summary>
        /// <typeparam name="T">The type of build report items in the tree</typeparam>
        /// <param name="buildTreeAction">Action to build the tree data by populating the provided list</param>
        /// <param name="finalizeBeforeDisplayAction">Optional action to finalize the tree data before displaying it</param>
        protected void LoadTree<T>(Action<List<TreeViewItemData<T>>> buildTreeAction, Action<List<TreeViewItemData<T>>> finalizeBeforeDisplayAction = null) where T : IAddressablesBuildReportItem
        {
            // Clear the icon work queue when building a new tree
            m_IconLazyLoader.ClearWorkQueue();

            var treeViewItemResults = new List<TreeViewItemData<T>>();
            buildTreeAction(treeViewItemResults);
            m_CachedFullTree = treeViewItemResults;

            // Allow views to update additional state before displaying
            finalizeBeforeDisplayAction?.Invoke(treeViewItemResults);

            m_TreeView.SetRootItems(treeViewItemResults);
            m_TreeView.Rebuild();
            ShowTree();
        }

        /// <summary>
        /// Creates the loading indicator UI elements including spinner and text
        /// </summary>
        /// <param name="rootVisualElement">The root visual element to query for the content view</param>
        protected void CreateLoadingIndicator(VisualElement rootVisualElement)
        {
            if(m_ContentViewElement != null)
                m_ContentViewElement.Clear();

            m_ContentViewElement = rootVisualElement.Q<VisualElement>(BuildReportUtility.ContentView);
            if (m_ContentViewElement == null)
                return;

            // Always clear and add the current tree view, even if containers already exist
            // This handles the case where CreateGUI() is called multiple times with new tree views
            m_ContentViewElement.Clear();
            //if (m_TreeView != null && m_TreeView.parent != m_ContentViewElement)
            //    m_ContentViewElement.Add(m_TreeView);

            if (m_LoadingContainer == null)
            {
                // Create loading container - centered in the content view
                m_LoadingContainer = new VisualElement();
                m_LoadingContainer.name = "LoadingContainer";
                m_LoadingContainer.style.flexGrow = 1;
                m_LoadingContainer.style.alignItems = Align.Center;
                m_LoadingContainer.style.justifyContent = Justify.Center;

                // Create inner container for spinner and text (side by side)
                var innerContainer = new VisualElement();
                innerContainer.style.flexDirection = FlexDirection.Row;
                innerContainer.style.alignItems = Align.Center;

                // Create spinner
                var spinner = new VisualElement();
                spinner.name = "LoadingSpinner";
                spinner.style.width = 32;
                spinner.style.height = 32;
                spinner.style.marginRight = 10;

                var spinnerIcon = new Image();
                spinnerIcon.image = EditorGUIUtility.IconContent("Loading").image as Texture2D;
                spinnerIcon.style.width = 32;
                spinnerIcon.style.height = 32;
                spinner.Add(spinnerIcon);

                // Create loading text
                var loadingText = new Label("Searching...");
                loadingText.style.color = Color.white;
                loadingText.style.unityFontStyleAndWeight = FontStyle.Bold;
                loadingText.style.fontSize = 14;

                innerContainer.Add(spinner);
                innerContainer.Add(loadingText);
                m_LoadingContainer.Add(innerContainer);

                // Add rotation animation for spinner
                spinner.schedule.Execute(() =>
                {
                    if (m_LoadingContainer != null && m_LoadingContainer.parent != null)
                    {
                        var currentRotation = spinner.transform.rotation.eulerAngles.z;
                        spinner.transform.rotation = Quaternion.Euler(0, 0, currentRotation + 10);
                    }
                }).Every(50);
            }

            if (m_NoResultsContainer == null)
            {
                // Create "No results" container - centered in the content view
                m_NoResultsContainer = new VisualElement();
                m_NoResultsContainer.name = "NoResultsContainer";
                m_NoResultsContainer.style.flexGrow = 1;
                m_NoResultsContainer.style.alignItems = Align.Center;
                m_NoResultsContainer.style.justifyContent = Justify.Center;

                m_NoResultsLabel = new Label("No results found");
                m_NoResultsLabel.style.color = Color.white;
                m_NoResultsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                m_NoResultsLabel.style.fontSize = 14;

                m_NoResultsContainer.Add(m_NoResultsLabel);
            }
        }

        public void ShowEntryIcon(VisualElement element, IAddressablesBuildReportItem itemData, VisualTreeAsset baseItem, string colName)
        {
            (element as Label).text = string.Empty;
            element.Clear();

            VisualElement treeItem = baseItem.Clone(element);
            var icon = treeItem.Q<Image>(BuildReportUtility.TreeViewItemIcon);
            var name = treeItem.Q<TextElement>(BuildReportUtility.TreeViewItemName);
            name.text = itemData.GetCellContent(colName);

            if (itemData is IAddressablesBuildReportAsset asset)
            {
                string path = asset.ExplicitAsset == null ? asset.DataFromOtherAsset.AssetPath : asset.ExplicitAsset.AssetPath;

                // Use lazy loading for icons to improve performance
                if (!string.IsNullOrEmpty(path))
                {
                    m_IconLazyLoader.LoadIconLazy(icon, path);
                }
                else
                {
                    icon.AddToClassList(BuildReportUtility.TreeViewItemNoIcon);
                }

                if (asset.DataFromOtherAsset != null)
                    name.AddToClassList(BuildReportUtility.TreeViewImplicitAsset);
                if (asset is DuplicatedAssetsViewBuildReportDuplicatedAsset)
                    name.AddToClassList(BuildReportUtility.TreeViewDuplicatedAsset);
            }
            else if (itemData is LabelsViewBuildReportLabel label)
                icon.image = EditorGUIUtility.IconContent("FilterByLabel").image as Texture2D;
            else if (itemData is GroupsViewBuildReportGroup group ||
                     itemData is BundlesViewBuildReportIndirectlyReferencedBundles ||
                     itemData is GroupsViewBuildReportIndirectlyReferencedBundles)
                icon.image = EditorGUIUtility.IconContent("d_FolderOpened Icon").image as Texture2D;
            else if (itemData is IAddressablesBuildReportBundle)
                icon.image = EditorGUIUtility.IconContent("Package Manager").image as Texture2D;
            else
                icon.AddToClassList(BuildReportUtility.TreeViewItemNoIcon);

            name.AddManipulator(new ContextualMenuManipulator((ContextualMenuPopulateEvent evt) =>
            {
                evt.menu.AppendAction("Search in this window", (e) =>
                {
                    string newSearchValue = name.text;
                    m_SearchField.Q<TextField>().value = newSearchValue;
                });
            }));
        }

        public ContentView UseCachedView(VisualElement rootVisualElement)
        {
            m_ContentViewElement = rootVisualElement.Q<VisualElement>(BuildReportUtility.ContentView);
            if (m_ContentViewElement == null)
                return this;

            // Clear ContentView to remove any containers from other views
            m_ContentViewElement.Clear();
            HideNoResultsMessage();
            HideLoadingIndicator();

            RegisterSearchCallback();

            // Add this view's tree view
            if (m_TreeView != null && m_TreeView.parent != m_ContentViewElement)
                m_ContentViewElement.Add(m_TreeView);

            return this;
        }

        internal List<IAddressablesBuildReportItem> SortByColumnDescription(SortColumnDescription col)
        {
            IOrderedEnumerable<IAddressablesBuildReportItem> sortedTreeRootEnumerable;
            if (m_NumericColumnNames.ContainsKey(col.columnName))
            {
                Type t = m_NumericColumnNames[col.columnName];
                sortedTreeRootEnumerable = OrderByType(col.columnName, t);
            }
            else
            {
                sortedTreeRootEnumerable = m_TreeItems.OrderBy(item => item.GetSortContent(col.columnName));
            }

            List<IAddressablesBuildReportItem> finalTreeRoots = new List<IAddressablesBuildReportItem>(m_TreeItems.Count);
            foreach (var item in sortedTreeRootEnumerable)
                finalTreeRoots.Add(item);
            if (col.direction == SortDirection.Ascending)
                finalTreeRoots.Reverse();

            return finalTreeRoots;
        }
    }

    internal struct ContentViewColumnData
    {
        public string Name;
        public string Title;
        public Action<VisualElement, int> BindCellCallback;
        public Action<VisualElement> BindHeaderCallback;

        public ContentViewColumnData(string name, ContentView view, bool isNameColumn, string title = "N/a")
        {
            Name = name;
            Title = title;
            BindCellCallback = ((element, index) =>
            {
                view.CreateTreeViewCell(element, index, name, isNameColumn, view.GetType());
            });
            BindHeaderCallback = ((element) =>
            {
                view.CreateTreeViewHeader(element, name, isNameColumn);
            });
        }
    }

    /// <summary>
    /// Nested interface that can be either a bundle or asset.
    /// </summary>
    public interface IAddressablesBuildReportItem
    {
        /// <summary>
        /// The name of the build report item
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Create the UI element for a build report item
        /// </summary>
        /// <param name="rootVisualElement">The visual element container</param>
        void CreateGUI(VisualElement rootVisualElement);

        /// <summary>
        /// Get content for a cell given a column name
        /// </summary>
        /// <param name="colName">The name of the column</param>
        /// <returns>The display string for the cell</returns>
        string GetCellContent(string colName);

        /// <summary>
        /// Get the sortable content
        /// </summary>
        /// <param name="colName">The name of the column</param>
        /// <returns>The display string for the cell</returns>
        string GetSortContent(string colName);

    }

    /// <summary>
    /// Interface for an AssetBundle build report item
    /// </summary>
    public interface IAddressablesBuildReportBundle
    {
        /// <summary>
        /// The AssetBundle data
        /// </summary>
        public BuildLayout.Bundle Bundle { get; }
    }

    /// <summary>
    /// Interface for Asset build report item
    /// </summary>
    public interface IAddressablesBuildReportAsset
    {
        /// <summary>
        /// The data to set if the asset is an explicit asset (Addressable)
        /// </summary>
        public BuildLayout.ExplicitAsset ExplicitAsset { get; }

        /// <summary>
        /// The data to set if the asset is an implicit asset (non-Addressable, but pulled into an Asset Bundle)
        /// </summary>
        public BuildLayout.DataFromOtherAsset DataFromOtherAsset { get; }

        /// <summary>
        /// The data for the AssetBundle the asset belongs to
        /// </summary>
        public List<BuildLayout.Bundle> Bundles { get; }

        /// <summary>
        /// The total size of this asset plus its dependencies
        /// </summary>
        public ulong SizeWDependencies { get; }
    }
}
