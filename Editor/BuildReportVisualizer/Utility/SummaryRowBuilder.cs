#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.GUI;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal class PotentialIssuesCard
    {
        VisualElement m_Container;

        public PotentialIssuesCard(string text, Action viewButtonAction)
        {
            m_Container = new VisualElement();
            m_Container.style.width = m_Container.style.maxWidth = new Length(246f, LengthUnit.Pixel);
            m_Container.style.height = m_Container.style.maxHeight = new Length(140f, LengthUnit.Pixel);
            m_Container.style.backgroundColor = AddressablesGUIUtility.HeaderNormalColor;
            m_Container.style.flexDirection = FlexDirection.Row;

            Image icon = new Image();
            icon.image = BuildReportUtility.GetIcon("console.warnicon");
            icon.style.width = icon.style.height = new Length(24f, LengthUnit.Pixel);
            icon.style.paddingLeft = icon.style.paddingTop = new Length(6f, LengthUnit.Pixel);

            VisualElement textAndButton = new VisualElement();
            textAndButton.style.width = new Length(80f, LengthUnit.Percent);
            textAndButton.style.flexDirection = FlexDirection.Column;

            TextElement textAsset = new TextElement();
            textAsset.text = text;
            textAsset.style.paddingBottom = new Length(12f, LengthUnit.Pixel);
            textAsset.style.paddingTop = textAsset.style.paddingLeft = new Length(12f, LengthUnit.Pixel);
            textAsset.style.paddingRight = new Length(2f, LengthUnit.Pixel);
            textAndButton.Add(textAsset);

            Button viewButton = new Button(viewButtonAction);
            viewButton.style.maxWidth = new Length(50f, LengthUnit.Pixel);
            viewButton.text = "View";
            viewButton.style.paddingBottom = new Length(2f, LengthUnit.Pixel);
            textAndButton.Add(viewButton);

            m_Container.Add(icon);
            m_Container.Add(textAndButton);
        }

        public VisualElement Get()
        {
            return m_Container;
        }
    }

    internal class SummaryRowBuilder
    {
        Foldout m_Container;
        VisualElement m_TabRows;
        public SummaryRowBuilder(string title)
        {
            m_Container = new Foldout();
            m_Container.AddToClassList("SummaryTabBox");
            m_Container.text = title;

            m_TabRows = new VisualElement();
            m_TabRows.AddToClassList("SummaryTabRows");

            m_Container.Add(m_TabRows);
        }

        public SummaryRowBuilder With(string label, string value, FontStyle style = FontStyle.Normal)
        {
            VisualElement container = new VisualElement();
            var line = BuildReportUtility.GetSeparatingLine();
            line.style.width = new Length(100f, LengthUnit.Percent);
            container.Add(line);

            VisualElement tabRow = new VisualElement();
            tabRow.AddToClassList("SummaryTabRow");
            tabRow.style.width = new Length(100f, LengthUnit.Percent);

            Label lhs = new Label();
            lhs.text = label;
            lhs.style.flexWrap = Wrap.NoWrap;
            lhs.style.unityFontStyleAndWeight = style;
            lhs.style.paddingTop = new Length(2f, LengthUnit.Pixel);

            Label rhs = new Label();
            rhs.text = value;
            rhs.style.justifyContent = Justify.FlexEnd;
            rhs.style.maxWidth = new Length(80f, LengthUnit.Percent);
            rhs.style.maxHeight = new Length(20f, LengthUnit.Pixel);
            lhs.style.minHeight = new Length(20f, LengthUnit.Pixel);
            rhs.style.textOverflow = TextOverflow.Ellipsis;
            rhs.style.flexWrap = Wrap.NoWrap;
            rhs.style.paddingTop = new Length(2f, LengthUnit.Pixel);
            RegisterCopyTextToClipboardCallback(rhs);

            tabRow.Add(lhs);
            tabRow.Add(rhs);

            container.Add(tabRow);
            m_TabRows.Add(container);
            m_TabRows.style.minHeight = new Length(m_TabRows.childCount * 24f, LengthUnit.Pixel);
            m_TabRows.style.maxHeight = new Length(m_TabRows.childCount * 24f, LengthUnit.Pixel);
            m_Container.style.maxHeight = new Length((m_TabRows.childCount * 25f) + 15f, LengthUnit.Pixel);
            m_Container.style.minHeight = new Length((m_TabRows.childCount * 25f) + 15f, LengthUnit.Pixel);

            return this;
        }

        public SummaryRowBuilder With(params PotentialIssuesCard[] cards)
        {
            VisualElement container = new VisualElement();
            container.style.paddingBottom = new Length(4f, LengthUnit.Pixel);

            VisualElement tabRow = new VisualElement();
            tabRow.AddToClassList("SummaryTabRow");

            foreach(var card in cards)
                tabRow.Add(card.Get());

            container.Add(tabRow);
            m_TabRows.Add(container);
            m_Container.style.minHeight = new Length(180f, LengthUnit.Pixel);
            m_Container.style.maxHeight = new Length(180f, LengthUnit.Pixel);

            return this;
        }

        public Foldout Build()
        {
            return m_Container;
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
