using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Utility;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace Tests.Editor.Utility
{
    public class AssetReferenceUtilitiesTests
    {
        private SpritePackerMode m_SpritePackerMode;
        [SetUp]
        public void Setup()
        {
            m_SpritePackerMode = EditorSettings.spritePackerMode;
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;

            // first we copy the sprites~ directory from this folder into the Assets folder and rename it sprites
            DirectoryUtility.DirectoryCopy(AddressablesTestUtility.GetPackagePath() + "/Tests/Editor/Utility/sprites~", "Assets/sprites", true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSettings.spritePackerMode = m_SpritePackerMode;
            DirectoryUtility.DeleteDirectory("Assets/sprites", false, true);
        }

        // ok so first we want a straightforward test with a sprite atlas with several sprites
        // then we want a test with a sprite atlas with multiple sprites mixed with folders
        // then we want to test with nested folders
        [Test]
        [TestCase("Assets/sprites/Basic.spriteatlasv2")]
        [TestCase("Assets/sprites/FolderAndSprites.spriteatlasv2")]
        [TestCase("Assets/sprites/NestedFolder.spriteatlasv2")]
        public void TestSpriteAtlas(string atlasPath)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            Assert.IsNotNull(atlas);
            Assert.GreaterOrEqual(atlas.spriteCount, 1);

            HashSet<(Sprite, string)> map = AssetReferenceUtilities.GetAtlasSpritesAndPackables(ref atlas);
            Assert.AreEqual(atlas.spriteCount, map.Count);
            foreach (var entry in map)
            {
                var guid = entry.Item2;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var filename = Path.GetFileName(path);
                if (filename == "SpriteTypeMultiple.png" || filename == "00-00.png")
                {
                    var foundMultipleAsset = false;
                    Object[] data = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid));
                    if (data != null)
                    {
                        foreach (Object obj in data)
                        {
                            if (obj is Sprite)
                            {
                                if (obj.name == AssetReferenceUtilities.FormatName(entry.Item1.name))
                                {
                                    foundMultipleAsset = true;
                                    break;
                                }
                            }
                        }
                    }
                    Assert.True(foundMultipleAsset, $"did not find {entry.Item1.name} in texture2d marked as type multiple asset");
                }
                else
                {
                    Assert.AreEqual(AssetReferenceUtilities.FormatName(entry.Item1.name), filename.Replace(".png", ""), $"Expected to find {entry.Item1.name} in {path}");
                }
            }
        }

        [Test]
        // because the base object was renamed, the subobject should be renamed as well
        [TestCase("Assets/sprites/FolderAndSprites.spriteatlasv2", "00/00-05.png", "7facc2bacc8183d458d6d9bdf9b41d49", "renamed")]
        // the subobject should be renamed event for nested subobjects
        [TestCase("Assets/sprites/FolderAndSprites.spriteatlasv2", "03/00/00-00.png", "5adf8732c8ad2cf438a913a6bb59543a", "renamed")]
        // but if the type is texture2d with type multiple we have to still lookup by name which should stay the same
        [TestCase("Assets/sprites/FolderAndSprites.spriteatlasv2", "00/00-00.png", "065f3a549d47bf443ac987c2c85d288d", "00-00_2")]
        public void TestSpriteRename(string atlasPath, string toRename, string expectedGuid, string expectedName)
        {
            AssetDatabase.DeleteAsset("Assets/sprites/renamed.png");
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            Assert.IsNotNull(atlas);

            var atlasGuid = AssetDatabase.AssetPathToGUID(atlasPath);
            var sprite = new AssetReferenceSprite(atlasGuid);
            sprite.SetEditorAsset(atlas);


            HashSet<(Sprite, string)> map = AssetReferenceUtilities.GetAtlasSpritesAndPackables(ref atlas);
            Assert.AreEqual(atlas.spriteCount, map.Count);
            var foundSprite = false;
            foreach (var entry in map)
            {
                var guid = entry.Item2;
                if (guid == expectedGuid)
                {
                    foundSprite = true;
                    sprite.SetEditorSubObject(entry.Item1);
                    Assert.AreEqual($"{atlasGuid}[{entry.Item1.name}]",sprite.RuntimeKey);
                }
            }
            Assert.True(foundSprite, $"Could not find original sprite {toRename} with guid {expectedGuid}");
            var error = AssetDatabase.MoveAsset($"Assets/sprites/{toRename}", "Assets/sprites/renamed.png");
            Assert.IsEmpty(error);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foundSprite = false;
            map = AssetReferenceUtilities.GetAtlasSpritesAndPackables(ref atlas);
            foreach (var entry in map)
            {
                var guid = entry.Item2;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var filename = Path.GetFileName(path);
                if (guid == expectedGuid)
                {
                    foundSprite = true;
                    Assert.AreEqual("renamed", filename.Replace(".png", ""));
                }
            }
            Assert.True(foundSprite, $"Could not find renamed sprite renamed.png with guid {expectedGuid}");

            // refresh our asset reference and ensure we will see the new name with the same runtime key
            AssetReference subObjectRef = sprite;
            AssetReferenceDrawerUtilities.RefreshSubObjects(ref subObjectRef);
            Assert.AreEqual(expectedName, subObjectRef.SubObjectName);
            Assert.AreEqual(expectedGuid, subObjectRef.SubObjectGUID);
            Assert.AreEqual($"{atlasGuid}[{expectedName}]",subObjectRef.RuntimeKey);
        }

    }
}
