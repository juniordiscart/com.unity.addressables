#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class ContentDataListView
    {
        private MultiColumnTreeView m_MainTreeView;
        private ListView m_ListView;
        private VisualElement m_Parent;

        private List<ContentData> m_SourceData;

        private readonly Queue<VisualElement> m_VisualsCache = new Queue<VisualElement>(32);

        public void Initialise(ListView listView, MultiColumnTreeView mainTreeView)
        {
            if (listView == null)
            {
                Debug.LogException(new System.NullReferenceException("No treeView provided for displaying ContentDataList with"));
                return;
            }

            m_MainTreeView = mainTreeView;

            if (m_ListView != null)
                SetSource(new List<ContentData>());

            m_ListView = listView;
            m_ListView.makeItem = MakeItem;
            m_ListView.destroyItem = DestroyItem;
            m_ListView.bindItem = BindItem;

            m_ListView.itemsChosen += MoveToChosenContent;
        }

        public void SetParent(VisualElement parent)
        {
            if (m_Parent == parent)
                return;
            if (m_Parent != null)
                m_Parent.Remove(m_ListView);
            if (parent != null)
                parent.Add(m_ListView);
            m_Parent = parent;
        }

        private void BindItem(VisualElement element, int dataIndex)
        {
            ContentData contentData = m_SourceData[dataIndex];
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

        public void SetSource(List<ContentData> selectedData)
        {
            if (selectedData == null)
            {
                Debug.LogException(new System.NullReferenceException("Attempting to show details for a null item data"));
                return;
            }

            m_SourceData = selectedData;
            m_ListView.itemsSource = m_SourceData;
            m_ListView.Rebuild();
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
