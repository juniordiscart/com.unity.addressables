using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.GUIElements
{
    internal class VisualElementsWrapper
    {
        private VisualElement m_Root;
        public VisualElement Root => m_Root;

        private Dictionary<string, VisualElement> m_Elements = new Dictionary<string, VisualElement>();
        protected T GetElement<T>([CallerMemberName] string name = "") where T : VisualElement
        {
            VisualElement rtn;
            if (!m_Elements.TryGetValue(name, out rtn))
            {
                rtn = m_Root.Q(name);
                m_Elements[name] = rtn;
            }
            return rtn as T;
        }

        public VisualElementsWrapper(VisualElement rootView)
        {
            m_Root = rootView;
        }
    }
}
