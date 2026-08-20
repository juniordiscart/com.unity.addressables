using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using TreeView = UnityEngine.UIElements.TreeView;

namespace UnityEditor.AddressableAssets.Tests
{
    public class AddressableAssetWindowTests : AddressableAssetTestBase
    {
        [Test]
        public void AddressableAssetWindow_OfferToConvert_CantConvertWithNoBundles()
        {
            AddressableAssetsWindow aaWindow = ScriptableObject.CreateInstance<AddressableAssetsWindow>();
            var prevGroupCount = Settings.groups.Count;
            aaWindow.OfferToConvert(Settings);
            Assert.AreEqual(prevGroupCount, Settings.groups.Count);
            Object.DestroyImmediate(aaWindow);
        }

        [Test]
        public void AddressableAssetWindow_SimplifyAddress_ReturnsFileNameOnly()
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            var entry = Settings.CreateOrMoveEntry(m_AssetGUID, Settings.DefaultGroup);
            Assert.AreEqual(assetPath, entry.address);

            AddressableAssetEntryTreeView treeView = new AddressableAssetEntryTreeView(Settings);
            treeView.SimplifyAddressesImpl(new List<AssetEntryTreeViewItem>() {new AssetEntryTreeViewItem(entry, 1)});

            Assert.AreEqual(Path.GetFileNameWithoutExtension(assetPath), entry.address);
        }

        [Test]
        public void AddressableAssetWindow_RemovedEntries_AreNoLongerPresent()
        {
            var entry = Settings.CreateOrMoveEntry(m_AssetGUID, Settings.DefaultGroup);

            AddressableAssetEntryTreeView treeView = new AddressableAssetEntryTreeView(Settings);
            treeView.RemoveEntryImpl(new List<AssetEntryTreeViewItem>() {new AssetEntryTreeViewItem(entry, 1)}, true);

            Assert.IsNull(Settings.FindAssetEntry(m_AssetGUID));
        }

        [Test]
        public void AddressableAssetWindow_RemoveGroup_GroupGetsRemovedCorrectly()
        {
            var group = Settings.CreateGroup("RemoveMeGroup", false, false, true, new List<AddressableAssetGroupSchema>());
            AddressableAssetEntryTreeView treeView = new AddressableAssetEntryTreeView(Settings);
            treeView.RemoveGroupImpl(new List<AssetEntryTreeViewItem>() {new AssetEntryTreeViewItem(group, 1)}, true);
            Assert.IsNull(Settings.FindGroup("RemoveMeGroup"));
        }

        [Test]
        public void AddressableAssetWindow_RemoveMissingReferences_RemovesAllNullReferences()
        {
            Settings.groups.Add(null);
            Settings.groups.Add(null);

            AddressableAssetEntryTreeView treeView = new AddressableAssetEntryTreeView(Settings);
            treeView.RemoveMissingReferencesImpl();
            foreach (var group in Settings.groups)
                Assert.IsNotNull(group);
        }

        [Test]
        public void AddressableAssetWindow_SetDefaultGroup_SetsTheSpecifiedGroupToDefault()
        {
            var savedDefaultGroup = Settings.DefaultGroup;
            var newDefaultGroup = Settings.CreateGroup("NewDefaultGroup", false, false, true, new List<AddressableAssetGroupSchema>());
            AddressableAssetEntryTreeView treeView = new AddressableAssetEntryTreeView(Settings);

            treeView.SetGroupAsDefault(new List<AssetEntryTreeViewItem>() {new AssetEntryTreeViewItem(newDefaultGroup, 1)});

            Assert.AreEqual(newDefaultGroup, Settings.DefaultGroup);

            Settings.DefaultGroup = savedDefaultGroup;
            Settings.RemoveGroup(newDefaultGroup);
        }

        private AddressableAssetEntryTreeView InitGroupEditorWithState(AddressableAssetsWindow aaWindow, AddressableAssetEntryTreeViewState treeState, MultiColumnHeaderState mchs)
        {
            aaWindow.m_GroupEditor = new AddressableAssetsSettingsGroupEditor(aaWindow);
            aaWindow.m_GroupEditor.OnDisable();
            aaWindow.m_GroupEditor.settings = Settings;
            aaWindow.m_GroupEditor.m_TreeState = treeState;
            aaWindow.m_GroupEditor.m_Mchs = mchs;
            aaWindow.m_GroupEditor.InitialiseEntryTree();
            return aaWindow.m_GroupEditor.m_EntryTree;
        }

        [Test]
        public void AddressableAssetWindow_GroupWindow_ColumnWidthsAreSetWhenValid()
        {
            var mchs = AddressableAssetEntryTreeView.CreateDefaultMultiColumnHeaderState();
            AddressableAssetEntryTreeViewState treeState = new AddressableAssetEntryTreeViewState();
            treeState.columnWidths = new float[mchs.columns.Length];
            for (var i = 0; i < treeState.columnWidths.Length; i++)
            {
                treeState.columnWidths[i] = 9999.0f;
            }

            AddressableAssetsWindow aaWindow = ScriptableObject.CreateInstance<AddressableAssetsWindow>();
            var treeView = InitGroupEditorWithState(aaWindow, treeState, mchs);
            foreach (var col in treeView.multiColumnHeader.state.columns)
            {
                Assert.AreEqual(9999.0f, col.width);

            }
        }


        [Test]
        public void AddressableAssetWindow_GroupWindow_ColumnWidthsAreDefaultWhenInvalid()
        {
            // in this case we'll only set one header column so the widths don't match and nothing is done
            var defaultMchs = AddressableAssetEntryTreeView.CreateDefaultMultiColumnHeaderState();
            var mchs = AddressableAssetEntryTreeView.CreateDefaultMultiColumnHeaderState();
            AddressableAssetEntryTreeViewState treeState = new AddressableAssetEntryTreeViewState();
            treeState.columnWidths = new float[]{9999.0f};

            AddressableAssetsWindow aaWindow = ScriptableObject.CreateInstance<AddressableAssetsWindow>();
            var treeView = InitGroupEditorWithState(aaWindow, treeState, mchs);
            Assert.AreEqual(defaultMchs.columns.Length, treeView.multiColumnHeader.state.columns.Length);
            for (var i = 0; i < defaultMchs.columns.Length; i++)
            {
                Assert.AreNotEqual(9999.0f, defaultMchs.columns[i].width);
                Assert.AreEqual(treeView.multiColumnHeader.state.columns[i].width, defaultMchs.columns[i].width);
            }
        }

        [Test]
        public void AddressableAssetWindow_GroupWindow_AddGroupUpdatesSortSettings()
        {
            var defaultMchs = AddressableAssetEntryTreeView.CreateDefaultMultiColumnHeaderState();
            var mchs = AddressableAssetEntryTreeView.CreateDefaultMultiColumnHeaderState();
            AddressableAssetEntryTreeViewState treeState = new AddressableAssetEntryTreeViewState();

            AddressableAssetsWindow aaWindow = ScriptableObject.CreateInstance<AddressableAssetsWindow>();
            var treeView = InitGroupEditorWithState(aaWindow, treeState, mchs);

            AddressableAssetGroup group1 = null, group2 = null;
            try
            {
                Settings.OnModification += aaWindow.m_GroupEditor.OnSettingsModification;

                var defaultGroup = Settings.DefaultGroup;
                group1 = Settings.CreateGroup("Group 1", false, false, true, new List<AddressableAssetGroupSchema>());
                group2 = Settings.CreateGroup("Group 2", false, false, true, new List<AddressableAssetGroupSchema>());
                Assert.AreEqual(3, Settings.groups.Count);
                Assert.AreEqual(3, treeState.sortOrderList.Count);
            }
            finally
            {
                if (group1 != null)
                {
                    treeView.RemoveGroupImpl(new List<AssetEntryTreeViewItem>() { new AssetEntryTreeViewItem(group1, 1) }, true);
                }
                if (group2 != null)
                {
                    treeView.RemoveGroupImpl(new List<AssetEntryTreeViewItem>() { new AssetEntryTreeViewItem(group2, 1) }, true);
                }
                Settings.OnModification -= aaWindow.m_GroupEditor.OnSettingsModification;
            }
        }

        [Test]
        public void AddressableAssetWindow_GroupWindow_DeleteGroupUpdatesSortSettings()
        {
            var defaultMchs = AddressableAssetEntryTreeView.CreateDefaultMultiColumnHeaderState();
            var mchs = AddressableAssetEntryTreeView.CreateDefaultMultiColumnHeaderState();
            AddressableAssetEntryTreeViewState treeState = new AddressableAssetEntryTreeViewState();

            AddressableAssetsWindow aaWindow = ScriptableObject.CreateInstance<AddressableAssetsWindow>();
            var treeView = InitGroupEditorWithState(aaWindow, treeState, mchs);

            AddressableAssetGroup group1 = null;
            try
            {
                Settings.OnModification += aaWindow.m_GroupEditor.OnSettingsModification;

                var defaultGroup = Settings.DefaultGroup;
                group1 = Settings.CreateGroup("Group 1", false, false, true, new List<AddressableAssetGroupSchema>());
                Assert.AreEqual(2, Settings.groups.Count);
                Assert.AreEqual(2, treeState.sortOrderList.Count);
                Settings.RemoveGroup(group1);
                Assert.AreEqual(1, Settings.groups.Count);
                Assert.AreEqual(1, treeState.sortOrderList.Count);
            }
            finally
            {
                if (group1 != null)
                {
                    treeView.RemoveGroupImpl(new List<AssetEntryTreeViewItem>() { new AssetEntryTreeViewItem(group1, 1) }, true);
                }
                Settings.OnModification -= aaWindow.m_GroupEditor.OnSettingsModification;
            }
        }

        [Test]
        public void AddressableAssetWindow_CanSelectGroupTreeViewByAddressableAssetEntries()
        {
            //Setup
            var defaultGroup = Settings.DefaultGroup;
            Assert.IsNotNull(defaultGroup, "Default Group is not found");
            ProjectConfigData.ShowSubObjectsInGroupView = true;

            string path0 = GetAssetPath("test.prefab");
            string p0 = AssetDatabase.AssetPathToGUID(path0);
            Assert.IsFalse(string.IsNullOrEmpty(p0), "Could not setup for Asset \"test.prefab\"");
            Texture t = new Texture2D(4, 4);
            t.name = "tex";
            AssetDatabase.AddObjectToAsset(t, path0);
            AssetDatabase.SaveAssets();
            string p1 = AssetDatabase.AssetPathToGUID(GetAssetPath("test 1.prefab"));
            Assert.IsFalse(string.IsNullOrEmpty(p1), "Could not setup for Asset \"test 1.prefab\"");
            string p2 = AssetDatabase.AssetPathToGUID(GetAssetPath("test 2.prefab"));
            Assert.IsFalse(string.IsNullOrEmpty(p2), "Could not setup for Asset \"test 2.prefab\"");

            var e0 = Settings.CreateOrMoveEntry(p0, defaultGroup);
            List<AddressableAssetEntry> gathered = new List<AddressableAssetEntry>();
            e0.GatherAllAssets(gathered, false, true, true);
            Assert.AreEqual(1, gathered.Count, "Incorrect subObject count for Asset at " + path0);

            var e1 = Settings.CreateOrMoveEntry(p1, defaultGroup);
            var e2 = Settings.CreateOrMoveEntry(p2, defaultGroup);

            AddressableAssetsWindow aaWindow = ScriptableObject.CreateInstance<AddressableAssetsWindow>();
            aaWindow.m_GroupEditor = new AddressableAssetsSettingsGroupEditor(aaWindow);
            aaWindow.m_GroupEditor.OnDisable();
            aaWindow.m_GroupEditor.settings = Settings;
            var entryTree = aaWindow.m_GroupEditor.InitialiseEntryTree();

            //Test
            Assert.AreEqual(0, entryTree.GetSelection().Count, "entryTree is not expected to have anything select at creation");
            aaWindow.SelectAssetsInGroupEditor(new List<AddressableAssetEntry>() {e1});
            Assert.AreEqual(1, entryTree.GetSelection().Count, "Expecting to have \"test 1.prefab\" selected.");
            aaWindow.SelectAssetsInGroupEditor(new List<AddressableAssetEntry>() {e2});
            Assert.AreEqual(1, entryTree.GetSelection().Count, "Expecting to have \"test 2.prefab\" selected.");
            aaWindow.SelectAssetsInGroupEditor(new List<AddressableAssetEntry>() {e1, e2});
            Assert.AreEqual(2, entryTree.GetSelection().Count, "Expecting to have \"test 1.prefab\" and \"test 2.prefab\" selected.");

            Assert.IsTrue(ProjectConfigData.ShowSubObjectsInGroupView, "Need to display subObjects to test that they are being shown");
            aaWindow.SelectAssetsInGroupEditor(new List<AddressableAssetEntry>() {gathered[0]});
            Assert.AreEqual(1, entryTree.GetSelection().Count, "Expecting to have \"test.prefab[SubObject]\" selected.");

            //Cleanup
            Assert.IsTrue(Settings.RemoveAssetEntry(e1, false), "Failed to cleanup AssetEntry \"test 1.prefab\" from test settings.");
            Assert.IsTrue(Settings.RemoveAssetEntry(e2, false), "Failed to cleanup AssetEntry \"test 2.prefab\" from test settings.");
            Object.DestroyImmediate(aaWindow);
        }

        static List<AssetEntryTreeViewItem> GroupNodes(params AddressableAssetGroup[] groups)
        {
            var nodes = new List<AssetEntryTreeViewItem>();
            foreach (var g in groups)
                nodes.Add(new AssetEntryTreeViewItem(g, 1));
            return nodes;
        }

        // CBD-2230: Converting to Content Directory should disable (not remove) the AssetBundle schema.
        [Test]
        public void AddressableAssetWindow_ConvertToContentDirectory_DisablesBundledSchemaAndEnablesContentDirectory()
        {
            var group = Settings.CreateGroup("ConvertToCD_Local", false, false, false, null, typeof(BundledAssetGroupSchema));
            try
            {
                var bundled = group.GetSchema<BundledAssetGroupSchema>();
                bundled.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
                Assert.IsTrue(bundled.IsEnabled, "BundledAssetGroupSchema should start enabled.");

                var treeView = new AddressableAssetEntryTreeView(Settings);
                treeView.ConvertToContentDirectoryImpl(GroupNodes(group), skipConfirmation: true);

                Assert.IsTrue(group.HasSchema<BundledAssetGroupSchema>(), "BundledAssetGroupSchema should be kept, not removed.");
                Assert.IsFalse(group.GetSchema<BundledAssetGroupSchema>().IsEnabled, "BundledAssetGroupSchema should be disabled after conversion.");
                Assert.IsTrue(group.HasSchema<ContentDirectoryGroupSchema>(), "ContentDirectoryGroupSchema should be added.");
                Assert.IsTrue(group.GetSchema<ContentDirectoryGroupSchema>().IsEnabled, "ContentDirectoryGroupSchema should be enabled after conversion.");
            }
            finally
            {
                Settings.RemoveGroup(group);
            }
        }

        // CBD-2228: Converting to Content Directory with remote content should warn and add the schema disabled.
        [Test]
        public void AddressableAssetWindow_ConvertToContentDirectory_RemoteContent_AddsDisabledSchemaAndWarns()
        {
            var group = Settings.CreateGroup("ConvertToCD_Remote", false, false, false, null, typeof(BundledAssetGroupSchema));
            string originalRemoteLoadPath = Settings.profileSettings.GetValueById(Settings.activeProfileId,
                Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kRemoteLoadPath).Id);
            try
            {
                Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath, "http://fakeremotepath/");
                var bundled = group.GetSchema<BundledAssetGroupSchema>();
                bundled.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kRemoteLoadPath);

                LogAssert.Expect(LogType.Warning, new Regex("remote location"));

                var treeView = new AddressableAssetEntryTreeView(Settings);
                treeView.ConvertToContentDirectoryImpl(GroupNodes(group), skipConfirmation: true);

                Assert.IsTrue(group.HasSchema<ContentDirectoryGroupSchema>(), "ContentDirectoryGroupSchema should be added even for remote content.");
                Assert.IsFalse(group.GetSchema<ContentDirectoryGroupSchema>().IsEnabled, "ContentDirectoryGroupSchema should be disabled for remote content.");
                Assert.IsTrue(group.HasSchema<BundledAssetGroupSchema>(), "BundledAssetGroupSchema should be kept.");
                Assert.IsTrue(group.GetSchema<BundledAssetGroupSchema>().IsEnabled, "BundledAssetGroupSchema should remain enabled for remote content.");
            }
            finally
            {
                Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath, originalRemoteLoadPath);
                Settings.RemoveGroup(group);
            }
        }

        // CBD-2229: Add "Convert to AssetBundles" as the reverse of "Convert to Content Directory".
        [Test]
        public void AddressableAssetWindow_ConvertToAssetBundles_DisablesContentDirectoryAndEnablesBundled()
        {
            var group = Settings.CreateGroup("ConvertToAB", false, false, false, null, typeof(ContentDirectoryGroupSchema));
            try
            {
                Assert.IsTrue(group.GetSchema<ContentDirectoryGroupSchema>().IsEnabled, "ContentDirectoryGroupSchema should start enabled.");

                var treeView = new AddressableAssetEntryTreeView(Settings);
                treeView.ConvertToAssetBundlesImpl(GroupNodes(group));

                Assert.IsTrue(group.HasSchema<ContentDirectoryGroupSchema>(), "ContentDirectoryGroupSchema should be kept, not removed.");
                Assert.IsFalse(group.GetSchema<ContentDirectoryGroupSchema>().IsEnabled, "ContentDirectoryGroupSchema should be disabled after conversion.");
                Assert.IsTrue(group.HasSchema<BundledAssetGroupSchema>(), "BundledAssetGroupSchema should be added.");
                Assert.IsTrue(group.GetSchema<BundledAssetGroupSchema>().IsEnabled, "BundledAssetGroupSchema should be enabled after conversion.");
            }
            finally
            {
                Settings.RemoveGroup(group);
            }
        }

        // CBD-2229/2230: Converting to Content Directory and back should restore the AssetBundle build state.
        [Test]
        public void AddressableAssetWindow_ConvertRoundTrip_RestoresAssetBundleState()
        {
            var group = Settings.CreateGroup("ConvertRoundTrip", false, false, false, null, typeof(BundledAssetGroupSchema));
            try
            {
                var bundled = group.GetSchema<BundledAssetGroupSchema>();
                bundled.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);

                var treeView = new AddressableAssetEntryTreeView(Settings);
                treeView.ConvertToContentDirectoryImpl(GroupNodes(group), skipConfirmation: true);
                Assert.IsTrue(group.GetSchema<ContentDirectoryGroupSchema>().IsEnabled);
                Assert.IsFalse(group.GetSchema<BundledAssetGroupSchema>().IsEnabled);

                treeView.ConvertToAssetBundlesImpl(GroupNodes(group));
                Assert.IsTrue(group.GetSchema<BundledAssetGroupSchema>().IsEnabled, "AssetBundle schema should be re-enabled after round trip.");
                Assert.IsFalse(group.GetSchema<ContentDirectoryGroupSchema>().IsEnabled, "Content Directory schema should be disabled after round trip.");
            }
            finally
            {
                Settings.RemoveGroup(group);
            }
        }

        // CBD-2229/2230: Path edits made after a conversion must carry over when converting back.
        [Test]
        public void AddressableAssetWindow_ConvertBack_InheritsEditedPaths()
        {
            var group = Settings.CreateGroup("ConvertPathInherit", false, false, false, null, typeof(BundledAssetGroupSchema));
            try
            {
                var bundled = group.GetSchema<BundledAssetGroupSchema>();
                bundled.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);

                var treeView = new AddressableAssetEntryTreeView(Settings);
                treeView.ConvertToContentDirectoryImpl(GroupNodes(group), skipConfirmation: true);

                // Simulate the user editing the Content Directory paths after the first conversion.
                var contentDir = group.GetSchema<ContentDirectoryGroupSchema>();
                contentDir.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kRemoteBuildPath);
                contentDir.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kRemoteLoadPath);
                string editedBuildPathId = contentDir.BuildPath.Id;
                string editedLoadPathId = contentDir.LoadPath.Id;

                // Converting back re-enables the existing bundled schema, which must pick up the edited paths.
                treeView.ConvertToAssetBundlesImpl(GroupNodes(group));

                Assert.AreEqual(editedBuildPathId, group.GetSchema<BundledAssetGroupSchema>().BuildPath.Id,
                    "AssetBundle build path should inherit the edited Content Directory build path, not a stale value.");
                Assert.AreEqual(editedLoadPathId, group.GetSchema<BundledAssetGroupSchema>().LoadPath.Id,
                    "AssetBundle load path should inherit the edited Content Directory load path, not a stale value.");
            }
            finally
            {
                Settings.RemoveGroup(group);
            }
        }

        // CBD-2228: A remote group's build path is still inherited by the disabled Content Directory schema; only the
        // invalid remote load path is dropped.
        [Test]
        public void AddressableAssetWindow_ConvertToContentDirectory_RemoteContent_InheritsBuildPathButNotLoadPath()
        {
            var group = Settings.CreateGroup("ConvertToCD_RemoteBuildPath", false, false, false, null, typeof(BundledAssetGroupSchema));
            string originalRemoteLoadPath = Settings.profileSettings.GetValueById(Settings.activeProfileId,
                Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kRemoteLoadPath).Id);
            try
            {
                Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath, "http://fakeremotepath/");
                var bundled = group.GetSchema<BundledAssetGroupSchema>();
                bundled.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kRemoteBuildPath);
                bundled.LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kRemoteLoadPath);
                string bundledBuildPathId = bundled.BuildPath.Id;
                string remoteLoadPathId = bundled.LoadPath.Id;

                LogAssert.Expect(LogType.Warning, new Regex("remote location"));

                var treeView = new AddressableAssetEntryTreeView(Settings);
                treeView.ConvertToContentDirectoryImpl(GroupNodes(group), skipConfirmation: true);

                var contentDir = group.GetSchema<ContentDirectoryGroupSchema>();
                Assert.IsFalse(contentDir.IsEnabled, "Content Directory schema should be disabled for remote content.");
                Assert.AreEqual(bundledBuildPathId, contentDir.BuildPath.Id, "Build path should be inherited even for remote content.");
                Assert.AreNotEqual(remoteLoadPathId, contentDir.LoadPath.Id, "The invalid remote load path should not be inherited.");
            }
            finally
            {
                Settings.profileSettings.SetValue(Settings.activeProfileId, AddressableAssetSettings.kRemoteLoadPath, originalRemoteLoadPath);
                Settings.RemoveGroup(group);
            }
        }

        // CBD-2228: GetGroupIconType must not evaluate (and warn about) an unassigned Content Directory load path.
        [Test]
        public void AddressableAssetWindow_GetGroupIconType_EmptyContentDirectoryLoadPath_DoesNotWarn()
        {
            var group = Settings.CreateGroup("EmptyCDLoadPath", false, false, false, null, typeof(ContentDirectoryGroupSchema));
            try
            {
                var contentDirSchema = group.GetSchema<ContentDirectoryGroupSchema>();
                Assert.IsTrue(contentDirSchema.IsEnabled);
                // Simulate the mid-Validate state where the load path id has not yet been assigned.
                contentDirSchema.m_LoadPath = new ProfileValueReference();

                var iconType = AddressableAssetEntryTreeView.GetGroupIconType(group);

                LogAssert.NoUnexpectedReceived();
                Assert.AreEqual(GroupIconType.ContentDirectory, iconType, "An unassigned load path should not be treated as remote/error.");
            }
            finally
            {
                Settings.RemoveGroup(group);
            }
        }

        // CBD-2229: Only one buildable schema should ever be enabled after a conversion.
        [Test]
        public void AddressableAssetWindow_Convert_LeavesExactlyOneBuildableSchemaEnabled()
        {
            var group = Settings.CreateGroup("ConvertSingleEnabled", false, false, false, null, typeof(BundledAssetGroupSchema));
            try
            {
                group.GetSchema<BundledAssetGroupSchema>().LoadPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);

                var treeView = new AddressableAssetEntryTreeView(Settings);
                treeView.ConvertToContentDirectoryImpl(GroupNodes(group), skipConfirmation: true);

                bool bundledEnabled = group.GetSchema<BundledAssetGroupSchema>().IsEnabled;
                bool contentDirEnabled = group.GetSchema<ContentDirectoryGroupSchema>().IsEnabled;
                Assert.IsTrue(bundledEnabled ^ contentDirEnabled, "Exactly one buildable schema should be enabled after conversion.");
            }
            finally
            {
                Settings.RemoveGroup(group);
            }
        }
    }
}
