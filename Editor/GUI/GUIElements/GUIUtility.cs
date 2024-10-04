using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.GUIElements
{
    internal static class GUIUtility
    {
        public static readonly string OpenManualTooltip = L10n.Tr("Open the relevant documentation entry.");

        public const string HelpIconButtonClass = "icon-button__help-icon";
        public const string MenuIconButtonClass = "icon-button__menu-icon";

        public const string UIToolKitAssetsPath = "Packages/com.unity.addressables/Editor/GUI/GUIElements/";
        public const string UxmlFilesPath = UIToolKitAssetsPath + "UXML/";
        public const string StyleSheetsPath = UIToolKitAssetsPath + "StyleSheets/";

        public const string RibbonUxmlPath = UxmlFilesPath + "Ribbon.uxml";
        public const string RibbonUssPath = StyleSheetsPath + "Ribbon.uss";
        public const string RibbonDarkUssPath = StyleSheetsPath + "Ribbon_dark.uss";
        public const string RibbonLightUssPath = StyleSheetsPath + "Ribbon_light.uss";

        public static void SetVisibility(VisualElement element, bool visible)
        {
            SetElementDisplay(element, visible);
        }

        public static void SetElementDisplay(VisualElement element, bool value)
        {
            if (element == null)
                return;

            element.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            element.style.visibility = value ? Visibility.Visible : Visibility.Hidden;
        }

        public static VisualElement Clone(this VisualTreeAsset tree, VisualElement target = null, string styleSheetPath = null, Dictionary<string, VisualElement> slots = null)
        {
            var ret = tree.CloneTree();
            if (!string.IsNullOrEmpty(styleSheetPath))
                ret.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath));
            if (target != null)
                target.Add(ret);
            ret.style.flexGrow = 1f;
            return ret;
        }

        public static void SwitchClasses(this VisualElement element, string classToAdd, string classToRemove)
        {
            if (!element.ClassListContains(classToAdd))
                element.AddToClassList(classToAdd);
            element.RemoveFromClassList(classToRemove);
        }
    }
}
