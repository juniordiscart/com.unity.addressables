#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using System.IO;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEngine;
using UnityEngine.ResourceManagement.Profiling;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{

    internal class AssetLabel : VisualElement
    {
        public override VisualElement contentContainer
        {
            get
            {
                return m_Container;
            }
        }

        private VisualElement m_Container;
        private Image m_Icon;
        private Label m_Label;

        public int LabelMargin
        {
            set { m_Label.style.marginLeft = new StyleLength(value); }
        }

        public bool Wrap
        {
            set
            {
                m_Label.style.flexWrap = new StyleEnum<Wrap>( value ? UnityEngine.UIElements.Wrap.Wrap : UnityEngine.UIElements.Wrap.NoWrap);
                m_Label.style.flexShrink = new StyleFloat(1);
                m_Label.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
            }
        }

        public AssetLabel()
        {
            Initialise(4, false);
        }
        public AssetLabel(int margin, bool wrap)
        {
            Initialise(margin, wrap);
        }

        private void Initialise(int margin, bool wrap)
        {
            m_Container = new VisualElement();
            m_Container.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

            m_Icon = new Image();
            m_Icon.style.minWidth = new StyleLength(25);
            m_Icon.style.maxWidth = new StyleLength(25);
            m_Icon.style.maxHeight = new StyleLength(25);
            m_Container.Add(m_Icon);

            m_Label = new Label("");
            if (margin > 0)
                m_Label.style.marginLeft = new StyleLength(margin);
            m_Label.style.alignContent = new StyleEnum<Align>(Align.Center);
            m_Label.style.alignSelf = new StyleEnum<Align>(Align.Center);
            if (wrap)
            {
                m_Label.style.flexWrap = new StyleEnum<Wrap>(UnityEngine.UIElements.Wrap.Wrap);
                m_Label.style.flexShrink = new StyleFloat(1);
                m_Label.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
            }
            m_Container.Add(m_Label);

            hierarchy.Add(m_Container);
        }

        public new class UxmlFactory : UxmlFactory<AssetLabel, UxmlTraits> { }

        public void SetContent(ContentData contentData)
        {
            if (contentData is AssetData assetData)
            {
                string name = Path.GetFileName(assetData.AssetPath);
                bool isActive = assetData.Status != ContentStatus.Released && assetData.Status != ContentStatus.None;
                SetLabel(name, assetData.IsImplicit, isActive);
                SetIcon(assetData.MainAssetType);
            }
            else if (contentData is ObjectData objectData)
            {
                assetData = objectData.Parent as AssetData;
                bool isEnabled = assetData.Status != ContentStatus.Released && assetData.Status != ContentStatus.None;
                if (isEnabled && objectData.Status == ContentStatus.Released)
                    isEnabled = false;
                SetLabel(objectData.Name, assetData.IsImplicit, isEnabled);

                if (objectData.AssetType == AssetType.Component)
                    SetIcon(objectData.ComponentName);
                else
                    SetIcon(objectData.AssetType);
            }
            else if (contentData is BundleData)
            {
                SetLabel(contentData.Name, false, true);
                string textureName = EditorGUIUtility.isProSkin ? "d_Package Manager@2x" : "Package Manager@2x";
                SetIcon(EditorGUIUtility.IconContent(textureName).image as Texture2D);
            }
            else
            {
                SetLabel(contentData.Name, false, true);
                string textureName = EditorGUIUtility.isProSkin ? "d_FolderOpened Icon" : "FolderOpened Icon";
                SetIcon(EditorGUIUtility.IconContent(textureName).image as Texture2D);
            }
        }

        private void SetLabel(string name, bool italic, bool enabled)
        {
            m_Label.text = italic ? $"<i>{name}</i>" : name;
            m_Label.SetEnabled(enabled);
        }

        private void SetIcon(AssetType assetType)
        {
            SetIcon(ProfilerGUIUtilities.GetAssetIcon(assetType));
        }

        private void SetIcon(string componentName)
        {
            SetIcon(ProfilerGUIUtilities.GetComponentIcon(componentName));
        }

        private void SetIcon(Texture2D icon)
        {
            m_Icon.image = icon;
        }
    }
}
#endif
