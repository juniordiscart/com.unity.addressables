using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Tests;
using UnityEngine;

namespace Tests.Editor.GUI
{
    public class FastIconLoading: AddressableAssetTestBase
    {
        private struct DisposableTestTexture : IDisposable
        {
            private string m_filePath;

            public DisposableTestTexture(string filePath)
            {
                m_filePath = filePath;
                var t2d = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                t2d.SetPixel(0, 0, Color.red);
                {
                    using var fileStream = new FileStream(m_filePath, FileMode.Create);
                    fileStream.Write(t2d.EncodeToPNG());
                }
                AssetDatabase.ImportAsset(m_filePath);
            }

            public void Dispose()
            {
                if (File.Exists(m_filePath))
                    File.Delete(m_filePath);
            }
        }

        private struct DisposableTestAsset : IDisposable
        {
            private string m_filePath;
            public DisposableTestAsset(string filePath)
            {
                m_filePath = filePath;
                var asset = ScriptableObject.CreateInstance(typeof(SomeScriptableObject));
                AssetDatabase.CreateAsset(asset, filePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            public void Dispose()
            {
                if (File.Exists(m_filePath))
                    File.Delete(m_filePath);
            }
        }

        private struct DisposableTestPrefabPlusVariant : IDisposable
        {
            private string m_filePath;
            private string m_variantPath;

            public DisposableTestPrefabPlusVariant(string filePath, string variantPath)
            {
                m_filePath = filePath;
                m_variantPath = variantPath;

                var go = new GameObject("TestPrefab");
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, filePath);
                var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                PrefabUtility.SaveAsPrefabAsset(root, variantPath);
            }

            public void Dispose()
            {
                if (File.Exists(m_filePath))
                    File.Delete(m_filePath);
                if (File.Exists(m_variantPath))
                    File.Delete(m_variantPath);
            }
        }
        [Test]
        public void TestFastIcon_AssetGUID()
        {
            using var disposableTestAsset = new DisposableTestAsset("Assets/SomeAsset.asset");
            var typeGuid = AddressableAssetEntryIconLazyLoad.GetTypeGuidFromAsset("Assets/SomeAsset.asset");
                Assert.IsTrue(AssetDatabase.GUIDToAssetPath(typeGuid).EndsWith("SomeScriptableObject.cs"));
        }

        [Test]
        public void TestFastIcon_PrefabDetermination()
        {
            using var disposableTestPrefabs = new DisposableTestPrefabPlusVariant("Assets/Prefab.prefab", "Assets/PrefabVariant.prefab");
            var isVariant = AddressableAssetEntryIconLazyLoad.PrefabIcons.IsPrefabVariant("Assets/Prefab.prefab");
            Assert.IsFalse(isVariant);
            isVariant = AddressableAssetEntryIconLazyLoad.PrefabIcons.IsPrefabVariant("Assets/PrefabVariant.prefab");
            Assert.IsTrue(isVariant);
        }

        // don't do this for real, but helpful for us when testing icon validity
        private static Texture CallIconLazyLoadWithPathOnly(string path, bool doFileRead)
        {
            var entry = new AddressableAssetEntry("", "", null, false)
            {
                m_cachedAssetPath = path
            };
            var lazyLoader = new AddressableAssetEntryIconLazyLoad();
            // We don't want to expose it internally but we need to test the icon
            var methodHandle = typeof(AddressableAssetEntryIconLazyLoad).GetMethod("FastIconFromPath",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(methodHandle);
            return methodHandle.Invoke(lazyLoader, new object[] { entry, doFileRead }) as Texture;
        }

        private static void TestPathIsLoadedSameFromAssetDBAsFastPath(string path, bool doFileRead, string expected)
        {
            var tex = CallIconLazyLoadWithPathOnly(path, doFileRead);
            if (expected == null)
            {
                Assert.IsNull(tex);
                return;
            }
            Assert.AreEqual(tex.name, expected);
        }

        [Test]
        public void TestFastIcon_ValidForPrefabsAndAssets()
        {
            using var testTexture = new DisposableTestTexture("Assets/SomeTexture.png");
            using var disposableTestAsset = new DisposableTestAsset("Assets/SomeAsset.asset");
            using var disposableTestPrefabs = new DisposableTestPrefabPlusVariant("Assets/Prefab.prefab", "Assets/PrefabVariant.prefab");

            // Assets which we don't have a fast case for...
            Assert.IsNull(CallIconLazyLoadWithPathOnly("Assets/SomeTexture.png", false), "Texture icon should be null as it cannot be fast loaded");
            Assert.IsNull(CallIconLazyLoadWithPathOnly("Assets/SomeTexture.png", true), "Texture icon should be null as it cannot be fast loaded");

            // We have no default icon for ScriptableObjects - we rely on MiniPreview, so it's Null
            TestPathIsLoadedSameFromAssetDBAsFastPath("Assets/SomeAsset.asset", false, null);
            TestPathIsLoadedSameFromAssetDBAsFastPath("Assets/Prefab.prefab", false, "d_Prefab Icon");
            TestPathIsLoadedSameFromAssetDBAsFastPath("Assets/PrefabVariant.prefab", false, "d_Prefab Icon");

            // And, if we do perform file reads, we expect them to match AssetDB
            TestPathIsLoadedSameFromAssetDBAsFastPath("Assets/SomeAsset.asset", true, "d_ScriptableObject Icon");
            TestPathIsLoadedSameFromAssetDBAsFastPath("Assets/Prefab.prefab", true, "d_Prefab Icon");
            TestPathIsLoadedSameFromAssetDBAsFastPath("Assets/PrefabVariant.prefab", true, "d_PrefabVariant Icon");
        }
    }
}