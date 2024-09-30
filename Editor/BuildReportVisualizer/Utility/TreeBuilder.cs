#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal class TreeBuilder
    {
        MultiColumnTreeView m_TreeView;

        public TreeBuilder()
        {
            m_TreeView = new MultiColumnTreeView();
            m_TreeView.viewDataKey = $"tree-view-{GUID.Generate()}";
            m_TreeView.fixedItemHeight = 30f;
            m_TreeView.sortingEnabled = true;
            m_TreeView.autoExpand = false;
            m_TreeView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            m_TreeView.showBorder = true;
        }

        public TreeBuilder With(ContentViewColumnData column)
        {
            AddColumn(column);
            return this;
        }

        public TreeBuilder With(ContentViewColumnData[] columns)
        {
            foreach (var column in columns)
                AddColumn(column);

            return this;
        }

        void AddColumn(ContentViewColumnData column)
        {
            Column newColumn = new Column();
            if (column.Name.Contains("Name"))
            {
                newColumn.minWidth = new Length(150f, LengthUnit.Pixel);
                newColumn.width = Length.Auto();
            }
            else if (column.Name.Contains("Refs"))
            {
                newColumn.minWidth = new Length(60f, LengthUnit.Pixel);
                newColumn.width = new Length(60f, LengthUnit.Pixel);
            }
            else
                newColumn.minWidth = new Length(80f, LengthUnit.Pixel);
            newColumn.name = column.Name;
            newColumn.title = column.Title;
            newColumn.stretchable = true;
            newColumn.resizable = true;
            m_TreeView.columns.Add(newColumn);
            newColumn.makeCell = () => new Label();
            newColumn.bindCell = column.BindCellCallback;
        }

        public TreeBuilder With(Action<IEnumerable<object>> onSelectionCallback)
        {
            m_TreeView.selectionChanged += onSelectionCallback;
            return this;
        }

        public MultiColumnTreeView Build()
        {
            m_TreeView.Rebuild();
            return m_TreeView;
        }
    }
}
#endif
