#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class AddressablesProfilerDetailsTreeView
    {
        private const string k_ColumnVisibilityKeyPrefix = "com.unity.addressables.profiler.treeviewcolumn.";

        public AddressablesProfilerDetailsTreeView(TreeViewPane rootView)
        {
            m_TreeView = rootView.TreeView;
            if (m_TreeView == null)
            {
                Debug.LogError("Could not find treeView for ProfilerDetails view");
                return;
            }

            for (int i = 0; i < 32; ++i)
                m_LabelCache.Enqueue(new Label());

            m_LastColumnVisibles = new bool[m_TreeView.columns.Count];
            for (int i=0; i<m_TreeView.columns.Count; ++i)
            {
                Column col = m_TreeView.columns[i];
                col.sortable = true;
                col.visible = EditorPrefs.GetBool(k_ColumnVisibilityKeyPrefix + col.name, col.visible);
                m_LastColumnVisibles[i] = col.visible;
                if (col.name == TreeColumnNames.TreeColumnName)
                {
                    col.makeCell = MakeItem;
                    col.destroyCell = DestroyItem;
                    col.bindCell += BindName;
                }
                else
                {
                    col.makeCell = MakeLabel;
                    col.destroyCell = DestroyLabel;

                    if (col.name == TreeColumnNames.TreeColumnAddressedCount)
                        col.bindCell += BindAddressedCount;
                    else if (col.name == TreeColumnNames.TreeColumnType)
                        col.bindCell += BindType;
                    else if (col.name == TreeColumnNames.TreeColumnStatus)
                        col.bindCell += BindStatus;
                    else if (col.name == TreeColumnNames.TreeColumnPercentage)
                        col.bindCell += BindPercentage;
                    else if (col.name == TreeColumnNames.TreeColumnBundleSource)
                        col.bindCell += BindSource;
                    else if (col.name == TreeColumnNames.TreeColumnReferencedBy)
                        col.bindCell += BindReferencedBy;
                    else if (col.name == TreeColumnNames.TreeColumnReferencesTo)
                        col.bindCell += BindReferencesTo;
                }
            }

            m_TreeView.selectionChanged += TreeViewOnSelectionChanged;
        }

        public void Initialise(AddressablesProfilerDetailsDataInspector inspector)
        {
            m_DetailsInspector = inspector;
        }

        public void SetSortingChangeCallback(System.Action callback)
        {
            m_TreeView.columnSortingChanged += callback;
        }

        public SortColumnDescriptions SortDescriptions => m_TreeView.sortColumnDescriptions;

        private readonly MultiColumnTreeView m_TreeView;
        private int? m_SelectedId = null;

        private bool[] m_LastColumnVisibles;
        private readonly Queue<VisualElement> m_VisualsCache = new Queue<VisualElement>(32);
        private readonly Queue<Label> m_LabelCache = new Queue<Label>(32);
        private AddressablesProfilerDetailsDataInspector m_DetailsInspector;

        private void DestroyItem(VisualElement e)
        {
            m_VisualsCache.Enqueue(e);
        }

        private VisualElement MakeItem()
        {
            return m_VisualsCache.Count == 0 ? new AssetLabel(0, false) : m_VisualsCache.Dequeue();
        }

        private void DestroyLabel(VisualElement e)
        {
            m_LabelCache.Enqueue(e as Label);
        }

        private VisualElement MakeLabel()
        {
            Label lbl = m_LabelCache.Count == 0 ? new Label() : m_LabelCache.Dequeue();
            lbl.style.alignSelf = new StyleEnum<Align>(Align.FlexEnd);
            return lbl;
        }

        private void BindName(VisualElement element, int arg2)
        {
            ContentData contentData = m_TreeView.GetItemDataForIndex<ContentData>(arg2);
            (element as AssetLabel)?.SetContent(contentData);
        }

        private void BindType(VisualElement arg1, int arg2) {
            Bind(TreeColumnNames.TreeColumnType, arg1 as Label, arg2); }
        private void BindAddressedCount(VisualElement arg1, int arg2) {
            Bind(TreeColumnNames.TreeColumnAddressedCount, arg1 as Label, arg2); }
        private void BindReferencedBy(VisualElement arg1, int arg2) {
            Bind(TreeColumnNames.TreeColumnReferencedBy, arg1 as Label, arg2); }
        private void BindStatus(VisualElement arg1, int arg2) {
            Bind(TreeColumnNames.TreeColumnStatus, arg1 as Label, arg2); }
        private void BindPercentage(VisualElement arg1, int arg2) {
            Bind(TreeColumnNames.TreeColumnPercentage, arg1 as Label, arg2); }
        private void BindReferencesTo(VisualElement arg1, int arg2) {
            Bind(TreeColumnNames.TreeColumnReferencesTo, arg1 as Label, arg2); }

        private void Bind(string columnName, Label lbl, int itemIndex)
        {
            ContentData data = m_TreeView.GetItemDataForIndex<ContentData>(itemIndex);
            if (lbl == null)
                return;
            if (data == null)
            {
                lbl.text = "ERROR";
                return;
            }

            lbl.text = data.GetCellContent(columnName);
        }

        private void BindSource(VisualElement arg1, int arg2)
        {
            Label lbl = arg1 as Label;
            ContentData data = m_TreeView.GetItemDataForIndex<ContentData>(arg2);
            if (lbl == null || data == null )
            {
                lbl.text = "ERROR";
                return;
            }

            if (data is BundleData bData)
                lbl.text = bData.Source.ToString();
            else
                lbl.text = "";
        }

        public void SetRootItemsAndRebuild(IList<TreeViewItemData<ContentData>> rootItems)
        {
            m_TreeView.SetRootItems(rootItems);
            m_TreeView.Rebuild();

            if (m_SelectedId.HasValue)
            {
                var data = m_TreeView.GetItemDataForId<ContentData>(m_SelectedId.Value);
                if (data != null) // if data not found, then the item is not in the new frame
                    m_TreeView.SetSelectionById(m_SelectedId.Value);
                else
                    SingleSelection(null);
            }
        }

        private void TreeViewOnSelectionChanged(IEnumerable<object> obj)
        {
            // if m_TreeView.selectionType = SelectionType.Multiple, then can support multiple selection
            // currently static as Single
            foreach (object o in obj)
            {
                SingleSelection(o as ContentData);
                break;
            }
        }

        private void SingleSelection(ContentData selectedData)
        {
            m_DetailsInspector.SetSourceContent(selectedData);
            if (selectedData != null)
                m_SelectedId = selectedData.TreeViewID;
        }

        public void SaveColumnVisibility()
        {
            for (int i = 0; i < m_TreeView.columns.Count; ++i)
            {
                if (m_TreeView.columns[i].visible != m_LastColumnVisibles[i])
                {
                    m_LastColumnVisibles[i] = m_TreeView.columns[i].visible;
                    EditorPrefs.SetBool(k_ColumnVisibilityKeyPrefix + m_TreeView.columns[i].name, m_TreeView.columns[i].visible);
                }
            }
        }
    }
}

#endif
