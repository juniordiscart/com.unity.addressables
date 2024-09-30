#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.GUIElements;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.AddressableAssets.BuildReportVisualizer.BuildReportWindow;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal enum DetailsViewTab
    {
        ReferencesTo,
        ReferencedBy
    }

    class DetailsView : IAddressableView
    {
        DetailsViewTab m_ActiveContentsTab;
        VisualElement m_Root;
        BuildReportWindow m_Window;

        DetailsContentView m_Contents;
        DetailsSummaryView m_Summary;

        object m_DetailsRootObject;
        object m_DetailsActiveObject;

        internal DetailsView(BuildReportWindow window)
        {
            m_Window = window;
            m_ActiveContentsTab = DetailsViewTab.ReferencesTo;
        }

        public void CreateGUI(VisualElement rootVisualElement)
        {
            m_Root = rootVisualElement;

            m_Summary = new DetailsSummaryView(rootVisualElement, m_Window);
            m_Contents = new DetailsContentView(rootVisualElement, m_Window);

            rootVisualElement.Q<RibbonButton>("ReferencesToTab").clicked += () =>
            {
                m_ActiveContentsTab = DetailsViewTab.ReferencesTo;
                DetailsStack.Clear();

                DisplayContents(m_DetailsActiveObject);

            };

            rootVisualElement.Q<RibbonButton>("ReferencedByTab").clicked += () =>
            {
                m_ActiveContentsTab = DetailsViewTab.ReferencedBy;
                DetailsStack.Clear();

                DisplayContents(m_DetailsActiveObject);

            };
        }

        public void OnSelected(IEnumerable<object> items)
        {
            ClearGUI();
            DetailsStack.Clear();

            foreach (object item in items)
            {
                DisplayItemSummary(item);
                DisplayContents(item);
                m_DetailsRootObject = m_DetailsActiveObject = item;
            }
        }

        public void DisplayItemSummary(object item)
        {
            m_Summary.UpdateSummary(item);
        }

        public void DisplayContents(object contents)
        {
            m_Contents.DisplayContents(contents, m_ActiveContentsTab);
        }

        public void ClearGUI()
        {
            m_Summary.ClearSummary();
            m_Contents.ClearContents();
            DisplayContents(null);
        }
    }
}
#endif
