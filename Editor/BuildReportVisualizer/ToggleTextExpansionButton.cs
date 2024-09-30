#if UNITY_2022_2_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    public class ToggleTextExpansionButton
    {
        internal Button ToggleButton { get; set; }

        internal ToggleTextExpansionButton(VisualElement container, Length collapsedHeight)
        {
            ToggleButton = new Button();
            ToggleButton.userData = false;
            ToggleButton.text = "EYE";

            ToggleButton.clicked += () =>
            {
                if((bool)ToggleButton.userData)
                    container.style.maxHeight = new Length(100f, LengthUnit.Percent);
                else
                    container.style.maxHeight = collapsedHeight;

                ToggleButton.userData = !((bool)ToggleButton.userData);
            };
        }
    }
}
#endif
