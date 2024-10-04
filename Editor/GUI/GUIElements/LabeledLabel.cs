using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.GUIElements
{
    internal class LabeledLabel : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<LabeledLabel, UxmlTraits> { }
        public override VisualElement contentContainer
        {
            get
            {
                return m_Container;
            }
        }

        private VisualElement m_Container;
        private Label m_Label;
        private Label m_Text;

        public bool Wrap
        {
            set
            {
                m_Text.style.flexWrap = new StyleEnum<Wrap>(value ? UnityEngine.UIElements.Wrap.Wrap : UnityEngine.UIElements.Wrap.NoWrap);
                m_Text.style.flexShrink = new StyleFloat(1);
                m_Text.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
            }
        }

        public string Text
        {
            set { m_Text.text = value; }
        }

        public string Label
        {
            set { m_Label.text = $"<b>{value}</b>";  }
        }

        public LabeledLabel()
        {
            Initialise("", "", true);
        }
        public LabeledLabel(string label, string text, bool wrap)
        {
            Initialise(label, text, wrap);
        }

        private void Initialise(string label, string text, bool wrap)
        {
            m_Container = new VisualElement();
            m_Container.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            m_Container.style.flexGrow = new StyleFloat(1);

           m_Label = new Label($"<b>{label}</b>");
           m_Label.style.width = new StyleLength(100);
           m_Label.style.minWidth = new StyleLength(100);
           m_Label.style.maxWidth = new StyleLength(100);
           if (wrap)
           {
               m_Label.style.flexWrap = new StyleEnum<Wrap>(UnityEngine.UIElements.Wrap.Wrap);
               m_Label.style.flexShrink = new StyleFloat(1);
               m_Label.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
           }

           m_Label.tooltip = label;
           m_Container.Add(m_Label);

            m_Text = new Label(text);
            m_Text.style.width = new StyleLength(StyleKeyword.Auto);
            if (wrap)
            {
                m_Text.style.flexWrap = new StyleEnum<Wrap>(UnityEngine.UIElements.Wrap.Wrap);
                m_Text.style.flexShrink = new StyleFloat(1);
                m_Text.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
            }
            m_Container.Add(m_Text);

            hierarchy.Add(m_Container);
        }
    }
}
