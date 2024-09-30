#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class ContentDataTreeView
    {
        private MultiColumnTreeView m_MainTreeView;
        private TreeView m_TreeView;
        private VisualElement m_Parent;

        private readonly Queue<VisualElement> m_VisualsCache = new Queue<VisualElement>(32);

        public void SetParent(VisualElement parent)
        {
            if (m_Parent == parent)
                return;
            if (m_Parent != null)
                m_Parent.Remove(m_TreeView);
            if (parent != null)
                parent.Add(m_TreeView);
            m_Parent = parent;
        }

        public void Initialise(TreeView treeView, MultiColumnTreeView mainTreeView)
        {
            if (treeView == null)
            {
                Debug.LogException(new System.NullReferenceException("No treeView provided for displaying ContentDataList with"));
                return;
            }

            m_MainTreeView = mainTreeView;

            if (m_TreeView != null)
                SetSource(new List<ContentData>());

            m_TreeView = treeView;
            m_TreeView.makeItem = MakeItem;
            m_TreeView.destroyItem = DestroyItem;
            m_TreeView.bindItem = BindItem;

            m_TreeView.itemsChosen += MoveToChosenContent;
        }

        private void BindItem(VisualElement element, int dataIndex)
        {
            ContentData contentData = m_TreeView.GetItemDataForIndex<ContentData>(dataIndex);
            AssetLabel al = element as AssetLabel;
            al.SetContent(contentData);
        }

        private void DestroyItem(VisualElement e)
        {
            m_VisualsCache.Enqueue(e);
        }

        private VisualElement MakeItem()
        {
            return m_VisualsCache.Count == 0 ? new AssetLabel(0, false) : m_VisualsCache.Dequeue();
        }

        class ContentTreeItem
        {
            public ContentData self;
            public List<ContentTreeItem> children = new List<ContentTreeItem>();
            public ContentTreeItem(ContentData item)
            {
                self = item;
            }
        }
        public void SetSource(List<ContentData[]> contentData)
        {
            List<TreeViewItemData<ContentData>> rootTreeItems = new List<TreeViewItemData<ContentData>>();
            List<ContentTreeItem> rootItems = new List<ContentTreeItem>();
            foreach (ContentData[] branch in contentData)
            {
                if (branch == null || branch.Length == 0)
                    continue;
                ContentData data = branch[0];
                ContentTreeItem root = null;
                foreach (ContentTreeItem rootItem in rootItems)
                {
                    if (rootItem.self == data)
                    {
                        root = rootItem;
                        break;
                    }
                }

                if (root == null)
                {
                    root = new ContentTreeItem(data);
                    rootItems.Add(root);
                }

                for (int i = 1; i < branch.Length; ++i)
                {
                    data = branch[i];
                    root = GetOrCreateChild(root, data);
                }
            }

            foreach (ContentTreeItem rootItem in rootItems)
                rootTreeItems.Add(GenerateTreeItem(rootItem));

            SetRootItemsAndRebuild(rootTreeItems);
        }

        private static ContentTreeItem GetOrCreateChild(ContentTreeItem root, ContentData data)
        {
            foreach (ContentTreeItem rootChild in root.children)
            {
                if (rootChild.self == data)
                    return rootChild;
            }

            ContentTreeItem child = new ContentTreeItem(data);
            root.children.Add(child);
            return child;
        }

        TreeViewItemData<ContentData> GenerateTreeItem(ContentTreeItem content)
        {
            if (content.children.Count > 0)
            {
                List<TreeViewItemData<ContentData>> children = new List<TreeViewItemData<ContentData>>();
                foreach (ContentTreeItem contentChild in content.children)
                {
                    children.Add(GenerateTreeItem(contentChild));
                }
                return new TreeViewItemData<ContentData>(content.self.TreeViewID, content.self, children);
            }
            else
                return new TreeViewItemData<ContentData>(content.self.TreeViewID, content.self);
        }

        public void SetSource(List<ContentData> contentData)
        {
            if (contentData == null)
            {
                Debug.LogException(new System.NullReferenceException("Attempting to show details for a null item data"));
                return;
            }

            List<TreeViewItemData<ContentData>> rootItems = new List<TreeViewItemData<ContentData>>();
            int index = 0;
            foreach (ContentData baseAsset in contentData)
            {
                TreeViewItemData<ContentData> treeItem = new TreeViewItemData<ContentData>(index, baseAsset);
                rootItems.Add(treeItem);
                index++;
            }
            SetRootItemsAndRebuild(rootItems);
        }

        private void SetRootItemsAndRebuild(IList<TreeViewItemData<ContentData>> rootItems)
        {
            m_TreeView.SetRootItems(rootItems);
            m_TreeView.Rebuild();
        }

        private void MoveToChosenContent(IEnumerable<object> obj)
        {
            List<int> idsToSelect = new List<int>();
            foreach (object o in obj)
            {
                ContentData data = o as ContentData;
                if (data == null)
                {
                    Debug.LogException(new System.NullReferenceException("Missing content data for selection"));
                    continue;
                }
                idsToSelect.Add(data.TreeViewID);
            }

            if (m_MainTreeView != null)
            {
                m_MainTreeView.SetSelectionById(idsToSelect);
                if (idsToSelect.Count == 1)
                    m_MainTreeView.ScrollToItemById(idsToSelect[0]);
            }
        }
    }
}

#endif
