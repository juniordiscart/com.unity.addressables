using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.GUI
{
	class AddressableAssetsWindow : EditorWindow, IHasCustomMenu
	{
		private SearchRequest m_Request;
		private string m_HelpUrl;
        private const string k_WindowIconPathDark = "Packages/com.unity.addressables/Editor/Icons/Groups Window/Dark Theme/Addressables Window/d_AddressablesWindow.png";
        private const string k_WindowIconPathLight = "Packages/com.unity.addressables/Editor/Icons/Groups Window/Light Theme/Addressables Window/AddressablesWindow.png";
        private static Dictionary<string, Texture2D> s_WindowIconCache = new Dictionary<string, Texture2D>();

		[FormerlySerializedAs("m_groupEditor")]
		[SerializeField]
		internal AddressableAssetsSettingsGroupEditor m_GroupEditor;

		public AddressableAssetsSettingsGroupEditor GroupEditor
		{
			get => m_GroupEditor;
		}

		[MenuItem("Window/Asset Management/Addressables/Settings", priority = 2051)]
		internal static void ShowSettingsInspector()
		{
			var setting = AddressableAssetSettingsDefaultObject.Settings;
			if (setting == null)
			{
				Debug.LogWarning("Attempting to inspect default Addressables Settings, but no settings file exists.  Open 'Window/Asset Management/Addressables/Groups' for more info.");
			}
			else
			{
				EditorApplication.ExecuteMenuItem("Window/General/Inspector");
				EditorGUIUtility.PingObject(setting);
				Selection.activeObject = AddressableAssetSettingsDefaultObject.Settings;
			}
		}

        private static Texture2D GetWindowIcon()
        {
            bool isDark = EditorGUIUtility.isProSkin;
            string path = isDark ? k_WindowIconPathDark : k_WindowIconPathLight;

            // For high-DPI displays, try to load the @2x variant first
            string hiDpiPath = null;
            if (EditorGUIUtility.pixelsPerPoint > 1f)
            {
                hiDpiPath = path.Replace(".png", "@2x.png");
            }

            // Check cache first (use hi-DPI path as cache key if available)
            string cacheKey = hiDpiPath ?? path;
            if (s_WindowIconCache.TryGetValue(cacheKey, out var cachedIcon))
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
            s_WindowIconCache[cacheKey] = icon;
            return icon;
        }

		[MenuItem("Window/Asset Management/Addressables/Groups", priority = 2050)]
		internal static void Init()
		{
			AddressableAnalytics.ReportUsageEvent(AddressableAnalytics.UsageEventType.OpenGroupsWindow);
			var window = GetWindow<AddressableAssetsWindow>();
            window.titleContent = new GUIContent("Addressables Groups", GetWindowIcon());
			window.minSize = new Vector2(430, 250);
			window.Show();
		}

		public static Vector2 GetWindowPosition()
		{
			var window = GetWindow<AddressableAssetsWindow>();
			return new Vector2(window.position.x, window.position.y);
		}

		internal void SelectAssetsInGroupEditor(IList<AddressableAssetEntry> entries)
		{
			if (m_GroupEditor == null)
				m_GroupEditor = new AddressableAssetsSettingsGroupEditor(this);
			m_GroupEditor.SelectEntries(entries);
		}

        internal void SelectGroupInGroupEditor(AddressableAssetGroup group, bool fireSelectionChanged)
        {
            if (m_GroupEditor == null)
                m_GroupEditor = new AddressableAssetsSettingsGroupEditor(this);
            m_GroupEditor.SelectGroup(group, fireSelectionChanged);
        }

		public void OnEnable()
		{
			AddressableAnalytics.ReportUsageEvent(AddressableAnalytics.UsageEventType.OpenGroupsWindow, true);
            // Ensure the window icon is set (it may not persist across Editor restarts)
            titleContent = new GUIContent("Addressables Groups", GetWindowIcon());
			m_GroupEditor?.OnEnable();
			if (m_Request == null || m_Request.Status == StatusCode.Failure)
			{
				m_Request = PackageManager.Client.Search("com.unity.addressables");
			}

			AddressableAssetSettingsDefaultObject.onDefaultObjectsettingsChanged += ReloadWindow;
		}

		public void OnDisable()
		{
			m_GroupEditor?.OnDisable();

			AddressableAssetSettingsDefaultObject.onDefaultObjectsettingsChanged -= ReloadWindow;
		}

		internal void OfferToConvert(AddressableAssetSettings settings)
		{
			var bundleList = AssetDatabase.GetAllAssetBundleNames();
			if (settings != null && bundleList.Length > 0)
			{
				var displayChoice = EditorUtility.DisplayDialog("Legacy Bundles Detected",
					"We have detected the use of legacy bundles in this project.  Would you like to auto-convert those into Addressables? \nThis will take each asset bundle you have defined (we have detected " +
					bundleList.Length +
					" bundles), create an Addressables group with a matching name, then move all assets from those bundles into corresponding groups.  This will remove the asset bundle assignment from all assets, and remove all asset bundle definitions from this project.  This cannot be undone.",
					"Convert", "Ignore");
				if (displayChoice)
				{
					AddressableAssetUtility.ConvertAssetBundlesToAddressables();
				}
			}
		}

		public void OnGUI()
		{
			if (AddressableAssetSettingsDefaultObject.Settings == null)
			{
				GUILayout.Space(50);
				if (GUILayout.Button("Create Addressables Settings"))
				{
					m_GroupEditor = null;
					AddressableAssetSettingsDefaultObject.Settings = AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
						AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
					OfferToConvert(AddressableAssetSettingsDefaultObject.Settings);
				}

				GUILayout.Space(20);
				GUILayout.BeginHorizontal();
				GUILayout.Space(50);
				UnityEngine.GUI.skin.label.wordWrap = true;
				GUILayout.Label(
					"Click the \"Create\" button above or simply drag an asset into this window to start using Addressables.  Once you begin, the Addressables system will save some assets to your project to keep up with its data");
				GUILayout.Space(50);
				GUILayout.EndHorizontal();
				switch (Event.current.type)
				{
					case EventType.DragPerform:
						DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
						foreach (var path in DragAndDrop.paths)
						{
							if (AddressableAssetUtility.IsPathValidForEntry(path))
							{
								var guid = AssetDatabase.AssetPathToGUID(path);
								if (!string.IsNullOrEmpty(guid))
								{
									if (AddressableAssetSettingsDefaultObject.Settings == null)
										AddressableAssetSettingsDefaultObject.Settings = AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
											AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
									Undo.RecordObject(AddressableAssetSettingsDefaultObject.Settings, "AddressableAssetSettings");
									AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, AddressableAssetSettingsDefaultObject.Settings.DefaultGroup);
								}
							}
						}

						break;
					case EventType.DragUpdated:
					case EventType.DragExited:
						DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
						break;
				}
			}
			else
			{
				Rect contentRect = new Rect(0, 0, position.width, position.height);

				if (m_GroupEditor == null)
					m_GroupEditor = new AddressableAssetsSettingsGroupEditor(this);

				if (m_GroupEditor.OnGUI(contentRect))
					Repaint();
			}
		}

		public void AddItemsToMenu(GenericMenu menu)
		{
			if (m_Request != null && m_Request.Status == StatusCode.Success && m_Request.Result != null && m_Request.Result.Length == 1)
			{
				string[] parts = m_Request.Result[0].version.Split('.');
				if (parts.Length >= 2)
				{
					// Major & minor
					string vUrl = $"{parts[0]}.{parts[1]}";
					m_HelpUrl = $"https://docs.unity3d.com/Packages/com.unity.addressables@{vUrl}";
					menu.AddItem(new GUIContent("Help"), false, OnHelp);
				}
			}
		}

		void OnHelp()
		{
			if (!string.IsNullOrEmpty(m_HelpUrl))
			{
				Application.OpenURL(m_HelpUrl);
			}
		}

		private void ReloadWindow(AddressableAssetSettings settings)
		{
			if (settings == null)
			{
				m_GroupEditor = null;
			}
			else if (m_GroupEditor != null)
			{
				m_GroupEditor.Reload();
			}
		}
	}
}
