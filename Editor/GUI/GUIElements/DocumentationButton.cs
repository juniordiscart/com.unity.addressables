using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.GUIElements
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
    internal partial class DocumentationButton : VisualElement
    {

        [UxmlAttribute]
        private string m_Text = "";
        [UxmlAttribute]
        private string m_Page = "index.html";
        // example page link: index.html or AddressableAssetSettings.html#profile

        private Button m_Button;
        private string m_URL;
        private bool m_Initialized = false;

        public DocumentationButton()
        {
            if (string.IsNullOrEmpty(m_Text))
                Initialise(m_Page);
            else
                Initialise(m_Page, m_Text);
            m_Initialized = true;
        }

        private void Initialise(string page)
        {
            InitButton(page);
            m_Button.style.minWidth = new StyleLength(17);
            m_Button.style.maxWidth = new StyleLength(17);
            m_Button.style.maxHeight = new StyleLength(17);
            m_Button.style.minHeight = new StyleLength(17);

            string textureName = EditorGUIUtility.isProSkin ? "d__Help@2x" : "_Help@2x";
            m_Button.style.backgroundImage = new StyleBackground(EditorGUIUtility.IconContent(textureName).image as Texture2D);

            contentContainer.Add(m_Button);
        }

        // this is called by the HelpDisplayManager
        public void Initialise(string page, string labelText)
        {
            if (m_Initialized)
                contentContainer.Remove(m_Button);
            InitButton(page);

            VisualElement icon = new VisualElement();
            icon.AddToClassList("link-icon");
            m_Button.Add(icon);

            Label label = new Label(labelText);
            label.AddToClassList("link-text");
            m_Button.Add(label);

            contentContainer.Add(m_Button);
        }

        private void InitButton(string page)
        {
            m_Button = new Button();
            m_Button.clicked += OpenDocumentation;
            SetDocumentation(page);

            m_Button.style.borderBottomWidth = new StyleFloat(0f);
            m_Button.style.borderTopWidth = new StyleFloat(0f);
            m_Button.style.borderLeftWidth = new StyleFloat(0f);
            m_Button.style.borderRightWidth = new StyleFloat(0f);
            m_Button.style.backgroundColor = new StyleColor(Color.clear);
            m_Button.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
        }

        private void SetDocumentation(string page)
        {
            m_URL = AddressableAssetUtility.GenerateDocsURL(page);
        }

        private void OpenDocumentation()
        {
            Application.OpenURL(m_URL);
        }
    }
#else
        using System.Collections.Generic;
        using UnityEngine;
        using UnityEngine.UIElements;


        internal class DocumentationButton : VisualElement
        {
            public new class UxmlFactory : UxmlFactory<DocumentationButton, UxmlTraits> { }

            public new class UxmlTraits : VisualElement.UxmlTraits
            {
                UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text", defaultValue = "" };
                UxmlStringAttributeDescription m_Page = new UxmlStringAttributeDescription { name = "page", defaultValue = "index.html" };
                // example page link: index.html or AddressableAssetSettings.html#profile

                public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
                {
                    get
                    {
                        yield return new UxmlChildElementDescription(typeof(DocumentationButton));
                    }
                }


                public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
                {
                    base.Init(ve, bag, cc);
                    string page = m_Page.GetValueFromBag(bag, cc);
                    string text = m_Text.GetValueFromBag(bag, cc);

                    if (string.IsNullOrEmpty(text))
                        ((DocumentationButton)ve).Initialise(page);
                    else
                        ((DocumentationButton)ve).Initialise(page, text);
                }
            }

            public override VisualElement contentContainer
            {
                get
                {
                    return m_Button;
                }
            }

            private Button m_Button;
            private string m_URL;

            public void Initialise(string page)
            {
                InitButton(page);
                m_Button.style.minWidth = new StyleLength(17);
                m_Button.style.maxWidth = new StyleLength(17);
                m_Button.style.maxHeight = new StyleLength(17);
                m_Button.style.minHeight = new StyleLength(17);

                string textureName = EditorGUIUtility.isProSkin ? "d__Help@2x" : "_Help@2x";
                m_Button.style.backgroundImage = new StyleBackground(EditorGUIUtility.IconContent(textureName).image as Texture2D);

                hierarchy.Add(m_Button);
            }

            public void Initialise(string page, string labelText)
            {
                InitButton(page);

                VisualElement icon = new VisualElement();
                icon.AddToClassList("link-icon");
                m_Button.Add(icon);

                Label label = new Label(labelText);
                label.AddToClassList("link-text");
                m_Button.Add(label);

                hierarchy.Add(m_Button);
            }

            private void InitButton(string page)
            {
                if (m_Button != null)
                    hierarchy.Remove(m_Button);
                m_Button = new Button();
                m_Button.clicked += OpenDocumentation;
                SetDocumentation(page);

                m_Button.style.borderBottomWidth = new StyleFloat(0f);
                m_Button.style.borderTopWidth = new StyleFloat(0f);
                m_Button.style.borderLeftWidth = new StyleFloat(0f);
                m_Button.style.borderRightWidth = new StyleFloat(0f);
                m_Button.style.backgroundColor = new StyleColor(Color.clear);
                m_Button.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            }

            private void SetDocumentation(string page)
            {
                m_URL = AddressableAssetUtility.GenerateDocsURL(page);
            }

            private void OpenDocumentation()
            {
                Application.OpenURL(m_URL);
            }
        }
#endif
}
