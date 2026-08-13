using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Tests;
using UnityEditor.IMGUI.Controls;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.Editor.GUI
{
    public class AddressableAssetSettingsGroupEditorTests : AddressableAssetTestBase
    {
        private AddressableAssetsSettingsGroupEditor m_GroupEditor;
        private GUID settingsGuid;
        [SetUp]
        public void SetUp()
        {
            Assert.AreEqual(1, Settings.groups.Count, "Expect only Default Group is initialized at test startup.");
            Assert.AreEqual(1, Settings.GroupTemplateObjects.Count, "Expect only Packed Group Template is initialized at test startup.");

            settingsGuid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(Settings));

            // initial sort hasn't been set
            var sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Assert.AreEqual(0, sortSettings.sortOrder.Length, "Expect no group sort set at test startup.");

            AddressableAssetSettingsDefaultObject.Settings = Settings;
            m_GroupEditor = new AddressableAssetsSettingsGroupEditor(null);
            m_GroupEditor.settings = Settings;
            m_GroupEditor.InitialiseEntryTree();
        }

        [TearDown]
        public void TearDown()
        {
            m_GroupEditor?.OnDisable();
            ReloadSettings();
            if (Directory.Exists(AddressablesFolder))
            {
                if (!AssetDatabase.DeleteAsset(AddressablesFolder))
                    Directory.Delete(AddressablesFolder, true);
            }

            if (Directory.Exists(ConfigFolder)) {
                if (!AssetDatabase.DeleteAsset(ConfigFolder))
                    Directory.Delete(ConfigFolder, true);
            }
        }
        [Test]
        public void TestInitialSortIsSaved()
        {

            Debug.Log(Settings.groups);
            Assert.AreEqual(1, Settings.groups.Count);

            // initial sort hasn't been set
            var sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Assert.AreEqual(1, sortSettings.sortOrder.Length);
        }

        [Test]
        public void TestAdd_FromEmpty()
        {
            // when adding we want them to appear at the top of the tree
            var defaultGroup = Settings.groups[0];
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup1 = GetGroupByName("Packed Assets");
            // at this point we should have a sort
            var sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Debug.Log(sortSettings.sortOrder.Length);
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup1.Guid,
                defaultGroup.Guid
            });


            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup2 = GetGroupByName("Packed Assets1");
            sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup2.Guid,
                packedAssetsGroup1.Guid,
                defaultGroup.Guid
            });
            LogTreeView();
        }

        [Test]
        public void TestAdd_ToSorted()
        {
            var defaultGroup = Settings.groups[0];
            for (int i = 0; i < 5; i++)
            {
                m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            }

            // we create 5 groups. Packed Assets through Packed Assets4
            var sortedGroups = new string[6];
            for (var i = 0; i < 5; i++)
            {
                Debug.Log("Packed Assets" + (i == 0 ? "" : i));
                sortedGroups[i] = GetGroupByName("Packed Assets"  + (i == 0 ? "" : i)).Guid; // the first Packed Assets group has no number
            }
            // we add the default group to the end fo the sort
            sortedGroups[5] = GetGroupByName(AddressableAssetSettings.DefaultLocalGroupName).Guid;

            // we reverse the groups Packed Assets4 ... Packed Assets
            Array.Reverse(sortedGroups);

            // we set the state of the groups as the sort order
            Assert.IsTrue(m_GroupEditor.m_EntryTree.state is AddressableAssetEntryTreeViewState);
            if (m_GroupEditor.m_EntryTree.state is AddressableAssetEntryTreeViewState state)
            {
                state.sortOrderList = new List<string>();
                state.sortOrderList.AddRange(sortedGroups);
            }

            // Now we add Packed Assets5
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);

            var sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                GetGroupByName("Packed Assets5").Guid,
                GetGroupByName(AddressableAssetSettings.DefaultLocalGroupName).Guid,
                GetGroupByName("Packed Assets4").Guid,
                GetGroupByName("Packed Assets3").Guid,
                GetGroupByName("Packed Assets2").Guid,
                GetGroupByName("Packed Assets1").Guid,
                GetGroupByName("Packed Assets").Guid,
            });
        }

        [Test]
        public void TestRemove_SingleGroup()
        {
            // when adding we want them to appear at the top of the tree
            var defaultGroup = Settings.groups[0];
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup1 = GetGroupByName("Packed Assets");
            var packedAssetsGroup2 = GetGroupByName("Packed Assets1");

            // they are now in reverse order of being added
            var sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup2.Guid,
                packedAssetsGroup1.Guid,
                defaultGroup.Guid
            });

            // remove the middle element
            var toRemove = new List<AssetEntryTreeViewItem>()
            {
                new AssetEntryTreeViewItem(packedAssetsGroup1, 1)
            };
            m_GroupEditor.m_EntryTree.RemoveGroupImpl(toRemove, true);

            // after removing the order should be preserved
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup2.Guid,
                defaultGroup.Guid
            });
        }

        [Test]
        public void TestRemove_MultiGroup()
        {
            // when adding we want them to appear at the top of the tree
            var defaultGroup = Settings.groups[0];
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup1 = GetGroupByName("Packed Assets");
            var packedAssetsGroup2 = GetGroupByName("Packed Assets1");
            var packedAssetsGroup3 = GetGroupByName("Packed Assets2");
            var packedAssetsGroup4 = GetGroupByName("Packed Assets3");

            // they are now in reverse order of being added
            var sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup4.Guid,
                packedAssetsGroup3.Guid,
                packedAssetsGroup2.Guid,
                packedAssetsGroup1.Guid,
                defaultGroup.Guid
            });

            // remove elements 1 and 3
            var toRemove = new List<AssetEntryTreeViewItem>()
            {
                new AssetEntryTreeViewItem(packedAssetsGroup1, 1),
                new AssetEntryTreeViewItem(packedAssetsGroup3, 1)
            };
            m_GroupEditor.m_EntryTree.RemoveGroupImpl(toRemove, true);

            // after removing the order should be preserved
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup4.Guid,
                packedAssetsGroup2.Guid,
                defaultGroup.Guid
            });
        }

        [Test]
        public void TestRename_Succeeds()
        {
            // when adding we want them to appear at the top of the tree
            var defaultGroup = Settings.groups[0];
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup1 = GetGroupByName("Packed Assets");
            var packedAssetsGroup2 = GetGroupByName("Packed Assets1");

            // they are now in reverse order of being added
            var sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup2.Guid,
                packedAssetsGroup1.Guid,
                defaultGroup.Guid
            });

            // get the element from the tree view that we want to rename
            int itemID = GetTreeViewItemIDByName(packedAssetsGroup1.Name);
            Assert.IsTrue(itemID != -1, "Could not find group \"Packed Assets\" in tree.");

            //  rename "PackedAssets1" to "Packed Assets4"
            Assert.True(m_GroupEditor.m_EntryTree.PerformRename("Packed Assets1", "Packed Assets4",
                itemID, true), "Could not rename from \"Packed Assets1\" to \"Packed Assets4\".");

            // after renaming the order should be preserved
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup2.Guid,
                packedAssetsGroup1.Guid,
                defaultGroup.Guid
            });
        }

        [Test]
        public void TestRename_Fails()
        {
            // when adding we want them to appear at the top of the tree
            var defaultGroup = Settings.groups[0];
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup1 = GetGroupByName("Packed Assets");
            var packedAssetsGroup2 = GetGroupByName("Packed Assets1");

            // they are now in reverse order of being added
            var sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup2.Guid,
                packedAssetsGroup1.Guid,
                defaultGroup.Guid
            });


            // get the element from the tree view that we want to rename
            int itemID = GetTreeViewItemIDByName(packedAssetsGroup1.Name);
            Assert.IsTrue(itemID != -1, "Could not find group \"Packed Assets\" in tree.");

            //  rename "Packed Assets1" to "Packed Assets" which already exists
            Assert.False(m_GroupEditor.m_EntryTree.PerformRename("Packed Assets1", "Packed Assets",
                itemID, true), "Was able to rename from \"Packed Assets1\" to \"Packed Assets\".");

            // after failing to rename the order should be preserved
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup2.Guid,
                packedAssetsGroup1.Guid,
                defaultGroup.Guid
            });
        }

        [Test]
        public void TestMove_FilePathToNullTarget()
        {
            // when adding we want them to appear at the top of the tree
            var defaultGroup = Settings.groups[0];

            var draggedPaths = new string[]
            {
                TestFolder + TestFolder + "/test.prefab",
            };
            var visualMode = m_GroupEditor.m_EntryTree.HandleDragAndDropPaths(draggedPaths, null, true);
            Assert.AreEqual(DragAndDropVisualMode.Rejected, visualMode);
        }

        [Test]
        public void TestMove_SingleElement()
        {
            // when adding we want them to appear at the top of the tree
            var defaultGroup = Settings.groups[0];
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup1 = GetGroupByName("Packed Assets");
            var packedAssetsGroup2 = GetGroupByName("Packed Assets1");
            // at this point we should have a sort
            var sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup2.Guid,
                packedAssetsGroup1.Guid,
                defaultGroup.Guid
            });

            // drag the default group into the middle position (1)
            var draggedNodes = new List<AssetEntryTreeViewItem>() { GetTreeViewItemByName(AddressableAssetSettings.DefaultLocalGroupName + " (Default)") };
            m_GroupEditor.m_EntryTree.HandleDragAndDropItems(draggedNodes, null, null, 1, true);
            sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup2.Guid,
                defaultGroup.Guid,
                packedAssetsGroup1.Guid,
            });
        }

        [Test]
        public void TestAdded_ThroughImportAsset()
        {
            // this test is basically simulating what would happen if a group is added through VCS
            var groupPath = TestFolder + "/Packed Assets Imported.asset";
            try
            {
                // when adding we want them to appear at the top of the tree
                var defaultGroup = Settings.groups[0];
                m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
                m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
                var packedAssetsGroup1 = GetGroupByName("Packed Assets");
                var packedAssetsGroup2 = GetGroupByName("Packed Assets1");
                Debug.Log($"Packed Asset {packedAssetsGroup1.Guid}");
                Debug.Log($"Packed Asset1 {packedAssetsGroup2.Guid}");

                // at this point we should have a sort
                var sortSettings = AddressableAssetGroupSortSettings.GetSettings();
                for (int i = 0; i < sortSettings.sortOrder.Length; i++)
                    Debug.Log(sortSettings.sortOrder[i]);
                Assert.AreEqual(sortSettings.sortOrder, new string[]
                {
                    packedAssetsGroup2.Guid,
                    packedAssetsGroup1.Guid,
                    defaultGroup.Guid
                });

                // copy an existing group and modify it to have a new name and GUID
                var sourceFile = ConfigFolder + "/AssetGroups/Packed Assets1.asset";
                var contents = File.ReadAllText(sourceFile);
                contents = contents.Replace(packedAssetsGroup2.Name, "Packed Assets Imported");
                contents = contents.Replace(packedAssetsGroup2.Guid, GUID.Generate().ToString());
                File.WriteAllText(groupPath, contents);
                AssetDatabase.ImportAsset(groupPath, ImportAssetOptions.ForceSynchronousImport);

                // this is technically an add so it will just go to the top
                var packedAssetsGroupImported = GetGroupByName("Packed Assets Imported");
                Assert.IsNotNull(packedAssetsGroupImported);
                sortSettings = AddressableAssetGroupSortSettings.GetSettings();
                Assert.AreEqual(sortSettings.sortOrder, new string[]
                {
                    packedAssetsGroupImported.Guid,
                    packedAssetsGroup2.Guid,
                    packedAssetsGroup1.Guid,
                    defaultGroup.Guid,
                });
            }
            finally
            {
                if (File.Exists(groupPath))
                {
                    File.Delete(groupPath);
                }
            }
        }


        [Test]
        public void TestMove_MultipleElements()
        {
            // when adding we want them to appear at the top of the tree
            var defaultGroup = Settings.groups[0];
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup1 = GetGroupByName("Packed Assets");
            var packedAssetsGroup2 = GetGroupByName("Packed Assets1");
            // at this point we should have a sort
            var sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup2.Guid,
                packedAssetsGroup1.Guid,
                defaultGroup.Guid
            });

            // drag the two packed groups to the end
            var draggedNodes = new List<AssetEntryTreeViewItem>() { GetTreeViewItemByName("Packed Assets1"), GetTreeViewItemByName("Packed Assets") };
            m_GroupEditor.m_EntryTree.HandleDragAndDropItems(draggedNodes, null, null, 3, true);
            sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                defaultGroup.Guid,
                packedAssetsGroup2.Guid,
                packedAssetsGroup1.Guid,
            });
        }

        [Test]
        public void TestMove_GroupAndAsset()
        {
            // when adding we want them to appear at the top of the tree
            var defaultGroup = Settings.groups[0];
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup1 = GetGroupByName("Packed Assets");
            var packedAssetsGroup2 = GetGroupByName("Packed Assets1");
            // at this point we should have a sort
            var sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup2.Guid,
                packedAssetsGroup1.Guid,
                defaultGroup.Guid
            });

            // first we drag in a prefab
            var draggedPaths = new string[]
            {
                TestFolder + TestFolder + "/test.prefab",
            };
            var visualMode =
                m_GroupEditor.m_EntryTree.HandleDragAndDropPaths(draggedPaths, GetTreeViewItemByName("Packed Assets"),
                    true);
            Assert.AreEqual(DragAndDropVisualMode.Copy, visualMode);

            // then drag the group and asset together
            var draggedNodes = new List<AssetEntryTreeViewItem>() { GetTreeViewItemByName("Packed Assets"), GetTreeViewItemByName("Test Object") };
            visualMode = m_GroupEditor.m_EntryTree.HandleDragAndDropItems(draggedNodes, null, null, 0, true);
            Assert.AreEqual(DragAndDropVisualMode.Copy, visualMode);
            sortSettings = AddressableAssetGroupSortSettings.GetSettings();
            Assert.AreEqual(sortSettings.sortOrder, new string[] {
                packedAssetsGroup1.Guid,
                packedAssetsGroup2.Guid,
                defaultGroup.Guid,
            });
        }

        [Test]
        public void TestMove_RejectDragIntoReadOnlyGroup()
        {
            // when adding we want them to appear at the top of the tree
            var defaultGroup = Settings.groups[0];
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup1 = GetGroupByName("Packed Assets");

            // drag an asset into the group
            var draggedPaths = new string[]
            {
                TestFolder + "/test.prefab",
            };
            var visualMode =
                m_GroupEditor.m_EntryTree.HandleDragAndDropPaths(draggedPaths, GetTreeViewItemByName("Packed Assets"),
                    true);
            Assert.AreEqual(DragAndDropVisualMode.Copy, visualMode);

            // mark read only
            packedAssetsGroup1.ReadOnly = true;

            // try to drag a new asset into the group
            draggedPaths = new string[]
            {
                TestFolder + "/test1.prefab",
            };
            visualMode =
                m_GroupEditor.m_EntryTree.HandleDragAndDropPaths(draggedPaths, GetTreeViewItemByName("Packed Assets"),
                    true);
            Assert.AreEqual(DragAndDropVisualMode.Rejected, visualMode);

            // try to drag an asset out of the group
            var item = GetTreeViewItemByName(TestFolder + "/test.prefab");
            Assert.IsNotNull(item);
            var draggedNodes = new List<AssetEntryTreeViewItem>() { item };
            visualMode = m_GroupEditor.m_EntryTree.HandleDragAndDropItems(draggedNodes,
                GetTreeViewItemByName("Packed Assets1"), null, 0, true);
            Assert.AreEqual(DragAndDropVisualMode.Rejected, visualMode);
        }


        [Test]
        public void TestSort_Presorted()
        {
            // when adding we want them to appear at the top of the tree
            var defaultGroup = Settings.groups[0];
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup1 = GetGroupByName("Packed Assets");
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup2 = GetGroupByName("Packed Assets1");

            AssetDatabase.DeleteAsset(AddressableAssetGroupSortSettings.DEFAULT_SETTING_PATH);
            var sortSettings = ScriptableObject.CreateInstance<AddressableAssetGroupSortSettings>();
            sortSettings.sortOrder = new string[] {
                packedAssetsGroup1.Guid,
                defaultGroup.Guid,
                packedAssetsGroup2.Guid,
            };
            AssetDatabase.CreateAsset(sortSettings, AddressableAssetGroupSortSettings.DEFAULT_SETTING_PATH);

            // sort the groups
            m_GroupEditor.m_EntryTree.DeserializeState(settingsGuid);
            m_GroupEditor.m_EntryTree.SortGroups();

            Assert.AreEqual(m_GroupEditor.m_EntryTree.GetTreeViewState().sortOrderList, new string[] {
                packedAssetsGroup1.Guid,
                defaultGroup.Guid,
                packedAssetsGroup2.Guid,
            });
        }

        [Test]
        public void TestSort_EmptyOrder()
        {
            var defaultGroup = Settings.groups[0];
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup1 = GetGroupByName("Packed Assets");
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup2 = GetGroupByName("Packed Assets1");
            Assert.AreEqual(3, Settings.groups.Count);

            // explicitly clear out the saved sort order
            AssetDatabase.DeleteAsset(AddressableAssetGroupSortSettings.DEFAULT_SETTING_PATH);
            var sortSettings = ScriptableObject.CreateInstance<AddressableAssetGroupSortSettings>();
            sortSettings.sortOrder = new string[] {
            };
            AssetDatabase.CreateAsset(sortSettings, AddressableAssetGroupSortSettings.DEFAULT_SETTING_PATH);

            // sort the groups
            m_GroupEditor.m_EntryTree.DeserializeState(settingsGuid);
            m_GroupEditor.m_EntryTree.SortGroups();

            // should be sorted alphabetically
            Assert.AreEqual(m_GroupEditor.m_EntryTree.GetTreeViewState().sortOrderList, new string[] {
                defaultGroup.Guid,
                packedAssetsGroup1.Guid,
                packedAssetsGroup2.Guid,
            });
        }

        [Test]
        public void TestSort_MismatchedGroups()
        {
            //
            var defaultGroup = Settings.groups[0];
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup1 = GetGroupByName("Packed Assets");
            m_GroupEditor.m_EntryTree.CreateNewGroup(Settings.GroupTemplateObjects[0]);
            var packedAssetsGroup2 = GetGroupByName("Packed Assets1");
            Assert.AreEqual(3, Settings.groups.Count);

            // explicitly mess up the sort order
            var missingGroupGuid = new GUID();
            AssetDatabase.DeleteAsset(AddressableAssetGroupSortSettings.DEFAULT_SETTING_PATH);
            var sortSettings = ScriptableObject.CreateInstance<AddressableAssetGroupSortSettings>();
            sortSettings.sortOrder = new string[] {
                defaultGroup.Guid,
                missingGroupGuid.ToString(),
                packedAssetsGroup2.Guid,
            };
            AssetDatabase.CreateAsset(sortSettings, AddressableAssetGroupSortSettings.DEFAULT_SETTING_PATH);
            // m_GroupEditor.m_EntryTree.GetTreeViewState().sortOrder = new List<string>();

            // sort the groups
            m_GroupEditor.m_EntryTree.DeserializeState(settingsGuid);
            m_GroupEditor.m_EntryTree.SortGroups();

            // packedAssetsGroup1 gets moved up to the top as it did not exist in the sort order
            // missingGroupGuid is discarded as it does not apply to any known group
            Assert.AreEqual(m_GroupEditor.m_EntryTree.GetTreeViewState().sortOrderList, new string[] {
                packedAssetsGroup1.Guid,
                defaultGroup.Guid,
                packedAssetsGroup2.Guid,
            });
        }


        AddressableAssetGroup GetGroupByName(string name)
        {
            return Settings.groups.Find((g) => g.Name == name); // the first Packed Assets group has no number
        }

        int GetTreeViewItemIDByName(string name)
        {
            int itemID = -1;
            var rows = m_GroupEditor.m_EntryTree.GetRows();
            foreach (var r in rows)
            {
                if (r.displayName == name)
                {
                    itemID = r.id;
                }
            }
            return itemID;
        }

        AssetEntryTreeViewItem GetTreeViewItemByName(string name)
        {
            var rows = m_GroupEditor.m_EntryTree.GetRows();
            foreach (var r in rows)
            {
                if (r.displayName == name)
                {
                    return r as AssetEntryTreeViewItem;
                }
            }
            return null;
        }

        void LogTreeView()
        {
            var builder = new StringBuilder();
            var rows = m_GroupEditor.m_EntryTree.GetRows();
            foreach (var r in rows)
            {
                builder.Append($"* {r.displayName}\n");
            }
            Debug.Log(builder.ToString());
        }
    }
}