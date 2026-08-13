#if UNITY_6000_0_OR_NEWER
using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEditor.AddressableAssets.Tests
{
    /// <summary>
    /// Tests for the ContentStateSerializer class.
    /// These tests verify serialization and deserialization without using BinaryFormatter.
    /// </summary>
    public class ContentStateSerializerTests
    {
        private string m_TempDir;

        [SetUp]
        public void Setup()
        {
            m_TempDir = Path.Combine(Path.GetTempPath(), "ContentStateSerializerTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(m_TempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_TempDir))
            {
                Directory.Delete(m_TempDir, true);
            }
        }

        [Test]
        public void Serialize_CreatesFileWithVersionMarker()
        {
            // Arrange
            var contentState = CreateSampleContentState();
            var path = Path.Combine(m_TempDir, "test_content_state.bin");

            // Act
            ContentStateSerializer.Serialize(contentState, path);

            // Assert
            Assert.IsTrue(File.Exists(path), "File should be created");
            Assert.IsFalse(ContentStateSerializer.IsLegacyFormat(path), "New file should not be legacy format");
        }

        [Test]
        public void Serialize_Deserialize_RoundTrip_PreservesPlayerVersion()
        {
            // Arrange
            var original = CreateSampleContentState();
            original.playerVersion = "test-version-12345";
            var path = Path.Combine(m_TempDir, "roundtrip_player_version.bin");

            // Act
            ContentStateSerializer.Serialize(original, path);
            var result = ContentStateSerializer.Deserialize(path);

            // Assert
            Assert.AreEqual("test-version-12345", result.playerVersion);
        }

        [Test]
        public void Serialize_Deserialize_RoundTrip_PreservesEditorVersion()
        {
            // Arrange
            var original = CreateSampleContentState();
            original.editorVersion = "2024.1.0f1";
            var path = Path.Combine(m_TempDir, "roundtrip_editor_version.bin");

            // Act
            ContentStateSerializer.Serialize(original, path);
            var result = ContentStateSerializer.Deserialize(path);

            // Assert
            Assert.AreEqual("2024.1.0f1", result.editorVersion);
        }

        [Test]
        public void Serialize_Deserialize_RoundTrip_PreservesRemoteCatalogLoadPath()
        {
            // Arrange
            var original = CreateSampleContentState();
            original.remoteCatalogLoadPath = "https://cdn.example.com/catalog.json";
            var path = Path.Combine(m_TempDir, "roundtrip_catalog_path.bin");

            // Act
            ContentStateSerializer.Serialize(original, path);
            var result = ContentStateSerializer.Deserialize(path);

            // Assert
            Assert.AreEqual("https://cdn.example.com/catalog.json", result.remoteCatalogLoadPath);
        }

        [Test]
        public void Serialize_Deserialize_RoundTrip_PreservesCachedInfos()
        {
            // Arrange
            var original = CreateSampleContentState();
            var path = Path.Combine(m_TempDir, "roundtrip_cached_infos.bin");

            // Act
            ContentStateSerializer.Serialize(original, path);
            var result = ContentStateSerializer.Deserialize(path);

            // Assert
            Assert.IsNotNull(result.cachedInfos);
            Assert.AreEqual(original.cachedInfos.Length, result.cachedInfos.Length);
            Assert.AreEqual(original.cachedInfos[0].groupGuid, result.cachedInfos[0].groupGuid);
            Assert.AreEqual(original.cachedInfos[0].bundleFileId, result.cachedInfos[0].bundleFileId);
        }

        [Test]
        public void Serialize_Deserialize_RoundTrip_PreservesCachedBundles()
        {
            // Arrange
            var original = CreateSampleContentState();
            var path = Path.Combine(m_TempDir, "roundtrip_cached_bundles.bin");

            // Act
            ContentStateSerializer.Serialize(original, path);
            var result = ContentStateSerializer.Deserialize(path);

            // Assert
            Assert.IsNotNull(result.cachedBundles);
            Assert.AreEqual(original.cachedBundles.Length, result.cachedBundles.Length);
            Assert.AreEqual(original.cachedBundles[0].bundleFileId, result.cachedBundles[0].bundleFileId);
        }

        [Test]
        public void Serialize_Deserialize_RoundTrip_PreservesAssetBundleRequestOptions()
        {
            // Arrange
            var original = CreateSampleContentState();
            var originalOptions = (AssetBundleRequestOptions)original.cachedBundles[0].data;
            var path = Path.Combine(m_TempDir, "roundtrip_bundle_options.bin");

            // Act
            ContentStateSerializer.Serialize(original, path);
            var result = ContentStateSerializer.Deserialize(path);

            // Assert
            var resultOptions = result.cachedBundles[0].data as AssetBundleRequestOptions;
            Assert.IsNotNull(resultOptions);
            Assert.AreEqual(originalOptions.BundleName, resultOptions.BundleName);
            Assert.AreEqual(originalOptions.BundleSize, resultOptions.BundleSize);
            Assert.AreEqual(originalOptions.Crc, resultOptions.Crc);
            Assert.AreEqual(originalOptions.Hash, resultOptions.Hash);
            Assert.AreEqual(originalOptions.Timeout, resultOptions.Timeout);
            Assert.AreEqual(originalOptions.RetryCount, resultOptions.RetryCount);
        }

        [Test]
        public void Serialize_WithEmptyArrays_Succeeds()
        {
            // Arrange
            var original = new AddressablesContentState
            {
                playerVersion = "1.0",
                editorVersion = "2023.1",
                remoteCatalogLoadPath = "",
                cachedInfos = new CachedAssetState[0],
                cachedBundles = new CachedBundleState[0]
            };
            var path = Path.Combine(m_TempDir, "empty_arrays.bin");

            // Act
            ContentStateSerializer.Serialize(original, path);
            var result = ContentStateSerializer.Deserialize(path);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.cachedInfos.Length);
            Assert.AreEqual(0, result.cachedBundles.Length);
        }

        [Test]
        public void Serialize_WithNullArrays_Succeeds()
        {
            // Arrange
            var original = new AddressablesContentState
            {
                playerVersion = "1.0",
                editorVersion = "2023.1",
                remoteCatalogLoadPath = null,
                cachedInfos = null,
                cachedBundles = null
            };
            var path = Path.Combine(m_TempDir, "null_arrays.bin");

            // Act
            ContentStateSerializer.Serialize(original, path);
            var result = ContentStateSerializer.Deserialize(path);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("1.0", result.playerVersion);
        }

        [Test]
        public void Serialize_CreatesDirectoryIfNotExists()
        {
            // Arrange
            var original = CreateSampleContentState();
            var nestedPath = Path.Combine(m_TempDir, "nested", "dir", "test.bin");

            // Act
            ContentStateSerializer.Serialize(original, nestedPath);

            // Assert
            Assert.IsTrue(File.Exists(nestedPath));
        }

        [Test]
        public void Serialize_OverwritesExistingFile()
        {
            // Arrange
            var path = Path.Combine(m_TempDir, "overwrite.bin");
            var original1 = CreateSampleContentState();
            original1.playerVersion = "version1";
            var original2 = CreateSampleContentState();
            original2.playerVersion = "version2";

            // Act
            ContentStateSerializer.Serialize(original1, path);
            ContentStateSerializer.Serialize(original2, path);
            var result = ContentStateSerializer.Deserialize(path);

            // Assert
            Assert.AreEqual("version2", result.playerVersion);
        }

        [Test]
        public void IsLegacyFormat_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            var path = Path.Combine(m_TempDir, "nonexistent.bin");

            // Act & Assert
            Assert.IsFalse(ContentStateSerializer.IsLegacyFormat(path));
        }

        [Test]
        public void IsLegacyFormat_WithNewFormatFile_ReturnsFalse()
        {
            // Arrange
            var original = CreateSampleContentState();
            var path = Path.Combine(m_TempDir, "new_format.bin");
            ContentStateSerializer.Serialize(original, path);

            // Act & Assert
            Assert.IsFalse(ContentStateSerializer.IsLegacyFormat(path));
        }

        [Test]
        public void IsLegacyFormat_WithRandomFile_ReturnsTrue()
        {
            // Arrange - create a file without the version marker
            var path = Path.Combine(m_TempDir, "random_file.bin");
            File.WriteAllBytes(path, new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 });

            // Act & Assert
            Assert.IsTrue(ContentStateSerializer.IsLegacyFormat(path));
        }

        [Test]
        public void ExtractToJson_CreatesValidJsonFile()
        {
            // Arrange
            var original = CreateSampleContentState();
            var binaryPath = Path.Combine(m_TempDir, "extract_test.bin");
            var jsonPath = Path.Combine(m_TempDir, "extract_test.json");
            ContentStateSerializer.Serialize(original, binaryPath);

            // Act
            ContentStateSerializer.ExtractToJson(binaryPath, jsonPath);

            // Assert
            Assert.IsTrue(File.Exists(jsonPath), "JSON file should be created");
            var jsonContent = File.ReadAllText(jsonPath);
            Assert.IsTrue(jsonContent.Contains("playerVersion"), "JSON should contain playerVersion");
            Assert.IsTrue(jsonContent.Contains(original.playerVersion), "JSON should contain the actual player version value");
        }

        [Test]
        public void SerializeDeserialize_WithMultipleCachedAssetStates_PreservesAll()
        {
            // Arrange
            var original = new AddressablesContentState
            {
                playerVersion = "1.0",
                editorVersion = "2023.1",
                remoteCatalogLoadPath = "https://example.com",
                cachedInfos = new[]
                {
                    new CachedAssetState
                    {
                        groupGuid = "group1",
                        bundleFileId = "bundle1",
                        asset = new AssetState(),
                        dependencies = new AssetState[0]
                    },
                    new CachedAssetState
                    {
                        groupGuid = "group2",
                        bundleFileId = "bundle2",
                        asset = new AssetState(),
                        dependencies = new AssetState[0]
                    },
                    new CachedAssetState
                    {
                        groupGuid = "group3",
                        bundleFileId = "bundle3",
                        asset = new AssetState(),
                        dependencies = new AssetState[0]
                    }
                },
                cachedBundles = new CachedBundleState[0]
            };
            var path = Path.Combine(m_TempDir, "multiple_cached.bin");

            // Act
            ContentStateSerializer.Serialize(original, path);
            var result = ContentStateSerializer.Deserialize(path);

            // Assert
            Assert.AreEqual(3, result.cachedInfos.Length);
            Assert.AreEqual("group1", result.cachedInfos[0].groupGuid);
            Assert.AreEqual("group2", result.cachedInfos[1].groupGuid);
            Assert.AreEqual("group3", result.cachedInfos[2].groupGuid);
        }

        private AddressablesContentState CreateSampleContentState()
        {
            var bundleOptions = new AssetBundleRequestOptions
            {
                Hash = "abcd1234efgh5678ijkl9012mnop3456",
                Crc = 12345,
                Timeout = 30,
                ChunkedTransfer = true,
                RedirectLimit = 10,
                RetryCount = 3,
                BundleName = "test_bundle",
                AssetLoadMode = AssetLoadMode.RequestedAssetAndDependencies,
                BundleSize = 1024000,
                UseCrcForCachedBundle = true,
                UseUnityWebRequestForLocalBundles = false,
                ClearOtherCachedVersionsWhenLoaded = true
            };

            return new AddressablesContentState
            {
                playerVersion = "1.0.0",
                editorVersion = "2023.1.0f1",
                remoteCatalogLoadPath = "https://example.com/catalog.json",
                cachedInfos = new[]
                {
                    new CachedAssetState
                    {
                        asset = new AssetState(),
                        dependencies = new AssetState[0],
                        groupGuid = "test-group-guid",
                        bundleFileId = "test-bundle-file-id",
                        data = bundleOptions
                    }
                },
                cachedBundles = new[]
                {
                    new CachedBundleState
                    {
                        bundleFileId = "test-bundle-file-id",
                        data = bundleOptions
                    }
                }
            };
        }
    }
}
#endif
