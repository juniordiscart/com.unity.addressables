using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Diagnostics;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;
#if (ENABLE_CCD && UNITY_2019_4_OR_NEWER)
using static UnityEditor.AddressableAssets.Build.CcdBuildEvents;
#endif

// ReSharper disable DelegateSubtraction

namespace UnityEditor.AddressableAssets.GUI
{
	[Serializable]
	internal class AddressableAssetsSettingsGroupEditor
	{
		[System.AttributeUsage(AttributeTargets.Class)]
        public class HideBuildMenuInUI : Attribute
        {
        }

		/// <summary>
		/// Interface used for classes that implement Addressables build menu steps.
		/// </summary>
		public interface IAddressablesBuildMenu
		{
			/// <summary>
			/// Path from Build in the Addressables Groups Window.
			/// </summary>
			string BuildMenuPath { get; }

			/// <summary>
			/// If returns true, build menu will extend to available Build Scripts.
			/// </summary>
			bool SelectableBuildScript { get; }

			/// <summary>
			/// Display order in the menu, lower values are displayed first.
			/// </summary>
			int Order { get; }

			/// <summary>
			/// Called before beginning the Addressables content build.
			/// </summary>
			/// <param name="input">Input used for the Addressables content build</param>
			/// <returns>True for success, else false and fail the build</returns>
			bool OnPrebuild(AddressablesDataBuilderInput input);

			/// <summary>
			/// Called after the Addressables content build if the build was successful.
			/// </summary>
			/// <param name="input">Input used for the Addressables content build</param>
			/// <param name="result">Result of the Addressables content build</param>
			/// <returns>True for success, else false and fail the build</returns>
			bool OnPostbuild(AddressablesDataBuilderInput input, AddressablesPlayerBuildResult result);
		}

		internal struct BuildMenuContext
		{
			public IAddressablesBuildMenu BuildMenu { get; set; }
			public int buildScriptIndex;
			public AddressableAssetSettings Settings { get; set; }
		}

		[FormerlySerializedAs("treeState")]
		[SerializeField]
		TreeViewState m_TreeState;

		[FormerlySerializedAs("mchs")]
		[SerializeField]
		MultiColumnHeaderState m_Mchs;

		internal AddressableAssetEntryTreeView m_EntryTree;

		public AddressableAssetsWindow window;

		SearchField m_SearchField;
		const int k_SearchHeight = 20;

		AddressableAssetSettings m_Settings;

		internal AddressableAssetSettings settings
		{
			get
			{
				if (m_Settings == null)
				{
					m_Settings = AddressableAssetSettingsDefaultObject.Settings;
				}

				return m_Settings;
			}
			set => m_Settings = value;
		}

		bool m_ResizingVerticalSplitter;
		Rect m_VerticalSplitterRect = new Rect(0, 0, 10, k_SplitterWidth);

		[SerializeField]
		float m_VerticalSplitterPercent;

		const int k_SplitterWidth = 3;


		public AddressableAssetsSettingsGroupEditor(AddressableAssetsWindow w)
		{
			window = w;
			m_VerticalSplitterPercent = 0.8f;
			OnEnable();
		}

		public void SelectEntries(IList<AddressableAssetEntry> entries)
		{
			List<int> selectedIDs = new List<int>(entries.Count);
			Stack<AssetEntryTreeViewItem> items = new Stack<AssetEntryTreeViewItem>();

			if (m_EntryTree == null || m_EntryTree.Root == null)
				InitialiseEntryTree();

			foreach (TreeViewItem item in m_EntryTree.Root.children)
			{
				if (item is AssetEntryTreeViewItem i)
					items.Push(i);
			}

			while (items.Count > 0)
			{
				var i = items.Pop();

				bool contains = false;
				if (i.entry != null)
				{
					foreach (AddressableAssetEntry entry in entries)
					{
						// class instances can be different but refer to the same entry, use guid
						if (entry.guid == i.entry.guid && i.entry.TargetAsset == entry.TargetAsset)
						{
							contains = true;
							break;
						}
					}
				}

				if (!i.IsGroup && contains)
				{
					selectedIDs.Add(i.id);
				}
				else if (i.hasChildren)
				{
					foreach (TreeViewItem child in i.children)
					{
						if (child is AssetEntryTreeViewItem c)
							items.Push(c);
					}
				}
			}

			foreach (int i in selectedIDs)
				m_EntryTree.FrameItem(i);
			m_EntryTree.SetSelection(selectedIDs);
		}

        public void SelectGroup(AddressableAssetGroup group, bool fireSelectionChanged)
        {
            Stack<AssetEntryTreeViewItem> items = new Stack<AssetEntryTreeViewItem>();

            if (m_EntryTree == null || m_EntryTree.Root == null)
                InitialiseEntryTree();

            foreach (TreeViewItem item in m_EntryTree.Root.children)
            {
                if (item is AssetEntryTreeViewItem i)
                    items.Push(i);
            }

            while (items.Count > 0)
            {
                AssetEntryTreeViewItem item = items.Pop();

                if (item.IsGroup && item.group.Guid == group.Guid)
                {
                    m_EntryTree.FrameItem(item.id);
                    var selectedIds = new List<int>(){ item.id };
                    if (fireSelectionChanged)
                        m_EntryTree.SetSelection(selectedIds, TreeViewSelectionOptions.FireSelectionChanged);
                    else
                        m_EntryTree.SetSelection(selectedIds);
                    return;
                }
                else if (!string.IsNullOrEmpty(item.folderPath) && item.hasChildren)
                {
                    foreach (TreeViewItem child in item.children)
                    {
                        if (child is AssetEntryTreeViewItem c)
                            items.Push(c);
                    }
                }
            }
        }

		void OnSettingsModification(AddressableAssetSettings s, AddressableAssetSettings.ModificationEvent e, object o)
		{
			if (m_EntryTree == null)
				return;

			switch (e)
			{
				case AddressableAssetSettings.ModificationEvent.GroupAdded:
				case AddressableAssetSettings.ModificationEvent.GroupRemoved:
				case AddressableAssetSettings.ModificationEvent.EntryAdded:
				case AddressableAssetSettings.ModificationEvent.EntryMoved:
				case AddressableAssetSettings.ModificationEvent.EntryRemoved:
				case AddressableAssetSettings.ModificationEvent.GroupRenamed:
				case AddressableAssetSettings.ModificationEvent.EntryModified:
				case AddressableAssetSettings.ModificationEvent.BatchModification:
					m_EntryTree.Reload();
					if (window != null)
						window.Repaint();
					break;
			}
		}

		GUIStyle GetStyle(string styleName)
		{
			GUIStyle s = UnityEngine.GUI.skin.FindStyle(styleName);

			if (s == null)
				s = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);

			if (s == null)
			{
				Addressables.LogError("Missing built-in guistyle " + styleName);
				s = new GUIStyle();
			}

			return s;
		}

		[NonSerialized]
		List<GUIStyle> m_SearchStyles;

		[NonSerialized]
		GUIStyle m_ButtonStyle;

		[NonSerialized]
		Texture2D m_CogIcon;

        void TopToolbar(Rect toolbarPos)
        {
            if (m_SearchStyles == null)
            {
                m_SearchStyles = new List<GUIStyle>();

                string toolbarSearchTextField = "ToolbarSeachTextFieldPopup";
                string toolbarSearchCancelButton = "ToolbarSeachCancelButton";
                string toolbarSearchCancelButtonEmpty = "ToolbarSeachCancelButtonEmpty";

                if(!AddressablesGUIUtility.HasStyle(toolbarSearchTextField))
                {
                    toolbarSearchTextField = "ToolbarSearchTextFieldPopup";
                    toolbarSearchCancelButton = "ToolbarSearchCancelButton";
                    toolbarSearchCancelButtonEmpty = "ToolbarSearchCancelButtonEmpty";
                }

                m_SearchStyles.Add(GetStyle(toolbarSearchTextField)); //GetStyle("ToolbarSearchTextField");
                m_SearchStyles.Add(GetStyle(toolbarSearchCancelButton));
                m_SearchStyles.Add(GetStyle(toolbarSearchCancelButtonEmpty));

            }

			if (m_ButtonStyle == null)
				m_ButtonStyle = GetStyle("ToolbarButton");
			if (m_CogIcon == null)
				m_CogIcon = EditorGUIUtility.FindTexture("_Popup");


			GUILayout.BeginArea(new Rect(0, 0, toolbarPos.width, k_SearchHeight));

			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				float spaceBetween = 4f;


				{
					var guiMode = new GUIContent("New", "Create a new group");
					Rect rMode = GUILayoutUtility.GetRect(guiMode, EditorStyles.toolbarDropDown);
					if (EditorGUI.DropdownButton(rMode, guiMode, FocusType.Passive, EditorStyles.toolbarDropDown))
					{
						var menu = new GenericMenu();
						foreach (var templateObject in settings.GroupTemplateObjects)
						{
							if (templateObject != null)
								menu.AddItem(new GUIContent(templateObject.name), false, m_EntryTree.CreateNewGroup, templateObject);
						}

						menu.AddSeparator(string.Empty);
						menu.AddItem(new GUIContent("Blank (no schema)"), false, m_EntryTree.CreateNewGroup, null);
						menu.DropDown(rMode);
					}
				}

				if (toolbarPos.width > 430)
					CreateProfileDropdown();

				{
					var guiMode = new GUIContent("Tools", "Tools used to configure or analyze Addressable Assets");
					Rect rMode = GUILayoutUtility.GetRect(guiMode, EditorStyles.toolbarDropDown);
					if (EditorGUI.DropdownButton(rMode, guiMode, FocusType.Passive, EditorStyles.toolbarDropDown))
					{
						var menu = new GenericMenu();
						menu.AddItem(new GUIContent("Inspect System Settings"), false, () =>
						{
							EditorApplication.ExecuteMenuItem("Window/General/Inspector");
							EditorGUIUtility.PingObject(AddressableAssetSettingsDefaultObject.Settings);
							Selection.activeObject = AddressableAssetSettingsDefaultObject.Settings;
						});
						menu.AddItem(new GUIContent("Check for Content Update Restrictions"), false, OnPrepareUpdate);

						menu.AddItem(new GUIContent("Window/Profiles"), false, () => EditorWindow.GetWindow<ProfileWindow>().Show(true));
						menu.AddItem(new GUIContent("Window/Labels"), false, () => EditorWindow.GetWindow<LabelWindow>(true).Intialize(settings));
						menu.AddItem(new GUIContent("Window/Analyze"), false, AnalyzeWindow.ShowWindow);
						menu.AddItem(new GUIContent("Window/Hosting Services"), false, () => EditorWindow.GetWindow<HostingServicesWindow>().Show(settings));
						menu.AddItem(new GUIContent("Window/Event Viewer"), false, ResourceProfilerWindow.ShowWindow);

#if UNITY_2022_2_OR_NEWER
                        menu.AddItem(new GUIContent("Window/Addressables Report"), false, BuildReportVisualizer.BuildReportWindow.ShowWindow);
#endif

                        menu.AddItem(new GUIContent("Groups View/Show Sprite and Subobject Addresses"), ProjectConfigData.ShowSubObjectsInGroupView, () =>
                        {
                            ProjectConfigData.ShowSubObjectsInGroupView = !ProjectConfigData.ShowSubObjectsInGroupView;
                            m_EntryTree.Reload();
                        });
                        menu.AddItem(
                            new GUIContent("Groups View/Group Hierarchy with Dashes",
                                "If enabled, group names are parsed as if a '-' represented a child in hierarchy.  So a group called 'a-b-c' would be displayed as if it were in a folder called 'b' that lived in a folder called 'a'.  In this mode, only groups without '-' can be rearranged within the groups window."),
                            ProjectConfigData.ShowGroupsAsHierarchy, () =>
                            {
                                ProjectConfigData.ShowGroupsAsHierarchy = !ProjectConfigData.ShowGroupsAsHierarchy;
                                m_EntryTree.Reload();
                            });

						menu.AddItem(new GUIContent("Groups View/Sort Groups"), false, OnSortGroups);
						menu.AddItem(new GUIContent("Groups View/Reload"), false, Reload);

						var bundleList = AssetDatabase.GetAllAssetBundleNames();
						if (bundleList != null && bundleList.Length > 0)
							menu.AddItem(new GUIContent("Convert Legacy AssetBundles"), false, () => window.OfferToConvert(AddressableAssetSettingsDefaultObject.Settings));

						menu.DropDown(rMode);
					}
				}

				GUILayout.FlexibleSpace();
				if (toolbarPos.width > 300)
					GUILayout.Space((spaceBetween * 2f) + 8);

				{
					string playmodeButtonName = toolbarPos.width < 300 ? "Play Mode" : "Play Mode Script";
					var guiMode = new GUIContent(playmodeButtonName, "Determines how the Addressables system loads assets in Play Mode");
					Rect rMode = GUILayoutUtility.GetRect(guiMode, EditorStyles.toolbarDropDown);
					if (EditorGUI.DropdownButton(rMode, guiMode, FocusType.Passive, EditorStyles.toolbarDropDown))
					{
						var menu = new GenericMenu();
						for (int i = 0; i < settings.DataBuilders.Count; i++)
						{
							var m = settings.GetDataBuilder(i);
							if (m.CanBuildData<AddressablesPlayModeBuildResult>())
							{
                                string text = m is Build.DataBuilders.BuildScriptPackedPlayMode
                                    ? $"Use Existing Build ({PlatformMappingService.GetAddressablesPlatformPathInternal(EditorUserBuildSettings.activeBuildTarget)})"
                                    : m.Name;
								menu.AddItem(new GUIContent(text), i == settings.ActivePlayModeDataBuilderIndex, OnSetActivePlayModeScript, i);
							}
						}

						menu.DropDown(rMode);
					}
				}

				var guiBuild = new GUIContent("Build", "Options for building Addressable Assets");
				Rect rBuild = GUILayoutUtility.GetRect(guiBuild, EditorStyles.toolbarDropDown);
				if (EditorGUI.DropdownButton(rBuild, guiBuild, FocusType.Passive, EditorStyles.toolbarDropDown))
				{
					var types = AddressableAssetUtility.GetTypes<IAddressablesBuildMenu>();
					var genericDropdownMenu = new GenericMenu();
					var displayMenus = CreateBuildMenus(types);
					foreach (IAddressablesBuildMenu buildOption in displayMenus)
					{
						if (buildOption.SelectableBuildScript)
						{
							bool addressablesPlayerBuildResultBuilderExists = false;
							for (int i = 0; i < settings.DataBuilders.Count; i++)
							{
								var dataBuilder = settings.GetDataBuilder(i);
								if (dataBuilder.CanBuildData<AddressablesPlayerBuildResult>())
								{
									addressablesPlayerBuildResultBuilderExists = true;
									BuildMenuContext context = new BuildMenuContext()
									{
                                        buildScriptIndex = i,
										BuildMenu = buildOption,
										Settings = settings
									};

									genericDropdownMenu.AddItem(new GUIContent(buildOption.BuildMenuPath + "/" + dataBuilder.Name), false, OnBuildAddressables, context);
								}
							}

							if (!addressablesPlayerBuildResultBuilderExists)
								genericDropdownMenu.AddDisabledItem(new GUIContent(buildOption.BuildMenuPath + "/No Build Script Available"));
						}
						else
						{
							BuildMenuContext context = new BuildMenuContext()
							{ buildScriptIndex = -1, BuildMenu = buildOption, Settings = settings };
							genericDropdownMenu.AddItem(new GUIContent(buildOption.BuildMenuPath), false, OnBuildAddressables, context);
						}
					}

					genericDropdownMenu.AddSeparator("");
					genericDropdownMenu.AddItem(new GUIContent("Clear Build Cache/All"), false, OnCleanAll);
					genericDropdownMenu.AddItem(new GUIContent("Clear Build Cache/Content Builders/All"), false, OnCleanAddressables, null);
					for (int i = 0; i < settings.DataBuilders.Count; i++)
					{
						var m = settings.GetDataBuilder(i);
						genericDropdownMenu.AddItem(new GUIContent("Clear Build Cache/Content Builders/" + m.Name), false, OnCleanAddressables, m);
					}

                    genericDropdownMenu.AddItem(new GUIContent("Clear Build Cache/Build Pipeline Cache"), false, OnCleanSBP, true);
					genericDropdownMenu.DropDown(rBuild);
				}

#if (ENABLE_CCD && UNITY_2019_4_OR_NEWER)
                var guiBuildToCcd = new GUIContent("Build to CCD", "Options for building Addressable Assets");
                Rect rBuildToCcd = GUILayoutUtility.GetRect(guiBuildToCcd, EditorStyles.toolbarDropDown);
                if (EditorGUI.DropdownButton(rBuildToCcd, guiBuildToCcd, FocusType.Passive, EditorStyles.toolbarDropDown))
				{
                    var types = AddressableAssetUtility.GetTypes<IAddressablesBuildMenu>();
                    var genericDropdownMenu = new GenericMenu();
                    var displayMenus = CreateBuildMenus(types);
                    foreach (IAddressablesBuildMenu buildOption in displayMenus)
                    {
                        if (buildOption.SelectableBuildScript)
                        {
                            bool addressablesPlayerBuildResultBuilderExists = false;
                            for (int i = 0; i < settings.DataBuilders.Count; i++)
                            {
                                var dataBuilder = settings.GetDataBuilder(i);
                                if (dataBuilder.CanBuildData<AddressablesPlayerBuildResult>())
                                {
                                    addressablesPlayerBuildResultBuilderExists = true;
                                    BuildMenuContext context = new BuildMenuContext()
                                    {
                                        buildScriptIndex = i,
                                        BuildMenu = buildOption,
                                        Settings = settings
                                    };

                                    genericDropdownMenu.AddItem(new GUIContent(dataBuilder.Name), false, OnBuildCcd, context);
                                }
                            }

                            if (!addressablesPlayerBuildResultBuilderExists)
                                genericDropdownMenu.AddDisabledItem(new GUIContent("No Build Script Available"));
                        }
                        else
                        {
                            BuildMenuContext context = new BuildMenuContext()
                            { buildScriptIndex = -1, BuildMenu = buildOption, Settings = settings };
                            genericDropdownMenu.AddItem(new GUIContent(buildOption.BuildMenuPath), false, OnBuildCcd, context);
                        }
                    }
                    genericDropdownMenu.DropDown(rBuildToCcd);
				}
#endif

				GUILayout.Space(4);
				Rect searchRect = GUILayoutUtility.GetRect(0, toolbarPos.width * 0.6f, 16f, 16f, m_SearchStyles[0], GUILayout.MinWidth(65), GUILayout.MaxWidth(300));
				Rect popupPosition = searchRect;
				popupPosition.width = 20;

				if (Event.current.type == EventType.MouseDown && popupPosition.Contains(Event.current.mousePosition))
				{
					var menu = new GenericMenu();
					menu.AddItem(new GUIContent("Hierarchical Search"), ProjectConfigData.HierarchicalSearch, OnHierSearchClick);
					menu.DropDown(popupPosition);
				}
				else
				{
					var baseSearch = ProjectConfigData.HierarchicalSearch ? m_EntryTree.customSearchString : m_EntryTree.searchString;
					var searchString = m_SearchField.OnGUI(searchRect, baseSearch, m_SearchStyles[0], m_SearchStyles[1], m_SearchStyles[2]);
					if (baseSearch != searchString)
					{
						m_EntryTree?.Search(searchString);
					}
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}

		internal static List<IAddressablesBuildMenu> CreateBuildMenus(IList<Type> types, bool includeMenusHiddenFromUI = false)
		{
			List<IAddressablesBuildMenu> displayMenus = new List<IAddressablesBuildMenu>(types.Count);

			foreach (Type menuType in types)
			{
				if (Attribute.GetCustomAttribute(menuType, typeof(HideBuildMenuInUI)) != null && !includeMenusHiddenFromUI)
					continue;

				var menuInst = Activator.CreateInstance(menuType) as IAddressablesBuildMenu;
				if (string.IsNullOrEmpty(menuInst.BuildMenuPath))
					continue;
				var existing = displayMenus.Find(b => b.BuildMenuPath == menuInst.BuildMenuPath);
				if (existing == null)
					displayMenus.Add(menuInst);
				else
				{
					var existingType = existing.GetType();
					if (menuType.IsSubclassOf(existingType))
					{
						// override existing with our current
						displayMenus.Remove(existing);
						displayMenus.Add(menuInst);
					}
					else if (!existingType.IsSubclassOf(menuType))
					{
						// both are same level, issue
						Addressables.LogWarning(
							$"Trying to new build menu [{menuType}] with path \"{menuInst.BuildMenuPath}\". But an existing type already exists with that path, [{existingType}].");
					}
				}
			}

			displayMenus.Sort((a, b) => a.Order.CompareTo(b.Order));
			return displayMenus;
		}

		private static void OnBuildAddressables(object ctx)
		{
			BuildMenuContext buildAddressablesContext = (BuildMenuContext)ctx;
			OnBuildAddressables(buildAddressablesContext);
		}

		internal static void OnBuildAddressables(BuildMenuContext context)
		{
			if (context.BuildMenu == null)
			{
				Addressables.LogError("Addressable content build failure : null build menu context");
				return;
			}

			if (context.buildScriptIndex >= 0)
				context.Settings.ActivePlayerDataBuilderIndex = context.buildScriptIndex;

			var builderInput = new AddressablesDataBuilderInput(context.Settings);

			if (!HandlePreBuild(context, builderInput))
				return;

			AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult rst, builderInput);

			HandlePostBuild(context, builderInput, rst);
		}

#if (ENABLE_CCD && UNITY_2019_4_OR_NEWER)
        private static void OnBuildCcd(object ctx)
        {
            BuildMenuContext buildAddressableContext = (BuildMenuContext)ctx;
            OnBuildCcd(buildAddressableContext);
        }

        private static async void OnBuildCcd(BuildMenuContext context)
        {
            bool isUpdate = false; 
            PreEvent preEvent = null;
            PostEvent postEvent = null;
            try {
                if (context.BuildMenu == null)
                {
                    Addressables.LogError("Addressable content build failure : null build menu context");
                    return;
                }

                if (context.buildScriptIndex >= 0)
                    context.Settings.ActivePlayerDataBuilderIndex = context.buildScriptIndex;

                isUpdate = context.BuildMenu is AddressablesBuildMenuUpdateAPreviousBuild;
                RegisterBuildMenuEvents(context, isUpdate, out preEvent, out postEvent);

                await AddressableAssetSettings.BuildAndReleasePlayerContent(isUpdate);
            } finally {
                UnregisterBuildMenuEvents(isUpdate, preEvent, postEvent);
                EditorUtility.ClearProgressBar();                               
            }
        }

        static void RegisterBuildMenuEvents(BuildMenuContext context, bool isUpdate, out PreEvent preEvent, out PostEvent postEvent) {
            preEvent = GetHandlePreBuildDelegate(context);
            postEvent = GetHandlePostBuildDelegate(context);
            if (isUpdate)
            {
                CcdBuildEvents.OnPreUpdateEvents += preEvent;
                CcdBuildEvents.PrependPostUpdateEvent(postEvent);
                return;
            }
            CcdBuildEvents.OnPreBuildEvents += preEvent;
            CcdBuildEvents.PrependPostBuildEvent(postEvent);
        }

        static void UnregisterBuildMenuEvents(bool isUpdate, PreEvent preEvent, PostEvent postEvent)
        {
            if (preEvent == null || postEvent == null)
            {
                return;
            }
            if (isUpdate)
            {
                CcdBuildEvents.OnPreUpdateEvents -= preEvent;
                CcdBuildEvents.OnPostUpdateEvents -= postEvent;
                return;
            }
            CcdBuildEvents.OnPreBuildEvents -= preEvent;
            CcdBuildEvents.OnPostBuildEvents -= postEvent;
        }


        static PreEvent GetHandlePreBuildDelegate(BuildMenuContext context)
        {
            return input =>
            {
                return Task.FromResult(HandlePreBuild(context, input));
            };
        }

        static PostEvent GetHandlePostBuildDelegate(BuildMenuContext context)
        {
            return (input, result) =>
            {
                HandlePostBuild(context, input, result);
                return Task.FromResult(true);
            };
        }

#endif

		static bool HandlePreBuild(BuildMenuContext context, AddressablesDataBuilderInput builderInput)
		{
			if (!context.BuildMenu.OnPrebuild(builderInput))
			{
				Addressables.LogError($"Addressable content pre-build failure : {context.BuildMenu.BuildMenuPath}");
				return false;
			}

			return true;
		}

		static void HandlePostBuild(BuildMenuContext context, AddressablesDataBuilderInput builderInput, AddressablesPlayerBuildResult rst)
		{
			if (string.IsNullOrEmpty(rst.Error) && !context.BuildMenu.OnPostbuild(builderInput, rst))
				Addressables.LogError($"Addressable content post-build failure : {context.BuildMenu.BuildMenuPath}");
		}

		void OnCleanAll()
		{
            if (!EditorUtility.DisplayDialog("Clear build cache", "Do you really want to clear your entire build cache and runtime data cache?", "Yes", "No"))
                return;
			OnCleanAddressables(null);
            OnCleanSBP(false);
		}

		void OnCleanAddressables(object builder)
		{
			AddressableAssetSettings.CleanPlayerContent(builder as IDataBuilder);
		}

        void OnCleanSBP(object prompt)
		{
            BuildCache.PurgeCache((bool) prompt);
		}

		void OnPrepareUpdate()
		{
			var path = ContentUpdateScript.GetContentStateDataPath(false);
			if (string.IsNullOrEmpty(path))
			{
				Debug.LogWarning("No path specified for Content State Data file.");
				return;
			}

			if (ResourceManagerConfig.ShouldPathUseWebRequest(path))
				path = ContentUpdateScript.DownloadBinFileToTempLocation(path);

			if (!File.Exists(path))
			{
				if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
				{
					Debug.LogWarningFormat("No Content State Data file exists at path: {0}", path);
					return;
				}
				else
				{
					bool selectFileManually = EditorUtility.DisplayDialog("Unable to Check for Update Restrictions", $"The addressable_content_state.bin file could " +
						$"not be found at {path}", "Select .bin file", "Cancel content update");
					if (selectFileManually)
						path = ContentUpdateScript.GetContentStateDataPath(true);
					else
					{
						Debug.LogWarningFormat("No Content State Data file exists at path: {0}.  Content update has been cancelled.", path);
						return;
					}
				}
			}

			ContentUpdatePreviewWindow.PrepareForContentUpdate(AddressableAssetSettingsDefaultObject.Settings, path);
		}

		void OnSortGroups()
		{
			Undo.RecordObject(settings, "AddressableAssetSettings");
			List<AddressableAssetGroup> sortedGroups = new List<AddressableAssetGroup>(settings.groups);

			sortedGroups.Sort((g0, g1) =>
			{
				if (g0 == null)
				{
					return -1;
				}
				else if (g1 == null)
				{
					return 1;
				}

				if (g0.ReadOnly || g1.ReadOnly)
				{
					return g0.ReadOnly.CompareTo(g1.ReadOnly) * -1;
				}

				if (g0.Default || g1.Default)
				{
					return g0.Default.CompareTo(g1.Default) * -1;
				}

				return g0.Name.CompareTo(g1.Name);
			});

			settings.groups.Clear();
			settings.groups.AddRange(sortedGroups);

			settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupMoved, settings.groups, true, true);
			Reload();
		}

#if (ENABLE_CCD && UNITY_2019_4_OR_NEWER)
		async void OnBuildAndRelease()
		{
			await AddressableAssetSettings.BuildAndReleasePlayerContent();
		}
#endif

		void OnBuildScript(object context)
		{
			OnSetActiveBuildScript(context);
			OnBuildPlayerData();
		}

		void OnBuildPlayerData()
		{
			AddressableAssetSettings.BuildPlayerContent();
		}

		void OnSetActiveBuildScript(object context)
		{
			AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilderIndex = (int)context;
		}

		void OnSetActivePlayModeScript(object context)
		{
			AddressableAssetSettingsDefaultObject.Settings.ActivePlayModeDataBuilderIndex = (int)context;
		}

		void OnHierSearchClick()
		{
			ProjectConfigData.HierarchicalSearch = !ProjectConfigData.HierarchicalSearch;
			m_EntryTree.SwapSearchType();
			m_EntryTree.Reload();
			m_EntryTree.Repaint();
		}

		void CreateProfileDropdown()
		{
			var activeProfileName = settings.profileSettings.GetProfileName(settings.activeProfileId);
			if (string.IsNullOrEmpty(activeProfileName))
			{
				settings.activeProfileId = null; //this will reset it to default.
				activeProfileName = settings.profileSettings.GetProfileName(settings.activeProfileId);
			}

			var profileButton = new GUIContent("Profile: " + activeProfileName, "The active collection of build path settings");

			Rect r = GUILayoutUtility.GetRect(profileButton, m_ButtonStyle, GUILayout.Width(115f));
			if (EditorGUI.DropdownButton(r, profileButton, FocusType.Passive, EditorStyles.toolbarDropDown))
			{
				//GUIUtility.hotControl = 0;
				var menu = new GenericMenu();

				var nameList = settings.profileSettings.GetAllProfileNames();

				foreach (var name in nameList)
				{
					menu.AddItem(new GUIContent(name), name == activeProfileName, SetActiveProfile, name);
				}

				menu.AddSeparator(string.Empty);
				menu.AddItem(new GUIContent("Manage Profiles"), false, () => EditorWindow.GetWindow<ProfileWindow>().Show(true));
				menu.DropDown(r);
			}
		}

		void SetActiveProfile(object context)
		{
			var n = context as string;
			AddressableAssetSettingsDefaultObject.Settings.activeProfileId = AddressableAssetSettingsDefaultObject.Settings.profileSettings.GetProfileId(n);
			AddressableAssetUtility.OpenAssetIfUsingVCIntegration(AddressableAssetSettingsDefaultObject.Settings);
		}

		bool m_ModificationRegistered;

		public void OnEnable()
		{
			if (AddressableAssetSettingsDefaultObject.Settings == null)
				return;
			if (!m_ModificationRegistered)
			{
				AddressableAssetSettingsDefaultObject.Settings.OnModification += OnSettingsModification;
				m_ModificationRegistered = true;
			}
		}

		public void OnDisable()
		{
			if (AddressableAssetSettingsDefaultObject.Settings == null)
				return;
			if (m_ModificationRegistered)
			{
				AddressableAssetSettingsDefaultObject.Settings.OnModification -= OnSettingsModification;
				m_ModificationRegistered = false;
			}
		}

		public bool OnGUI(Rect pos)
		{
			if (settings == null)
				return false;

			if (!m_ModificationRegistered)
			{
				m_ModificationRegistered = true;
				settings.OnModification -= OnSettingsModification; //just in case...
				settings.OnModification += OnSettingsModification;
			}

			if (m_EntryTree == null)
				InitialiseEntryTree();

			HandleVerticalResize(pos);
			var inRectY = pos.height;
			var searchRect = new Rect(pos.xMin, pos.yMin, pos.width, k_SearchHeight);
			var treeRect = new Rect(pos.xMin, pos.yMin + k_SearchHeight, pos.width, inRectY - k_SearchHeight);

			TopToolbar(searchRect);
			m_EntryTree.OnGUI(treeRect);
			return m_ResizingVerticalSplitter;
		}

		internal AddressableAssetEntryTreeView InitialiseEntryTree()
		{
			if (m_TreeState == null)
				m_TreeState = new TreeViewState();

			var headerState = AddressableAssetEntryTreeView.CreateDefaultMultiColumnHeaderState();
			if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_Mchs, headerState))
				MultiColumnHeaderState.OverwriteSerializedFields(m_Mchs, headerState);
			m_Mchs = headerState;

			m_SearchField = new SearchField();
			m_EntryTree = new AddressableAssetEntryTreeView(m_TreeState, m_Mchs, this);
			m_EntryTree.Reload();
			return m_EntryTree;
		}

		public void Reload()
		{
			if (m_EntryTree != null)
			{
				if (settings != AddressableAssetSettingsDefaultObject.Settings)
				{
					settings = AddressableAssetSettingsDefaultObject.Settings;
				}

				m_EntryTree.Reload();
			}
		}

		void HandleVerticalResize(Rect position)
		{
			m_VerticalSplitterRect.y = (int)(position.yMin + position.height * m_VerticalSplitterPercent);
			m_VerticalSplitterRect.width = position.width;
			m_VerticalSplitterRect.height = k_SplitterWidth;


			EditorGUIUtility.AddCursorRect(m_VerticalSplitterRect, MouseCursor.ResizeVertical);
			if (Event.current.type == EventType.MouseDown && m_VerticalSplitterRect.Contains(Event.current.mousePosition))
				m_ResizingVerticalSplitter = true;

			if (m_ResizingVerticalSplitter)
			{
				var mousePosInRect = Event.current.mousePosition.y - position.yMin;
				m_VerticalSplitterPercent = Mathf.Clamp(mousePosInRect / position.height, 0.20f, 0.90f);
				m_VerticalSplitterRect.y = (int)(position.height * m_VerticalSplitterPercent + position.yMin);

				if (Event.current.type == EventType.MouseUp)
				{
					m_ResizingVerticalSplitter = false;
				}
			}
			else
				m_VerticalSplitterPercent = Mathf.Clamp(m_VerticalSplitterPercent, 0.20f, 0.90f);
		}
	}
}
