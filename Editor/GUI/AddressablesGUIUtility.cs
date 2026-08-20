using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.TextCore.Text;


namespace UnityEditor.AddressableAssets.GUI
{
    internal struct FoldoutSessionStateValue
    {
        bool? m_Value;
        private string m_Key;

        public FoldoutSessionStateValue(string key)
        {
            m_Value = null;
            m_Key = key;
        }

        public bool IsActive
        {
            get
            {
                if (string.IsNullOrEmpty(m_Key))
                    throw new NullReferenceException("FoldoutSessionStateValue does not have a valid key set");

                if (m_Value.HasValue == false)
                    m_Value = SessionState.GetBool(m_Key, true);
                return m_Value.Value;
            }
            set
            {
                m_Value = value;
                SessionState.SetBool(m_Key, value);
            }
        }
    }

    internal class AddressablesGUIUtility
    {
        private static Dictionary<string, FoldoutSessionStateValue> m_CachedSessionStates = new Dictionary<string, FoldoutSessionStateValue>();
        private static Dictionary<string, Texture2D> s_SchemaIconCache = new Dictionary<string, Texture2D>();

        internal static GUIStyle GetStyle(string styleName)
        {
            GUIStyle s = UnityEngine.GUI.skin.FindStyle(styleName);
            if (s == null)
                s = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
            if (s == null)
            {
                Debug.LogError("Missing built-in guistyle " + styleName);
                s = new GUIStyle();
            }

            return s;
        }

        internal static bool HasStyle(string styleName)
        {
            GUIStyle s = UnityEngine.GUI.skin.FindStyle(styleName);
            if (s == null)
                s = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
            if (s == null)
                return false;

            return true;
        }

        internal static string ConvertTextToStrikethrough(string value)
        {
            string str = "";
            foreach (char c in value)
                str = str + c + '\u0336';
            return str;
        }

        internal static string TruncateWithEllipsis(string text, float maxWidth, float approxCharWidth)
        {
            int maxChars = Mathf.FloorToInt(maxWidth / approxCharWidth);
            if (text.Length <= maxChars)
                return text;
            if (maxChars <= 3)
                return text.Substring(0, Mathf.Max(1, maxChars));
            return text.Substring(0, maxChars - 3) + "...";
        }

        internal static bool GetFoldoutValue(string stateKey)
        {
            if (m_CachedSessionStates.TryGetValue(stateKey, out var val))
                return val.IsActive;
            var foldoutState = new FoldoutSessionStateValue(stateKey);
            m_CachedSessionStates.Add(stateKey, foldoutState);
            return foldoutState.IsActive;
        }

        internal static void SetFoldoutValue(string stateKey, bool isActive)
        {
            if (m_CachedSessionStates.TryGetValue(stateKey, out var val))
            {
                val.IsActive = isActive;
                return;
            }

            var foldoutState = new FoldoutSessionStateValue(stateKey);
            foldoutState.IsActive = isActive;
            m_CachedSessionStates.Add(stateKey, foldoutState);
        }

        internal static float HeaderHeight = 20f;

        internal static void DrawDivider()
        {
            GUILayout.Space(1.5f);
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(2.5f));
            r.x = 0;
            r.width = EditorGUIUtility.currentViewWidth;
            r.height = 1;

            EditorGUI.DrawRect(r, HeaderBorderColor);
        }

        internal static Color HeaderBorderColor
        {
            get
            {
                float shade = EditorGUIUtility.isProSkin ? 26f / 255f : 0.6f;
                return new Color(shade, shade, shade, 1);
            }
        }

        internal static Color BottomBorderColor
        {
            get
            {
                float shade = EditorGUIUtility.isProSkin ? 48f / 255f : 205f / 255f;
                return new Color(shade, shade, shade, 1);
            }
        }


        internal static Color HeaderNormalColor
        {
            get
            {
                float shade = EditorGUIUtility.isProSkin ? 62f / 255f : 205f / 255f;
                return new Color(shade, shade, shade, 1);
            }
        }

        internal static Color HeaderHoverColor
        {
            get
            {
                float shade = EditorGUIUtility.isProSkin ? 70f / 255f : 215f / 255f;
                return new Color(shade, shade, shade, 1);
            }
        }

        public static bool FoldoutWithHelp(bool isActive, GUIContent content, Action helpAction = null)
        {
            Rect controlRect = EditorGUILayout.GetControlRect();
            GUIStyle iconStyle = UnityEngine.GUI.skin.FindStyle("IconButton") ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("IconButton");
            if (helpAction != null)
            {
                Rect helpRect = controlRect;
                helpRect.x = controlRect.x + controlRect.width - helpRect.height;
                helpRect.width = helpRect.height;
                if (UnityEngine.GUI.Button(helpRect, EditorGUIUtility.IconContent("_Help"), iconStyle))
                    helpAction.Invoke();
            }

            bool isPressedDown = controlRect.Contains(UnityEngine.Event.current.mousePosition)
                                 && UnityEngine.Event.current.type == UnityEngine.EventType.MouseDown
                                 && UnityEngine.Event.current.button == 0;
            if (isPressedDown)
            {
                isActive = !isActive;
                UnityEngine.Event.current.Use();
                UnityEngine.GUI.changed = true;
            }

            EditorGUI.Foldout(controlRect, isActive, content, false);
            return isActive;
        }

        public static bool DrawEnableButton(Rect enableButtonRect, AddressableAssetGroupSchema schema, AddressableAssetGroup groupTarget, AddressableAssetGroup[] groupTargets)
        {
            var schemaType = schema.GetType();
            var canEnableSchema = schema as ICanBeEnabled;
            if (canEnableSchema == null)
                return false;
            bool isEnabledValueToDisplay = canEnableSchema.IsEnabled;

            bool hasMixedValues = false;
            if (groupTargets.Length > 1)
            {
                foreach (var group in groupTargets)
                {
                    if (group != groupTarget && group.HasSchema(schemaType))
                    {
                        var otherSchema = group.GetSchema(schemaType) as ICanBeEnabled;
                        if (otherSchema != null && otherSchema.IsEnabled != isEnabledValueToDisplay)
                        {
                            hasMixedValues = true;
                            break;
                        }
                    }
                }
                EditorGUI.showMixedValue = hasMixedValues;
            }

            EditorGUI.BeginChangeCheck();
            bool newEnabled = EditorGUI.Toggle(enableButtonRect, GUIContent.none, isEnabledValueToDisplay);
            if (EditorGUI.EndChangeCheck())
            {
                // When toggling with mixed values, default to false.
                // This makes disabling large numbers of schemas at once easier.
                newEnabled = hasMixedValues ? false : newEnabled;
                Undo.RecordObject(schema, (newEnabled ? "Enable" : "Disable") + " Schema");
                canEnableSchema.IsEnabled = newEnabled;
                EditorUtility.SetDirty(schema);
                if (groupTargets.Length > 1)
                {
                    foreach (var group in groupTargets)
                    {
                        if (group != groupTarget && group.HasSchema(schemaType))
                        {
                            var groupSchema = group.GetSchema(schemaType);
                            var canBeEnabled = groupSchema as ICanBeEnabled;
                            if (canBeEnabled != null)
                            {
                                Undo.RecordObject(groupSchema, (newEnabled ? "Enable" : "Disable") + " Schema");
                                canBeEnabled.IsEnabled = newEnabled;
                                EditorUtility.SetDirty(groupSchema);
                            }
                        }
                    }
                }
            }
            EditorGUI.showMixedValue = false;
            return newEnabled;
        }

        /// <summary>
        /// Determines the icon type for a schema based on its type.
        /// </summary>
        /// <param name="schema">The schema to evaluate</param>
        /// <returns>The icon type to display</returns>
        internal static GroupIconType GetSchemaIconType(AddressableAssetGroupSchema schema)
        {
            if (schema == null) return GroupIconType.None;

            if (schema is BundledAssetGroupSchema)
                return GroupIconType.AssetBundle;
            if (schema is ContentDirectoryGroupSchema)
                return GroupIconType.ContentDirectory;

            return GroupIconType.None;
        }

        /// <summary>
        /// Gets the appropriate icon for a schema header based on schema type.
        /// </summary>
        /// <param name="schema">The schema to get the icon for</param>
        /// <returns>The icon texture, or null if no icon should be displayed</returns>
        internal static Texture2D GetSchemaIcon(AddressableAssetGroupSchema schema)
        {
            var iconType = GetSchemaIconType(schema);
            if (iconType == GroupIconType.None)
                return null;

            // For Inspector headers, always use non-selected state
            bool isDark = EditorGUIUtility.isProSkin;

            string path;
            if (iconType == GroupIconType.AssetBundle)
            {
                path = isDark
                    ? "Packages/com.unity.addressables/Editor/Icons/Groups Window/Dark Theme/Asset Bundle/d_AssetBundle.png"
                    : "Packages/com.unity.addressables/Editor/Icons/Groups Window/Light Theme/Asset Bundle/AssetBundle.png";
            }
            else // GroupIconType.ContentDirectory
            {
                path = isDark
                    ? "Packages/com.unity.addressables/Editor/Icons/Groups Window/Dark Theme/Content Directory/d_ContentDirectory.png"
                    : "Packages/com.unity.addressables/Editor/Icons/Groups Window/Light Theme/Content Directory/ContentDirectory.png";
            }

            // For high-DPI displays, try to load the @2x variant first
            string hiDpiPath = null;
            if (EditorGUIUtility.pixelsPerPoint > 1f)
            {
                hiDpiPath = path.Replace(".png", "@2x.png");
            }

            // Check cache first (use hi-DPI path as cache key if available)
            string cacheKey = hiDpiPath ?? path;
            if (s_SchemaIconCache.TryGetValue(cacheKey, out var cachedIcon))
                return cachedIcon;

            // Try to load hi-DPI variant first, fall back to base icon
            Texture2D icon = null;
            if (hiDpiPath != null)
            {
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>(hiDpiPath);
            }
            if (icon == null)
            {
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            // Cache the result (including null to prevent repeated disk access)
            s_SchemaIconCache[cacheKey] = icon;

            return icon;
        }

        public static bool BeginFoldoutHeaderGroupWithHelp(bool isActive, GUIContent content, Action helpAction = null, int indent = 0, Action<Rect> menuAction = null,
                                                           AddressableAssetGroupSchema schema = null, AddressableAssetGroup groupTarget = null, AddressableAssetGroup[] groupTargets = null)
        {
            Rect headerRect = EditorGUILayout.GetControlRect();
            headerRect.height = HeaderHeight;

            Rect bgRect = new Rect(headerRect);
            bgRect.x = 0;
            bgRect.width = EditorGUIUtility.currentViewWidth;
            bool isHover = bgRect.Contains(UnityEngine.Event.current.mousePosition);
            EditorGUI.DrawRect(bgRect, isHover ? HeaderHoverColor : HeaderNormalColor);

            bgRect.y = headerRect.y - 1;
            bgRect.height = 1;
            Color topColor = HeaderBorderColor;
            EditorGUI.DrawRect(bgRect, topColor);
            bgRect.y = headerRect.y + headerRect.height + 1;
            bgRect.height = 0.5f;
            Color bottomColor = BottomBorderColor;
            EditorGUI.DrawRect(bgRect, bottomColor);
            headerRect.y += 1;

            if (indent > 0)
            {
                headerRect.x += indent;
                headerRect.width -= indent;
            }

            GUIStyle iconStyle = UnityEngine.GUI.skin.FindStyle("IconButton") ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("IconButton");
            if (menuAction != null)
            {
                Rect menuButtonRect = headerRect;
                menuButtonRect.y = headerRect.y + 3;
                menuButtonRect.x = headerRect.x + headerRect.width - menuButtonRect.height;
                menuButtonRect.width = menuButtonRect.height;
                if (UnityEngine.GUI.Button(menuButtonRect, EditorGUIUtility.IconContent("_Menu"), iconStyle))
                    menuAction.Invoke(menuButtonRect);
            }

            if (helpAction != null)
            {
                Rect helpRect = headerRect;
                helpRect.y = headerRect.y + (HeaderHeight - 16f) / 2f;
                helpRect.x = headerRect.x + headerRect.width - helpRect.height;
                if (menuAction != null)
                    helpRect.x -= helpRect.height;
                helpRect.width = 16f;
                helpRect.height = 16f;
                if (UnityEngine.GUI.Button(helpRect, EditorGUIUtility.IconContent("_Help"), iconStyle))
                    helpAction.Invoke();
            }

            // Layout order: foldout arrow | icon | checkbox | label
            // All elements vertically centered in the header
            float currentX = headerRect.x + 7f;
            float verticalCenter = headerRect.y + (HeaderHeight - 16f) / 2f;

            // Get schema icon if available - draw BEFORE the checkbox
            Texture2D schemaIcon = schema != null ? GetSchemaIcon(schema) : null;
            bool schemaIsEnabled = true;
            Rect toggleRect = Rect.zero;

            if (schemaIcon != null)
            {
                Rect iconRect = new Rect(currentX, verticalCenter, 16f, 16f);
                EditorGUI.BeginDisabledGroup(schema is ICanBeEnabled canBeEnabled && !canBeEnabled.IsEnabled);
                UnityEngine.GUI.DrawTexture(iconRect, schemaIcon, ScaleMode.ScaleToFit);
                EditorGUI.EndDisabledGroup();
                currentX += 18f; // Icon width + padding
            }

            // Draw checkbox after icon - MUST be drawn before click detection so it can receive input
            if (schema != null && schema is ICanBeEnabled)
            {
                toggleRect = new Rect(currentX, verticalCenter, 16f, 16f);
                schemaIsEnabled = DrawEnableButton(toggleRect, schema, groupTarget, groupTargets);
                currentX += 18f; // Checkbox width + padding
            }

            // Handle foldout click - exclude the checkbox area to allow it to receive clicks
            bool isPressedDown = isHover && UnityEngine.Event.current.type == UnityEngine.EventType.MouseDown && UnityEngine.Event.current.button == 0;
            if (isPressedDown && !toggleRect.Contains(UnityEngine.Event.current.mousePosition))
            {
                isActive = !isActive;
                UnityEngine.Event.current.Use();
                UnityEngine.GUI.changed = true;
            }

            // Draw label - use full header height so text aligns naturally
            var labelRect = new Rect(currentX, headerRect.y, headerRect.width - (currentX - headerRect.x), HeaderHeight);
            EditorGUI.BeginDisabledGroup(!schemaIsEnabled);
            GUIStyle style = EditorStyles.boldLabel;
            EditorGUI.LabelField(labelRect, content, style);
            EditorGUI.EndDisabledGroup();
            EditorGUI.Foldout(headerRect, isActive, new GUIContent(), false);
            if (isActive)
                GUILayout.Space(7f);
            else
                GUILayout.Space(4f);
            return isActive;
        }

        /// <summary>
        /// Draws an error section with icon, word-wrapped message, and a clickable link, with a top divider.
        /// </summary>
        internal static void DrawErrorBoxWithLink(string message, string linkText, string url)
        {
            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    GUIContent iconContent = EditorGUIUtility.IconContent("console.erroricon");
                    if (iconContent != null && iconContent.image != null)
                        GUILayout.Label(iconContent, GUILayout.Width(20f), GUILayout.Height(20f));

                    EditorGUILayout.BeginVertical();
                    {
                        GUILayout.Label(message, EditorStyles.wordWrappedLabel);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button(linkText, EditorStyles.linkLabel))
                                Application.OpenURL(url);
                            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        internal static string CanEnableSchemaError(string groupName, Type thisSchemaType, Type otherSchemaType)
        {
            return $"Failed to enable schema \"{AddressableAssetUtility.GetCachedTypeDisplayName(thisSchemaType)}\" because group named \"{groupName}\" already has a schema of type \"{AddressableAssetUtility.GetCachedTypeDisplayName(otherSchemaType)}\" enabled. Disable one to resolve.";
        }
    }
}
