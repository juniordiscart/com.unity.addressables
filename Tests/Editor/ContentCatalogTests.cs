using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.Utility;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;

namespace UnityEditor.AddressableAssets.Tests
{
    using Debug = UnityEngine.Debug;
    using Random = UnityEngine.Random;

    public class ContentCatalogTests
    {
        List<object> m_Keys;
        List<Type> m_Providers;

        [Serializable]
        public class SerializableKey
        {
            public int index;
            public string path;
        }

        [OneTimeSetUp]
        public void Init()
        {
            m_Keys = new List<object>();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 1000; i++)
            {
                var r = Random.Range(0, 100);
                if (r < 20)
                {
                    int len = Random.Range(1, 5);
                    for (int j = 0; j < len; j++)
                        sb.Append(GUID.Generate().ToString());
                    m_Keys.Add(sb.ToString());
                    sb.Length = 0;
                }
                else if (r < 40)
                {
                    m_Keys.Add((ushort)(i * 13));
                }
                else if (r < 50)
                {
                    m_Keys.Add(i * 13);
                }
                else if (r < 60)
                {
                    m_Keys.Add((uint)(i * 13));
                }
                else if (r < 80)
                {
                    m_Keys.Add(new SerializableKey { index = i, path = GUID.Generate().ToString() });
                }
                else
                {
                    m_Keys.Add(Hash128.Parse(GUID.Generate().ToString()));
                }
            }

            m_Providers = new List<Type>();
            m_Providers.Add(typeof(BundledAssetProvider));
            m_Providers.Add(typeof(AssetBundleProvider));
            m_Providers.Add(typeof(AssetDatabaseProvider));
            m_Providers.Add(typeof(JsonAssetProvider));
            m_Providers.Add(typeof(TextDataProvider));
            m_Providers.Add(typeof(TextDataProvider));
            m_Providers.Add(typeof(BinaryAssetProvider<BinaryContentCatalogData.Serializer>));
        }

        List<T> GetRandomSubset<T>(List<T> keys, int count)
        {
            if (keys.Count == 0 || count == 0)
                return new List<T>();
            var entryKeys = new HashSet<T>();
            for (int k = 0; k < count; k++)
                entryKeys.Add(keys[Random.Range(0, keys.Count)]);
            return entryKeys.ToList();
        }
        [Serializable]
        public class EvenData
        {
            public int index;
            public string path;
        }

        [Serializable]
        public class OddData
        {
            public int index;
            public string path;
        }

        [UnityTest]
        public IEnumerator RunStressContinuously([Values(100)] int locateCallCount, [Values(1000)] int locCount, [Values(128, 256, 512)] int bufferCacheSize)
        {
            var locType = typeof(UnityEngine.Object);
            var catalog = new BinaryContentCatalogData();
            var entries = new List<ContentCatalogDataEntry>();
            var allKeys = new List<object>();

            var deps = new List<List<object>>();
            for (int j = 0; j < 10; j++)
            {
                var depKeys = new List<object>();
                for (int i = 0; i < 5; i++)
                {
                    var d = new ContentCatalogDataEntry(
                        typeof(AssetBundle),
                        $"https://mysuperlongwebservername.com/internalId/path/blah/subdir with a very long name that should get cached and reused/urlstuffetc/assetbundle2345324d2354f3425g345g345g345g{i}.bundle",
                        "AssetBundleProvider",
                        new object[] { $"AssetBundleName_23d234d34f32gf243f23f235g2543g25g123d24{i}.bundle" },
                        null,
                        new AssetBundleRequestOptions { BundleName = "derwrgwergwetrhewrtherth" });
                    entries.Add(d);
                    depKeys.Add(d.Keys[0]);
                }
                deps.Add(depKeys);
            }

            for (int i = 0; i < locCount; i++)
            {
                var entryKeys = new object[] { $"CommonPartOfKey{i%100}/WithALongPath{i%10}/UniquePathOfKey-{i}", $"LabelNameA.{i / 10}", $"LabelNameB.{i / 100}", "CommonLabelA", "CommonLabelB" };
                entries.Add(new ContentCatalogDataEntry(
                    locType,
                    $"InternalAsset/PathInside/AssetBundle/filename{i}.fileExtension",
                    "BundledAssetProvider",
                    entryKeys,
                    deps[Random.Range(0, deps.Count)]));
                allKeys.Add(entryKeys[0]);
            }
            catalog.SetData(entries);
            var data = catalog.SerializeToByteArray();
            var loadedCatalog = new BinaryContentCatalogData(new BinaryStorageBuffer.Reader(data, bufferCacheSize, 0, new BinaryContentCatalogData.Serializer()));
            var locator = loadedCatalog.CreateCustomLocator("", null) as BinaryContentCatalogData.ResourceLocator;
            yield return null;
            int frameCount = 1000;
            for (int x = 0; x < frameCount; x++)
            {
                for (int i = 0; i < locateCallCount; i++)
                {
                    locator.Locate(allKeys[Random.Range(0, allKeys.Count)], locType, out var locs);
                    foreach(var l in locs)
                    {
                        var id = l.InternalId;
                        var pk = l.PrimaryKey;
                        var t = l.ResourceType;
                        var p = l.ProviderId;
                        var h = l.DependencyHashCode;
                        if (l.HasDependencies)
                        {
                            foreach (var d in l.Dependencies)
                            {
                                id = d.InternalId;
                                pk = d.PrimaryKey;
                                t = d.ResourceType;
                                p = d.ProviderId;
                                h = d.DependencyHashCode;
                                var o = d.Data as AssetBundleRequestOptions;
                            }
                        }
                    }
                }
                yield return null;
            }
        }

        [Test]
        public void BinaryCatalogSerializerWithInternalIdResolvingDisabled_DoesNotModifyInternalIds()
        {
            var internalId = "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/file.path";
            var locType = typeof(UnityEngine.Object);
            var catalog = new BinaryContentCatalogData();
            var entries = new List<ContentCatalogDataEntry>();
            entries.Add(new ContentCatalogDataEntry(locType, internalId, "", new string[] { "a" }));
            catalog.SetData(entries);
            var data = catalog.SerializeToByteArray();
            {
                var resolvedCatalog = new BinaryContentCatalogData(new BinaryStorageBuffer.Reader(data, 128, 0, new BinaryContentCatalogData.Serializer()));
                var resolvedLocator = resolvedCatalog.CreateCustomLocator("", null) as BinaryContentCatalogData.ResourceLocator;
                resolvedLocator.Locate("a", locType, out var locs);
                Assert.AreEqual($"{UnityEngine.AddressableAssets.Addressables.RuntimePath}/file.path", locs[0].InternalId);
            }

            {
                var nonresolvedCatalog = new BinaryContentCatalogData(new BinaryStorageBuffer.Reader(data, 128, 0, new BinaryContentCatalogData.Serializer().WithInternalIdResolvingDisabled()));
                var nonresolvedLocator = nonresolvedCatalog.CreateCustomLocator("", null) as BinaryContentCatalogData.ResourceLocator;
                nonresolvedLocator.Locate("a", locType, out var locs);
                Assert.AreEqual(internalId, locs[0].InternalId);
            }

        }

        [Test]
        public void BinaryCatalogCacheStress([Values(1000)] int locateCallCount, [Values(1000)] int locCount, [Values(64, 256, 1024, 4096)] int bufferCacheSize)
        {
            var locType = typeof(UnityEngine.Object);
            var catalog = new BinaryContentCatalogData();
            var entries = new List<ContentCatalogDataEntry>();
            var allKeys = new List<object>();
            Func<int, string> internalIdFunc = i => $"https://mysuperlongwebservername.com/internalId/path/blah/subdir/urlstuffetc/{i}.fileextension";
            Func<int, object[]> keysFunc = i => new object[] { $"LongKeyName.{i}", $"LabelNameA.{i / 10}", $"LabelNameB.{i / 100}", "CommonLabelA", "CommonLabelB" };
            var providerId = "provider Id goes here";
            for (int i = 0; i < locCount; i++)
            {
                var entryKeys = keysFunc(i);
                entries.Add(new ContentCatalogDataEntry(
                    locType,
                    internalIdFunc(i),
                    providerId,
                    entryKeys,
                    null));
                allKeys.AddRange(entryKeys);
            }
            catalog.SetData(entries);
            var data = catalog.SerializeToByteArray();
            var loadedCatalog = new BinaryContentCatalogData(new BinaryStorageBuffer.Reader(data, bufferCacheSize, 0, new BinaryContentCatalogData.Serializer()));
            var locator = loadedCatalog.CreateCustomLocator("", null) as BinaryContentCatalogData.ResourceLocator;
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < locateCallCount; i++)
            {
                var index = Random.Range(0, allKeys.Count);

                Assert.IsTrue(locator.Locate(allKeys[index], locType, out var locs));
                for (int j = 0; j < 10; j++)
                {
                    var l = locs[Random.Range(0, locs.Count)];
                    var locIndex = int.Parse(l.PrimaryKey.Substring(l.PrimaryKey.LastIndexOf('.')+1));
                    Assert.AreEqual(internalIdFunc(locIndex), l.InternalId);
                    Assert.AreEqual(keysFunc(locIndex)[0], l.PrimaryKey);
                    Assert.AreEqual(locType, l.ResourceType);
                    Assert.AreEqual(providerId, l.ProviderId);
                    Assert.AreEqual(-1, l.DependencyHashCode);
                }
            }
            sw.Stop();
        }

        [Test]
        public void AssetBundleRequestOptionsTest()
        {
            var options = new AssetBundleRequestOptions
            {
                ChunkedTransfer = true,
                Crc = 123,
                Hash = new Hash128(1, 2, 3, 4).ToString(),
                RedirectLimit = 4,
                RetryCount = 7,
                Timeout = 12,
                AssetLoadMode = AssetLoadMode.AllPackedAssetsAndDependencies
            };
            var dataEntry = new ContentCatalogDataEntry(typeof(ContentCatalogData), "internalId", "provider", new object[] { 1 }, null, options);
            var entries = new List<ContentCatalogDataEntry>();
            entries.Add(dataEntry);
            var ccData = new BinaryContentCatalogData("TestCatalog");
            ccData.SetData(entries);
            var data = ccData.SerializeToByteArray();
            ccData = new BinaryContentCatalogData(new BinaryStorageBuffer.Reader(data, 1024, 0, new BinaryContentCatalogData.Serializer()));
            var locator = ccData.CreateCustomLocator("");
            IList<IResourceLocation> locations;
            if (!locator.Locate(1, typeof(object), out locations))
                Assert.Fail("Unable to locate resource location");
            var loc = locations[0];
            var locOptions = loc.Data as AssetBundleRequestOptions;
            Assert.IsNotNull(locOptions);
            Assert.AreEqual(locOptions.ChunkedTransfer, options.ChunkedTransfer);
            Assert.AreEqual(locOptions.Crc, options.Crc);
            Assert.AreEqual(locOptions.Hash, options.Hash);
            Assert.AreEqual(locOptions.RedirectLimit, options.RedirectLimit);
            Assert.AreEqual(locOptions.RetryCount, options.RetryCount);
            Assert.AreEqual(locOptions.Timeout, options.Timeout);
            Assert.AreEqual(locOptions.AssetLoadMode, options.AssetLoadMode);
        }

        // Exposes BinaryContentCatalogData's protected header constants (as a subclass) so
        // tests below don't hardcode the live magic/version values.
        class TestableBinaryContentCatalogData : BinaryContentCatalogData
        {
            public const int LiveMagic = kMagic;
            public const int LiveVersion = kVersion;
        }

        static byte[] BuildMinimalSerializedCatalog()
        {
            var dataEntry = new ContentCatalogDataEntry(typeof(ContentCatalogData), "internalId", "provider", new object[] {1});
            var ccData = new BinaryContentCatalogData("TestCatalog");
            ccData.SetData(new List<ContentCatalogDataEntry> {dataEntry});
            return ccData.SerializeToByteArray();
        }

        [Test]
        public void BinaryCatalog_LogsExceptionAndReturnsNull_OnVersionMismatch()
        {
            // The public write path always stamps the current version, so simulate a catalog
            // written by an older package version by corrupting the header after serializing.
            // BinaryStorageBuffer.Reader.ReadObject<T> catches deserialization exceptions,
            // logs them via Debug.LogException, and returns default -- it does not rethrow.
            var data = BuildMinimalSerializedCatalog();
            var corruptedVersion = TestableBinaryContentCatalogData.LiveVersion + 1;
            Array.Copy(BitConverter.GetBytes(corruptedVersion), 0, data, 4, 4);

            var reader = new BinaryStorageBuffer.Reader(data, 1024, 0, new BinaryContentCatalogData.Serializer());
            LogAssert.Expect(LogType.Exception,
                $"Exception: Catalog data version mismatch: expected {TestableBinaryContentCatalogData.LiveVersion}, found {corruptedVersion}. Rebuild your Addressables content with the current package version.");
            var result = reader.ReadObject<BinaryContentCatalogData>(0, out _, false);
            Assert.IsNull(result);
        }

        [Test]
        public void BinaryCatalog_LogsExceptionAndReturnsNull_OnMagicMismatch()
        {
            var data = BuildMinimalSerializedCatalog();
            Array.Copy(BitConverter.GetBytes(TestableBinaryContentCatalogData.LiveMagic + 1), 0, data, 0, 4);

            var reader = new BinaryStorageBuffer.Reader(data, 1024, 0, new BinaryContentCatalogData.Serializer());
            LogAssert.Expect(LogType.Exception, "Exception: Invalid header data!!!");
            var result = reader.ReadObject<BinaryContentCatalogData>(0, out _, false);
            Assert.IsNull(result);
        }

        [Test]
        public void VerifySerialization()
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var catalog = new JsonContentCatalogData();
            var entries = new List<ContentCatalogDataEntry>();
            var availableKeys = new List<object>();

            for (int i = 0; i < 1000; i++)
            {
                var internalId = "Assets/TestPath/" + GUID.Generate() + ".asset";
                var eKeys = GetRandomSubset(m_Keys, Random.Range(1, 5));
                object data;
                if (i % 2 == 0)
                    data = new EvenData {index = i, path = internalId};
                else
                    data = new OddData {index = i, path = internalId};

                var e = new ContentCatalogDataEntry(typeof(ContentCatalogData), internalId, m_Providers[Random.Range(0, m_Providers.Count)].FullName, eKeys,
                    GetRandomSubset(availableKeys, Random.Range(0, 1)), data);
                availableKeys.Add(eKeys[0]);
                entries.Add(e);
            }

            catalog.SetData(entries);
            sw.Stop();
            var t = sw.Elapsed.TotalMilliseconds;
            sw.Reset();
            sw.Start();
            var locMap = catalog.CreateLocator();
            sw.Stop();
            Debug.LogFormat("Create: {0}ms, Load: {1}ms", t, sw.Elapsed.TotalMilliseconds);

            foreach (var k in locMap.Locations)
            {
                foreach (var loc in k.Value)
                {
                    var entry = entries.Find(e => e.InternalId == loc.InternalId);
                    Assert.AreEqual(entry.Provider, loc.ProviderId);

                    var deps = loc.Dependencies;
                    if (deps != null)
                    {
                        foreach (var ed in entry.Dependencies)
                        {
                            IList<IResourceLocation> depList;
                            Assert.IsTrue(locMap.Locate(ed, typeof(object), out depList));
                            for (int i = 0; i < depList.Count; i++)
                                Assert.AreEqual(depList[i].InternalId, deps[i].InternalId);
                        }
                    }
                }
            }
        }

        [Test]
        public void VerifyDependencyHashCalculation()
        {
            var catalog = new JsonContentCatalogData();
            Dictionary<int, object> hashSources = new Dictionary<int, object>();

            var dummyValues = new List<object>()
            {
                "<WILL-BE-REPLACED>",
                "startup-shared_assets_assets/fx_data/textures.bundle",
                "shared_assets_assets/fx_data/materials.bundle",
                "shaders_assets_all.bundle",
                "music_assets_music/maptheme6final.bundle",
                "fx_tex_assets_all.bundle",
                "shared_assets_assets/textures/ui/campain_act02.bundle",
                "shared_assets_assets/fx_data/meshes.bundle",
                "startup-shared_assets_assets/textures/ui/campain_act02.bundle",
                "startup-shared_assets_assets/textures/ui/campainart.bundle",
                "startup-shared_assets_assets/fx_data/materials.bundle",
                "shared_assets_assets/textures/ui/valleyoftreasures.bundle",
                "startup-shared_assets_assets/fx_data/meshes.bundle",
                "startup_UnityBuiltInAssets.bundle"
            };

            dummyValues[0] = "maps_assets_ref/valley1.bundle";
            var hashPart1 = dummyValues[0].GetHashCode();
            var hashSum1 = catalog.CalculateCollectedHash(dummyValues, hashSources);

            var dummyValues2 = new List<object>()
            {
                "maps_assets_ref/valley1.bundle",
                "startup-shared_assets_assets/fx_data/textures.bundle",
                "shared_assets_assets/fx_data/materials.bundle",
                "shaders_assets_all.bundle",
                "music_assets_music/maptheme6final.bundle",
                "fx_tex_assets_all.bundle",
                "shared_assets_assets/textures/ui/campain_act02.bundle",
                "shared_assets_assets/fx_data/meshes.bundle",
                "startup-shared_assets_assets/textures/ui/campain_act02.bundle",
                "startup-shared_assets_assets/textures/ui/campainart.bundle",
                "startup-shared_assets_assets/fx_data/materials.bundle",
                "shared_assets_assets/textures/ui/valleyoftreasures.bundle",
                "startup-shared_assets_assets/fx_data/meshes.bundle",
                "startup_UnityBuiltInAssets.bundle"
            };

            var hashSum1DifferentList = catalog.CalculateCollectedHash(dummyValues2, hashSources);

            dummyValues[0] = "maps_assets_ref/valley3.bundle";
            var hashPart2 = dummyValues[0].GetHashCode();
            var hashSum2 = catalog.CalculateCollectedHash(dummyValues, hashSources);

            Assert.AreEqual(hashSum1, hashSum1DifferentList);
            Assert.AreNotEqual(hashPart1, hashPart2);
            Assert.AreNotEqual(hashSum1, hashSum2);
        }

        [Test]
        public void VerifyEnumerableHashCalculation()
        {
            var dummyValues = new List<object>()
            {
                "maps_assets_ref/valley1.bundle",
                "startup-shared_assets_assets/fx_data/textures.bundle",
                "shared_assets_assets/fx_data/materials.bundle",
                "shaders_assets_all.bundle",
                "music_assets_music/maptheme6final.bundle",
                "fx_tex_assets_all.bundle",
                "shared_assets_assets/textures/ui/campain_act02.bundle",
                "shared_assets_assets/fx_data/meshes.bundle",
                "startup-shared_assets_assets/textures/ui/campain_act02.bundle",
                "startup-shared_assets_assets/textures/ui/campainart.bundle",
                "startup-shared_assets_assets/fx_data/materials.bundle",
                "shared_assets_assets/textures/ui/valleyoftreasures.bundle",
                "startup-shared_assets_assets/fx_data/meshes.bundle",
                "startup_UnityBuiltInAssets.bundle"
            };

            var dummyValues2 = new List<object>()
            {
                "maps_assets_ref/valley1.bundle",
                "startup-shared_assets_assets/fx_data/textures.bundle",
                "shared_assets_assets/fx_data/materials.bundle",
                "shaders_assets_all.bundle",
                "music_assets_music/maptheme6final.bundle",
                "fx_tex_assets_all.bundle",
                "shared_assets_assets/textures/ui/campain_act02.bundle",
                "shared_assets_assets/fx_data/meshes.bundle",
                "startup-shared_assets_assets/textures/ui/campain_act02.bundle",
                "startup-shared_assets_assets/textures/ui/campainart.bundle",
                "startup-shared_assets_assets/fx_data/materials.bundle",
                "shared_assets_assets/textures/ui/valleyoftreasures.bundle",
                "startup-shared_assets_assets/fx_data/meshes.bundle",
                "startup_UnityBuiltInAssets.bundle"
            };

            var hash1 = JsonContentCatalogData.GetHashCodeForEnumerable(dummyValues);
            var hash2 = JsonContentCatalogData.GetHashCodeForEnumerable(dummyValues2);
            Assert.AreEqual(hash1, hash2);

            dummyValues[0] = "maps_assets_ref/valley3.bundle";
            var hash3 = JsonContentCatalogData.GetHashCodeForEnumerable(dummyValues);
            Assert.AreNotEqual(hash1, hash3);
        }

        [TestCase("0#b", "ab", new string[] {"a"})]
        [TestCase("1#b", "bb", new string[] {"a", "b"})]
        [TestCase("b", "b", new string[] {"a"})]
        [TestCase("b", "b", new string[] {})]
        [TestCase("b", "b", null)]
        [TestCase("x#b", "x#b", new string[] {"a"})]
        [Test]
        public void ContentCatalogData_ExpandInternalId_GeneratesExpectedResults(string input, string expected, string[] prefixes)
        {
            Assert.AreEqual(expected, JsonContentCatalogData.ExpandInternalId(prefixes, input));
        }

        [Test]
        public void SerializationUtility_ReadWrite_Int32()
        {
            var data = new byte[100];
            for (int i = 0; i < 1000; i++)
            {
                var val = Random.Range(int.MinValue, int.MaxValue);
                var off = Random.Range(0, data.Length - sizeof(int));
                Assert.AreEqual(off + sizeof(int), SerializationUtilities.WriteInt32ToByteArray(data, val, off));
                Assert.AreEqual(val, SerializationUtilities.ReadInt32FromByteArray(data, off));
            }
        }

        string testData =
            @"{""m_LocatorId"":""AddressablesMainContentCatalog"",""m_InstanceProviderData"":{""m_Id"":""UnityEngine.ResourceManagement.ResourceProviders.InstanceProvider"",""m_ObjectType"":{""m_AssemblyName"":""Unity.ResourceManager, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",""m_ClassName"":""UnityEngine.ResourceManagement.ResourceProviders.InstanceProvider""},""m_Data"":""""},""m_SceneProviderData"":{""m_Id"":""UnityEngine.ResourceManagement.ResourceProviders.SceneProvider"",""m_ObjectType"":{""m_AssemblyName"":""Unity.ResourceManager, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",""m_ClassName"":""UnityEngine.ResourceManagement.ResourceProviders.SceneProvider""},""m_Data"":""""},""m_ResourceProviderData"":[{""m_Id"":""UnityEngine.ResourceManagement.ResourceProviders.AssetBundleProvider"",""m_ObjectType"":{""m_AssemblyName"":""Unity.ResourceManager, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",""m_ClassName"":""UnityEngine.ResourceManagement.ResourceProviders.AssetBundleProvider""},""m_Data"":""""},{""m_Id"":""UnityEngine.ResourceManagement.ResourceProviders.BundledAssetProvider"",""m_ObjectType"":{""m_AssemblyName"":""Unity.ResourceManager, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",""m_ClassName"":""UnityEngine.ResourceManagement.ResourceProviders.BundledAssetProvider""},""m_Data"":""""},{""m_Id"":""UnityEngine.ResourceManagement.ResourceProviders.BundledAssetProvider"",""m_ObjectType"":{""m_AssemblyName"":""Unity.ResourceManager, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",""m_ClassName"":""UnityEngine.ResourceManagement.ResourceProviders.BundledAssetProvider""},""m_Data"":""""}],""m_ProviderIds"":[""UnityEngine.ResourceManagement.ResourceProviders.AssetBundleProvider"",""UnityEngine.ResourceManagement.ResourceProviders.BundledAssetProvider""],""m_InternalIds"":[""{UnityEngine.AddressableAssets.Addressables.RuntimePath}/StandaloneWindows64/defaultlocalgroup_assets_all_d4ed3973c342e6f06795a0f8daaebaad.bundle"",""{UnityEngine.AddressableAssets.Addressables.RuntimePath}/StandaloneWindows64/defaultlocalgroup_unitybuiltinassets_8f144cd21867dc83f60ecd3c93095b52.bundle"",""{UnityEngine.AddressableAssets.Addressables.RuntimePath}/StandaloneWindows64/defaultlocalgroup_scenes_all_e91ebe7804da861b4deb67a340282541.bundle"",""Assets/New Material.mat"",""Assets/swef.unity""],""m_KeyDataString"":""CQAAAABEAAAAZGVmYXVsdGxvY2FsZ3JvdXBfYXNzZXRzX2FsbF9kNGVkMzk3M2MzNDJlNmYwNjc5NWEwZjhkYWFlYmFhZC5idW5kbGUATQAAAGRlZmF1bHRsb2NhbGdyb3VwX3VuaXR5YnVpbHRpbnNoYWRlcnNfOGYxNDRjZDIxODY3ZGM4M2Y2MGVjZDNjOTMwOTViNTIuYnVuZGxlAEQAAABkZWZhdWx0bG9jYWxncm91cF9zY2VuZXNfYWxsX2U5MWViZTc4MDRkYTg2MWI0ZGViNjdhMzQwMjgyNTQxLmJ1bmRsZQAXAAAAQXNzZXRzL05ldyBNYXRlcmlhbC5tYXQAIAAAADNlN2JmNTA3OTRhNzEyMjQ2YWU0ZGNiZTdhODQyOGM4ABEAAABBc3NldHMvc3dlZi51bml0eQAgAAAAYjY4MDdmODNlMWU0ODc2NGM4MjMyM2ZkNTExZTY0NjgEKToMuAQiVa/u"",""m_BucketDataString"":""CQAAAAQAAAABAAAAAAAAAE0AAAABAAAAAQAAAJ8AAAABAAAAAgAAAOgAAAABAAAAAwAAAAQBAAABAAAAAwAAACkBAAABAAAABAAAAD8BAAABAAAABAAAAGQBAAACAAAAAAAAAAEAAABpAQAAAgAAAAIAAAABAAAA"",""m_EntryDataString"":""BQAAAAAAAAAAAAAA/////wAAAAAAAAAAAAAAAAAAAAABAAAAAAAAAP////8AAAAAhQIAAAEAAAAAAAAAAgAAAAAAAAD/////AAAAADQFAAACAAAAAAAAAAMAAAABAAAABwAAACk6DLj/////AwAAAAEAAAAEAAAAAQAAAAgAAAAiVa/u/////wUAAAACAAAA"",""m_ExtraDataString"":""B0xVbml0eS5SZXNvdXJjZU1hbmFnZXIsIFZlcnNpb249MC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1udWxsSlVuaXR5RW5naW5lLlJlc291cmNlTWFuYWdlbWVudC5SZXNvdXJjZVByb3ZpZGVycy5Bc3NldEJ1bmRsZVJlcXVlc3RPcHRpb25z6AEAAHsAIgBtAF8ASABhAHMAaAAiADoAIgBkADQAZQBkADMAOQA3ADMAYwAzADQAMgBlADYAZgAwADYANwA5ADUAYQAwAGYAOABkAGEAYQBlAGIAYQBhAGQAIgAsACIAbQBfAEMAcgBjACIAOgAyADAAMgAxADcANAA3ADAAOQA5ACwAIgBtAF8AVABpAG0AZQBvAHUAdAAiADoAMAAsACIAbQBfAEMAaAB1AG4AawBlAGQAVAByAGEAbgBzAGYAZQByACIAOgBmAGEAbABzAGUALAAiAG0AXwBSAGUAZABpAHIAZQBjAHQATABpAG0AaQB0ACIAOgAtADEALAAiAG0AXwBSAGUAdAByAHkAQwBvAHUAbgB0ACIAOgAwACwAIgBtAF8AQgB1AG4AZABsAGUATgBhAG0AZQAiADoAIgA5ADIAZAAwAGYAOABiAGMAOQBkAGYAZABjADAAMwBlADEAMABkAGYAMgBmADMAYgAzAGIANABjADgAMgA3AGUAIgAsACIAbQBfAEIAdQBuAGQAbABlAFMAaQB6AGUAIgA6ADIANQAyADgALAAiAG0AXwBVAHMAZQBDAHIAYwBGAG8AcgBDAGEAYwBoAGUAZABCAHUAbgBkAGwAZQBzACIAOgB0AHIAdQBlAH0AB0xVbml0eS5SZXNvdXJjZU1hbmFnZXIsIFZlcnNpb249MC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1udWxsSlVuaXR5RW5naW5lLlJlc291cmNlTWFuYWdlbWVudC5SZXNvdXJjZVByb3ZpZGVycy5Bc3NldEJ1bmRsZVJlcXVlc3RPcHRpb25zEgIAAHsAIgBtAF8ASABhAHMAaAAiADoAIgA4AGYAMQA0ADQAYwBkADIAMQA4ADYANwBkAGMAOAAzAGYANgAwAGUAYwBkADMAYwA5ADMAMAA5ADUAYgA1ADIAIgAsACIAbQBfAEMAcgBjACIAOgAzADgAMQAzADcAMgA0ADgANQA5ACwAIgBtAF8AVABpAG0AZQBvAHUAdAAiADoAMAAsACIAbQBfAEMAaAB1AG4AawBlAGQAVAByAGEAbgBzAGYAZQByACIAOgBmAGEAbABzAGUALAAiAG0AXwBSAGUAZABpAHIAZQBjAHQATABpAG0AaQB0ACIAOgAtADEALAAiAG0AXwBSAGUAdAByAHkAQwBvAHUAbgB0ACIAOgAwACwAIgBtAF8AQgB1AG4AZABsAGUATgBhAG0AZQAiADoAIgBmAGMAOAAyAGEAMAAxAGUAYgAwAGEAMgA0AGIAOQBiAGQAOQBjADAAZQBjADEAZAAzAGEAOQBiADIANgA1ADUAXwB1AG4AaQB0AHkAYgB1AGkAbAB0AGkAbgBzAGgAYQBkAGUAcgBzACIALAAiAG0AXwBCAHUAbgBkAGwAZQBTAGkAegBlACIAOgA0ADQANAA1ADQALAAiAG0AXwBVAHMAZQBDAHIAYwBGAG8AcgBDAGEAYwBoAGUAZABCAHUAbgBkAGwAZQBzACIAOgB0AHIAdQBlAH0AB0xVbml0eS5SZXNvdXJjZU1hbmFnZXIsIFZlcnNpb249MC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1udWxsSlVuaXR5RW5naW5lLlJlc291cmNlTWFuYWdlbWVudC5SZXNvdXJjZVByb3ZpZGVycy5Bc3NldEJ1bmRsZVJlcXVlc3RPcHRpb25z6AEAAHsAIgBtAF8ASABhAHMAaAAiADoAIgBlADkAMQBlAGIAZQA3ADgAMAA0AGQAYQA4ADYAMQBiADQAZABlAGIANgA3AGEAMwA0ADAAMgA4ADIANQA0ADEAIgAsACIAbQBfAEMAcgBjACIAOgAzADQAMAA1ADQAMwA2ADQANQAxACwAIgBtAF8AVABpAG0AZQBvAHUAdAAiADoAMAAsACIAbQBfAEMAaAB1AG4AawBlAGQAVAByAGEAbgBzAGYAZQByACIAOgBmAGEAbABzAGUALAAiAG0AXwBSAGUAZABpAHIAZQBjAHQATABpAG0AaQB0ACIAOgAtADEALAAiAG0AXwBSAGUAdAByAHkAQwBvAHUAbgB0ACIAOgAwACwAIgBtAF8AQgB1AG4AZABsAGUATgBhAG0AZQAiADoAIgA5ADEANwBlADUANQAzAGQAZQBiAGQAOAAyADMAOABkAGMAMgBjADIAZAA2ADIANQBkADAAZgA4ADUAOQA0AGMAIgAsACIAbQBfAEIAdQBuAGQAbABlAFMAaQB6AGUAIgA6ADgANwA4ADIALAAiAG0AXwBVAHMAZQBDAHIAYwBGAG8AcgBDAGEAYwBoAGUAZABCAHUAbgBkAGwAZQBzACIAOgB0AHIAdQBlAH0A"",""m_Keys"":[""defaultlocalgroup_assets_all_d4ed3973c342e6f06795a0f8daaebaad.bundle"",""defaultlocalgroup_unitybuiltinassets_8f144cd21867dc83f60ecd3c93095b52.bundle"",""defaultlocalgroup_scenes_all_e91ebe7804da861b4deb67a340282541.bundle"",""Assets/New Material.mat"",""3e7bf50794a712246ae4dcbe7a8428c8"",""Assets/swef.unity"",""b6807f83e1e48764c82323fd511e6468"",""-1207158231"",""-290499294""],""m_resourceTypes"":[{""m_AssemblyName"":""Unity.ResourceManager, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",""m_ClassName"":""UnityEngine.ResourceManagement.ResourceProviders.IAssetBundleResource""},{""m_AssemblyName"":""UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",""m_ClassName"":""UnityEngine.Material""},{""m_AssemblyName"":""Unity.ResourceManager, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",""m_ClassName"":""UnityEngine.ResourceManagement.ResourceProviders.SceneInstance""}]}";

        [Test]
        public void CanLoad_OldCatalogFormat()
        {
            var ccd = JsonUtility.FromJson<JsonContentCatalogData>(testData);
            Assert.IsNotNull(ccd);
            var loc = ccd.CreateLocator();
            Assert.IsNotNull(loc);
            Assert.AreEqual(9, loc.Keys.Count());
            foreach (var k in loc.Keys)
            {
                Assert.IsTrue(loc.Locate(k, null, out var res));
                Assert.IsNotEmpty(res[0].PrimaryKey);
                Assert.IsNotEmpty(res[0].InternalId);
                Assert.IsNotEmpty(res[0].ProviderId);
                Assert.IsNotNull(res[0].ResourceType);
            }
        }

        // JSON-format counterparts to the cross-runtime TypeNameResolver coverage in
        // BinaryStorageBufferTests.cs. These drive real JsonUtility.ToJson/FromJson round-trips
        // (not just in-memory SetData/CreateLocator) to prove JsonContentCatalogData shares the
        // same runtime-portable type resolution as the binary catalog format.

        [Test]
        public void JsonCatalog_ResolvesType_WhenAssemblyNotFound()
        {
            // Simulate a catalog written by a different runtime: the assembly name on disk
            // can't be loaded here, but the corelib class name alone is enough to resolve.
            var catalog = new JsonContentCatalogData();
            var entry = new ContentCatalogDataEntry(typeof(string), "Assets/foo.asset", "SomeProvider", new object[] {"key"});
            catalog.SetData(new List<ContentCatalogDataEntry> {entry});

            var json = JsonUtility.ToJson(catalog);
            Assert.IsTrue(json.Contains("\"m_ClassName\":\"System.String\""), "test JSON missing expected resource type entry");
            json = json.Replace("\"m_AssemblyName\":\"\",\"m_ClassName\":\"System.String\"",
                "\"m_AssemblyName\":\"NonExistentAssembly.ForTesting\",\"m_ClassName\":\"System.String\"");

            var loaded = JsonUtility.FromJson<JsonContentCatalogData>(json);
            var loc = loaded.CreateLocator();
            Assert.IsTrue(loc.Locate("key", null, out var res));
            Assert.AreEqual(typeof(string), res[0].ResourceType);
        }

        [Test]
        public void JsonCatalog_RoundTrip_NonCore_StripsVersionInfo()
        {
            var catalog = new JsonContentCatalogData();
            var entry = new ContentCatalogDataEntry(typeof(Vector3), "Assets/foo.asset", "SomeProvider", new object[] {"key"});
            catalog.SetData(new List<ContentCatalogDataEntry> {entry});

            var json = JsonUtility.ToJson(catalog);
            Assert.IsFalse(json.Contains("Version="), "version info must be stripped");
            Assert.IsFalse(json.Contains("PublicKeyToken="), "public key token must be stripped");
            Assert.IsTrue(json.Contains("\"m_AssemblyName\":\"UnityEngine.CoreModule\""), "non-corelib assembly should be the simple name only");

            var loaded = JsonUtility.FromJson<JsonContentCatalogData>(json);
            var loc = loaded.CreateLocator();
            Assert.IsTrue(loc.Locate("key", null, out var res));
            Assert.AreEqual(typeof(Vector3), res[0].ResourceType);
        }

        [Test]
        public void JsonCatalog_RoundTrip_Corelib_UsesNullAssemblySentinel()
        {
            var catalog = new JsonContentCatalogData();
            var entry = new ContentCatalogDataEntry(typeof(string), "Assets/foo.asset", "SomeProvider", new object[] {"key"});
            catalog.SetData(new List<ContentCatalogDataEntry> {entry});

            var json = JsonUtility.ToJson(catalog);
            Assert.IsTrue(json.Contains("\"m_AssemblyName\":\"\",\"m_ClassName\":\"System.String\""),
                "corelib assembly should be encoded as the empty/null sentinel");

            var loaded = JsonUtility.FromJson<JsonContentCatalogData>(json);
            var loc = loaded.CreateLocator();
            Assert.IsTrue(loc.Locate("key", null, out var res));
            Assert.AreEqual(typeof(string), res[0].ResourceType);
        }
    }
}
