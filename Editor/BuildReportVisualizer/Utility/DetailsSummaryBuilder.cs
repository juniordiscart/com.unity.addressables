#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal class DetailsSummaryBuilder
    {
        List<VisualElement> m_Containers;

        public DetailsSummaryBuilder()
        {
            m_Containers = new List<VisualElement>();
        }

        public DetailsSummaryBuilder With(VisualElement element)
        {
            m_Containers.Add(element);
            return this;
        }

        public DetailsSummaryBuilder With(Texture icon, string value)
        {
            VisualElement container = new VisualElement();
            container.style.height = new Length(35f, LengthUnit.Pixel);
            container.style.flexDirection = FlexDirection.Row;
            container.style.paddingTop = new Length(10f, LengthUnit.Pixel);
            container.style.paddingBottom = new Length(5f, LengthUnit.Pixel);

            Label label = new Label();
            Image iconElement = new Image();
            iconElement.image = icon;
            iconElement.style.width = iconElement.style.height = 16;
            iconElement.style.minWidth = iconElement.style.minHeight = 16;

            container.Add(iconElement);
            container.Add(label);

            label.text = value;
            label.style.width = new Length(95f, LengthUnit.Percent);
            label.style.maxWidth = new Length(95f, LengthUnit.Percent);
            label.style.height = 16;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.overflow = Overflow.Hidden;
            RegisterCopyTextToClipboardCallback(label);

            m_Containers.Add(container);

            return this;
        }

        public DetailsSummaryBuilder With(string value)
        {
            VisualElement container = new VisualElement();
            container.style.height = new Length(20f, LengthUnit.Pixel);
            container.style.flexDirection = FlexDirection.Row;

            Label label = new Label();

            container.Add(label);

            label.text = value;
            label.style.width = new Length(100f, LengthUnit.Percent);
            label.style.maxWidth = new Length(100f, LengthUnit.Percent);
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.overflow = Overflow.Hidden;
            label.style.paddingBottom = new StyleLength(2f);
            RegisterCopyTextToClipboardCallback(label);

            m_Containers.Add(container);

            return this;
        }

        public DetailsSummaryBuilder With(string title, string value)
        {
            VisualElement container = new VisualElement();
            container.style.height = new Length(20f, LengthUnit.Pixel);
            container.style.flexDirection = FlexDirection.Row;

            Label lhs = new Label();
            Label rhs = new Label();

            container.Add(lhs);
            container.Add(rhs);

            lhs.text = title;
            lhs.style.width = new Length(50f, LengthUnit.Percent);
            lhs.style.maxWidth = new Length(50f, LengthUnit.Percent);
            lhs.style.unityTextAlign = TextAnchor.MiddleLeft;

            rhs.text = value;
            rhs.style.width = new Length(50f, LengthUnit.Percent);
            rhs.style.maxWidth = new Length(50f, LengthUnit.Percent);
            rhs.style.unityTextAlign = TextAnchor.MiddleRight;
            rhs.style.textOverflow = TextOverflow.Ellipsis;
            rhs.style.overflow = Overflow.Hidden;
            RegisterCopyTextToClipboardCallback(rhs);

            m_Containers.Add(container);

            return this;
        }

        public VisualElement Build()
        {
            var masterContainer = new VisualElement();

            foreach (var element in m_Containers)
            {
                masterContainer.Add(element);
                masterContainer.Add(BuildReportUtility.GetSeparatingLine());
            }

            return masterContainer;
        }

        void RegisterCopyTextToClipboardCallback(Label element)
        {
            element.RegisterCallback<ContextClickEvent>((args) =>
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Copy"), false, () =>
                {
                    GUIUtility.systemCopyBuffer = element.text;
                });

                menu.ShowAsContext();
            });
        }
    }
}
#endif
