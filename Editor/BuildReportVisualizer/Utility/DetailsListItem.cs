#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal class DetailsListItem
    {
        public string Text;
        public string ImagePath;
        public string ButtonImagePath;
        public Action DrillDownEvent;
        public FontStyle StyleForText;
        public bool CanUseContextMenu;

        public DetailsListItem(string text, string pathForImage, Action drillDownEvent, string buttonImagePath, FontStyle style = FontStyle.Normal)
        {
            Text = text;
            StyleForText = style;
            ImagePath = pathForImage;
            DrillDownEvent = drillDownEvent;
            ButtonImagePath = buttonImagePath;
            CanUseContextMenu = true;
        }

        // Alternate Constructor used for items that will not have a context menu or buttons, e.g. the "other assets" drillable items
        public DetailsListItem(string text, string pathForImage)
        {
            Text = text;
            StyleForText = FontStyle.Normal;
            ImagePath = pathForImage;
            DrillDownEvent = null;
            ButtonImagePath = null;
            CanUseContextMenu = false;
        }
    }
}
#endif
