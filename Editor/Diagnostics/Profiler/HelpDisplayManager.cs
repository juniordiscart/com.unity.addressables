#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class HelpDisplayManager
    {
        private class HelpElements
        {
            public HelpDisplay Elements;
            public ContentDataListView ContentList;
            public ContentDataTreeView ContentTree;
        }

        private Foldout m_HelpFoldout;
        private MultiColumnTreeView m_MainContentTree;

        private readonly Queue<HelpElements> m_HelpVisualsCache = new Queue<HelpElements>(32);
        private List<HelpElements> m_ActiveHelpDisplays = new List<HelpElements>();

        public void Initialise(InspectorPane rootView, MultiColumnTreeView treeView)
        {
            m_HelpFoldout = rootView.HelpFoldout;
            m_MainContentTree = treeView;
        }

        public void Clear()
        {
            List<HelpElements> removeElement = new List<HelpElements>(m_ActiveHelpDisplays);
            foreach (HelpElements activeHelpDisplay in removeElement)
                DestroyHelp(activeHelpDisplay);
            ProfilerGUIUtilities.Show(m_HelpFoldout);
        }

        public void HideIfEmpty()
        {
            if (m_ActiveHelpDisplays.Count == 0)
                ProfilerGUIUtilities.Hide(m_HelpFoldout);
        }

        private void DestroyHelp(HelpElements e)
        {
            m_HelpVisualsCache.Enqueue(e);
            m_HelpFoldout.Remove(e.Elements.Root);
            m_ActiveHelpDisplays.Remove(e);
        }

        public void MakeHelp(string helpText)
        {
            MakeHelp(helpText, new List<ContentData>(), null, null);
        }

        public void MakeHelp(string helpText, List<ContentData> addressedContent, string documentationPage = null, string documentationLabel = null)
        {
            HelpElements helpElement = InitialiseStandardHelpElements(helpText, documentationPage, documentationLabel);

            if (addressedContent != null && addressedContent.Count > 0)
            {
                if (helpElement.ContentList == null)
                {
                    ListView lv = new ListView();
                    lv.style.marginTop = new StyleLength(5);
                    helpElement.ContentList = new ContentDataListView();
                    helpElement.ContentList.Initialise(lv, m_MainContentTree);
                }
                helpElement.ContentList.SetParent(helpElement.Elements.MainContainer);
                helpElement.ContentList.SetSource(addressedContent);

                if (helpElement.ContentTree != null)
                    helpElement.ContentTree.SetParent(null);
            }
            else
            {
                if (helpElement.ContentList != null)
                    helpElement.ContentList.SetParent(null);
                if (helpElement.ContentTree != null)
                    helpElement.ContentTree.SetParent(null);
            }
        }

        public void MakeHelp(string helpText, List<ContentData[]> addressedContent, string documentationPage = null, string documentationLabel = null)
        {
            HelpElements helpElement = InitialiseStandardHelpElements(helpText, documentationPage, documentationLabel);

            if (addressedContent != null && addressedContent.Count > 0)
            {
                if (helpElement.ContentTree == null)
                {
                    TreeView lv = new TreeView();
                    lv.style.marginTop = new StyleLength(5);
                    helpElement.ContentTree = new ContentDataTreeView();
                    helpElement.ContentTree.Initialise(lv, m_MainContentTree);
                }
                helpElement.ContentTree.SetParent(helpElement.Elements.MainContainer);
                helpElement.ContentTree.SetSource(addressedContent);

                if (helpElement.ContentList != null)
                    helpElement.ContentList.SetParent(null);
            }
            else
            {
                if (helpElement.ContentList != null)
                    helpElement.ContentList.SetParent(null);
                if (helpElement.ContentTree != null)
                    helpElement.ContentTree.SetParent(null);
            }
        }

        private HelpElements InitialiseStandardHelpElements(string helpText, string documentationPage, string documentationLabel)
        {
            HelpElements helpElement = m_HelpVisualsCache.Count == 0
                ? new HelpElements
                {
                    Elements = HelpDisplay.Create(),
                    ContentList = null,
                    ContentTree = null
                }
                : m_HelpVisualsCache.Dequeue();

            helpElement.Elements.HelpLabel.text = helpText;
            if (m_ActiveHelpDisplays.Count == 0)
                helpElement.Elements.MainContainer.style.marginTop = new StyleLength(0f);
            else
                helpElement.Elements.MainContainer.style.marginTop = new StyleLength(10f);

            if (string.IsNullOrEmpty(documentationPage) || string.IsNullOrEmpty(documentationLabel))
            {
                ProfilerGUIUtilities.Hide(helpElement.Elements.DocumentationLink);
            }
            else
            {
                helpElement.Elements.DocumentationLink.Initialise(documentationPage, documentationLabel);
                ProfilerGUIUtilities.Show(helpElement.Elements.DocumentationLink);
            }

            m_HelpFoldout.Add(helpElement.Elements.Root);
            m_ActiveHelpDisplays.Add(helpElement);
            return helpElement;
        }
    }
}

#endif
