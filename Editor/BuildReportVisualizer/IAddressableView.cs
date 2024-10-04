using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    interface IAddressableView
    {
        void CreateGUI(VisualElement rootVisualElement);
    }
}
