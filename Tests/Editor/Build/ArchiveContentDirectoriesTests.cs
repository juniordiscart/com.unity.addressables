#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor; // explicitly imported for GUID backwards compatibility
using UnityEngine; // explicitly imported for GUID backwards compatibility
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.AddressableAssets.Tests
{
    public class ArchiveContentDirectoriesTests
    {
        string m_TestDir;

        [SetUp]
        public void SetUp()
        {
            m_TestDir = Path.Combine("Temp", "ArchiveContentDirectoriesTests");
            if (Directory.Exists(m_TestDir))
                Directory.Delete(m_TestDir, true);
            Directory.CreateDirectory(m_TestDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_TestDir))
                Directory.Delete(m_TestDir, true);
        }

        string CreateGuidFile(FileRegistry registry, int seed, long sizeInBytes = 1024, List<string> filePaths = null)
        {
            string guidName = GenerateGuidName(seed);
            string filePath = Path.Combine(m_TestDir, guidName);
            WriteFile(filePath, sizeInBytes, seed);
            registry.AddFile(filePath);
            filePaths?.Add(filePath);
            return filePath;
        }

        string CreateNonGuidFile(FileRegistry registry, string name, long sizeInBytes = 512, List<string> filePaths = null)
        {
            string filePath = Path.Combine(m_TestDir, name);
            WriteFile(filePath, sizeInBytes, 0);
            registry.AddFile(filePath);
            filePaths?.Add(filePath);
            return filePath;
        }

        static string GenerateGuidName(int seed)
        {
            var rng = new System.Random(seed);
            var bytes = new byte[16];
            rng.NextBytes(bytes);
            return new GUID(BitConverter.ToString(bytes).Replace("-", "").ToLower()).ToString();
        }

        static void WriteFile(string path, long size, int seed)
        {
            var rng = new System.Random(seed);
            var bytes = new byte[size];
            rng.NextBytes(bytes);
            File.WriteAllBytes(path, bytes);
        }

        [Test]
        public void FilterFiles_MixedFiles_ReturnsExpectedFilesAndSize()
        {
            var registry = new FileRegistry();
            var filePaths = new List<string>();
            string guidFile1 = CreateGuidFile(registry, seed: 1, sizeInBytes: 1024, filePaths: filePaths);
            string guidFile2 = CreateGuidFile(registry, seed: 2, sizeInBytes: 2048, filePaths: filePaths);
            CreateNonGuidFile(registry, "metadata.json", filePaths: filePaths);
            CreateNonGuidFile(registry, "BuildManifestHash.txt", filePaths: filePaths);

            var result = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.FilterFiles(filePaths, out _, out long totalSize);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(guidFile1, result[0].fileName);
            Assert.AreEqual(guidFile2, result[1].fileName);
            Assert.AreEqual(1024 + 2048, totalSize);
        }

        [Test]
        public void CreateArchiveWorkItems_SmallTotalSize_CreatesSingleBucket()
        {
            var registry = new FileRegistry();
            var filePaths = new List<string>();
            for (int i = 0; i < 5; i++)
                CreateGuidFile(registry, seed: i, sizeInBytes: 1024, filePaths: filePaths);

            var filtered = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.FilterFiles(filePaths, out var sizes, out long totalSize);
            var result = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.CreateArchiveWorkItems("", filtered, sizes, totalSize, 1024L * 1024 * 1024);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(5, result[0].files.Count);
        }

        [Test]
        public void CreateArchiveWorkItems_LargeTotalSize_CreatesMultipleBuckets()
        {
            var registry = new FileRegistry();
            var filePaths = new List<string>();
            for (int i = 0; i < 10; i++)
                CreateGuidFile(registry, seed: i, sizeInBytes: 1024, filePaths: filePaths);

            var filtered = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.FilterFiles(filePaths, out var sizes, out long totalSize);
            // 10KB total with 2KB avgMax => expect ~5-6 buckets
            var result = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.CreateArchiveWorkItems("", filtered, sizes, totalSize, 2 * 1024);

            Assert.Greater(result.Count, 1);
        }

        [Test]
        public void CreateArchiveWorkItems_AllFilesDistributed()
        {
            var registry = new FileRegistry();
            var filePaths = new List<string>();
            for (int i = 0; i < 10; i++)
                CreateGuidFile(registry, seed: i, sizeInBytes: 1024, filePaths: filePaths);

            var filtered = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.FilterFiles(filePaths, out var sizes, out long totalSize);
            var result = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.CreateArchiveWorkItems("", filtered, sizes, totalSize, 3 * 1024);

            int totalFiles = result.Sum(wi => wi.files.Count);
            Assert.AreEqual(10, totalFiles, "All files should be distributed across work items");
        }

        [Test]
        public void CreateArchiveWorkItems_NoEmptyBuckets()
        {
            var registry = new FileRegistry();
            var filePaths = new List<string>();
            for (int i = 0; i < 10; i++)
                CreateGuidFile(registry, seed: i, sizeInBytes: 1024, filePaths: filePaths);

            var filtered = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.FilterFiles(filePaths, out var sizes, out long totalSize);
            var result = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.CreateArchiveWorkItems("", filtered, sizes, totalSize, 2 * 1024);

            foreach (var wi in result)
                Assert.IsNotEmpty(wi.files, $"Work item {wi.archivePath} should not be empty");
        }

        [Test]
        public void CreateArchiveWorkItems_DeterministicBucketing()
        {
            var registry1 = new FileRegistry();
            var registry2 = new FileRegistry();
            var filePaths1 = new List<string>();
            var filePaths2 = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                CreateGuidFile(registry1, seed: i, sizeInBytes: 1024, filePaths: filePaths1);
                CreateGuidFile(registry2, seed: i, sizeInBytes: 1024, filePaths: filePaths2);
            }

            var filtered1 = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.FilterFiles(filePaths1, out var sizes1, out long totalSize1);
            var filtered2 = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.FilterFiles(filePaths2, out var sizes2, out long totalSize2);
            var result1 = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.CreateArchiveWorkItems("", filtered1, sizes1, totalSize1, 3 * 1024);
            var result2 = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.CreateArchiveWorkItems("", filtered2, sizes2, totalSize2, 3 * 1024);

            Assert.AreEqual(result1.Count, result2.Count, "Same input should produce same number of work items");
            for (int i = 0; i < result1.Count; i++)
            {
                Assert.AreEqual(result1[i].archivePath, result2[i].archivePath);
                Assert.AreEqual(result1[i].files.Count, result2[i].files.Count);
            }
        }

        [Test]
        public void CleanupRegistry_RemovesFilesFromDiskAndRegistry()
        {
            var registry = new FileRegistry();
            var filePaths = new List<string>();
            string guidFile1 = CreateGuidFile(registry, seed: 1, filePaths: filePaths);
            string guidFile2 = CreateGuidFile(registry, seed: 2, filePaths: filePaths);

            var removed = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.FilterFiles(filePaths, out _, out _);
            ContentDirectorySchemaBuilder.ContentDirectoryArchiver.CleanupRegistry(registry, removed, new List<(string archivePath, long uncompressedSize, List<ResourceFile> files)>());

            Assert.IsFalse(File.Exists(guidFile1));
            Assert.IsFalse(File.Exists(guidFile2));
            Assert.IsEmpty(registry.GetFilePaths());
        }

        [Test]
        public void CleanupRegistry_AddsArchivePathsToRegistry()
        {
            var registry = new FileRegistry();
            var added = new List<(string archivePath, long uncompressedSize, List<ResourceFile> files)> { ("path/to/content0.archive", 0L, null), ("path/to/content1.archive", 0L, null) };

            ContentDirectorySchemaBuilder.ContentDirectoryArchiver.CleanupRegistry(registry, new List<ResourceFile>(), added);

            var paths = registry.GetFilePaths().ToList();
            Assert.AreEqual(2, paths.Count);
            Assert.That(paths, Contains.Item("path/to/content0.archive"));
            Assert.That(paths, Contains.Item("path/to/content1.archive"));
        }

        [Test]
        public void CleanupRegistry_PreservesUnrelatedFiles()
        {
            var registry = new FileRegistry();
            string metaFile = CreateNonGuidFile(registry, "BuildManifestHash.txt");
            string guidFile = CreateGuidFile(registry, seed: 1);

            var removed = new List<ResourceFile>();
            var rf = new ResourceFile();
            rf.fileName = guidFile;
            removed.Add(rf);

            ContentDirectorySchemaBuilder.ContentDirectoryArchiver.CleanupRegistry(registry, removed, new List<(string archivePath, long uncompressedSize, List<ResourceFile> files)> { ("content0.archive", 0L, null) });

            var paths = registry.GetFilePaths().ToList();
            Assert.That(paths, Contains.Item(metaFile));
            Assert.That(paths, Contains.Item("content0.archive"));
            Assert.That(paths, Does.Not.Contain(guidFile));
        }

        [Test]
        public void ArchiveAndUpdateRegistry_EmptyRegistry_DoesNothing()
        {
            var registry = new FileRegistry();

            ContentDirectorySchemaBuilder.ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 4096L * 1024 * 1024, new List<string>(), registry);

            Assert.IsEmpty(registry.GetFilePaths());
            Assert.IsEmpty(Directory.GetFiles(m_TestDir));
        }

        [Test]
        public void ArchiveAndUpdateRegistry_NonGuidFilesOnly_DoesNothing()
        {
            var registry = new FileRegistry();
            var filePaths = new List<string>();
            string metaFile = CreateNonGuidFile(registry, "BuildManifestHash.txt", filePaths: filePaths);
            string jsonFile = CreateNonGuidFile(registry, "somehash.json", filePaths: filePaths);

            ContentDirectorySchemaBuilder.ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 4096L * 1024 * 1024, filePaths, registry);

            var remaining = registry.GetFilePaths().ToList();
            Assert.AreEqual(2, remaining.Count);
            Assert.That(remaining, Contains.Item(metaFile));
            Assert.That(remaining, Contains.Item(jsonFile));
        }

        [Test]
        public void ArchiveAndUpdateRegistry_CreatesArchivesAndCleansUp()
        {
            var registry = new FileRegistry();
            var filePaths = new List<string>();
            var guidFiles = new List<string>();
            for (int i = 0; i < 5; i++)
                guidFiles.Add(CreateGuidFile(registry, seed: i, filePaths: filePaths));
            string metaFile = CreateNonGuidFile(registry, "BuildManifestHash.txt");

            ContentDirectorySchemaBuilder.ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 4096L * 1024 * 1024, filePaths, registry);

            // Originals gone
            foreach (var f in guidFiles)
                Assert.IsFalse(File.Exists(f));

            // Archives created and registered
            var remaining = registry.GetFilePaths().ToList();
            Assert.That(remaining, Contains.Item(metaFile));
            var archives = remaining.Where(p => p.EndsWith(".archive")).ToList();
            Assert.IsNotEmpty(archives);
            foreach (var a in archives)
                Assert.IsTrue(File.Exists(a));
        }

        [Test]
        public void ArchiveAndUpdateRegistry_ArchiveNamesFollowConvention()
        {
            var registry = new FileRegistry();
            var filePaths = new List<string>();
            for (int i = 0; i < 3; i++)
                CreateGuidFile(registry, seed: i, filePaths: filePaths);

            ContentDirectorySchemaBuilder.ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 4096L * 1024 * 1024, filePaths, registry);

            foreach (var path in registry.GetFilePaths())
            {
                var fileName = Path.GetFileName(path);
                Assert.That(fileName, Does.Match(@"^content\d+\.archive$"),
                    $"Archive name should match content<N>.archive pattern: {fileName}");
            }
        }

        [Test]
        public void ArchiveAndUpdateRegistry_SmallAvgMaxSize_CreatesMultipleArchives()
        {
            var registry = new FileRegistry();
            var filePaths = new List<string>();
            for (int i = 0; i < 10; i++)
                CreateGuidFile(registry, seed: i, sizeInBytes: 1024, filePaths: filePaths);

            ContentDirectorySchemaBuilder.ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 2 * 1024, filePaths, registry);

            var archives = registry.GetFilePaths().ToList();
            Assert.Greater(archives.Count, 1);
        }

        [Test]
        public void ArchiveAndUpdateRegistry_LargeAvgMaxSize_CreatesSingleArchive()
        {
            var registry = new FileRegistry();
            var filePaths = new List<string>();
            for (int i = 0; i < 5; i++)
                CreateGuidFile(registry, seed: i, sizeInBytes: 1024, filePaths: filePaths);

            ContentDirectorySchemaBuilder.ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 1024L * 1024 * 1024, filePaths, registry);

            Assert.AreEqual(1, registry.GetFilePaths().Count());
        }

        [Test]
        public void ArchiveAndUpdateRegistry_Deterministic()
        {
            int archiveCount1;
            int archiveCount2;

            {
                var registry = new FileRegistry();
                var filePaths = new List<string>();
                for (int i = 0; i < 10; i++)
                    CreateGuidFile(registry, seed: i, sizeInBytes: 1024, filePaths: filePaths);
                ContentDirectorySchemaBuilder.ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 3 * 1024, filePaths, registry);
                archiveCount1 = registry.GetFilePaths().Count();
            }

            Directory.Delete(m_TestDir, true);
            Directory.CreateDirectory(m_TestDir);

            {
                var registry = new FileRegistry();
                var filePaths = new List<string>();
                for (int i = 0; i < 10; i++)
                    CreateGuidFile(registry, seed: i, sizeInBytes: 1024, filePaths: filePaths);
                ContentDirectorySchemaBuilder.ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 3 * 1024, filePaths, registry);
                archiveCount2 = registry.GetFilePaths().Count();
            }

            Assert.AreEqual(archiveCount1, archiveCount2);
        }

        [Test]
        public void CreateArchiveWorkItems_BucketUncompressedSizeMatchesFiles()
        {
            var registry = new FileRegistry();
            var filePaths = new List<string>();
            // Use distinct sizes so mismatches are detectable
            var expectedSizes = new long[] { 512, 1024, 2048, 4096, 8192 };
            for (int i = 0; i < expectedSizes.Length; i++)
                CreateGuidFile(registry, seed: i, sizeInBytes: expectedSizes[i], filePaths: filePaths);

            var filtered = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.FilterFiles(filePaths, out var sizes, out long totalSize);
            // Small target forces multiple buckets where possible
            var workItems = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.CreateArchiveWorkItems("", filtered, sizes, totalSize, 1024);

            // Per-bucket sum must equal the sum of its constituent file sizes
            foreach (var wi in workItems)
            {
                long expectedBucketSize = wi.files.Sum(rf =>
                {
                    int idx = filtered.IndexOf(rf);
                    return sizes[idx];
                });
                Assert.AreEqual(expectedBucketSize, wi.uncompressedSize,
                    $"Work item {wi.archivePath} uncompressedSize should equal sum of its files' sizes");
            }

            // Grand total must be preserved
            long grandTotal = workItems.Sum(wi => wi.uncompressedSize);
            Assert.AreEqual(totalSize, grandTotal, "Sum of per-bucket uncompressedSize should equal total");
        }

        [Test]
        public void ContentLayout_Load_ParsesArtifactsCorrectly()
        {
            string metadataPath = Path.Combine(m_TestDir, "metadata");
            Directory.CreateDirectory(metadataPath);
            string json = @"{""BuildManifestHash"":""abc123"",""BinaryArtifacts"":[{""ContentHash"":""hash1"",""Category"":""contentfile"",""Size"":1024},{""ContentHash"":""hash2"",""Category"":""resource"",""Size"":2048},{""ContentHash"":""hash3"",""Category"":""contentfile"",""Size"":512}]}";
            File.WriteAllText(Path.Combine(metadataPath, "ContentLayout.json"), json);

            var layout = ContentDirectorySchemaBuilder.ContentLayout.Load(metadataPath);

            Assert.AreEqual("abc123", layout.BuildManifestHash);
            Assert.AreEqual(3, layout.BinaryArtifacts.Count);
            Assert.AreEqual("hash1", layout.BinaryArtifacts[0].ContentHash);
            Assert.AreEqual("contentfile", layout.BinaryArtifacts[0].Category);
            Assert.AreEqual(1024UL, layout.BinaryArtifacts[0].Size);
        }

        [Test]
        public void ContentLayout_Load_HandlesEmptyArtifacts()
        {
            string metadataPath = Path.Combine(m_TestDir, "metadata");
            Directory.CreateDirectory(metadataPath);
            string json = @"{""BuildManifestHash"":""empty"",""BinaryArtifacts"":[]}";
            File.WriteAllText(Path.Combine(metadataPath, "ContentLayout.json"), json);

            var layout = ContentDirectorySchemaBuilder.ContentLayout.Load(metadataPath);

            Assert.AreEqual("empty", layout.BuildManifestHash);
            Assert.IsEmpty(layout.BinaryArtifacts);
        }

        [Test]
        public void CreateArchiveWorkItems_WithUdsResourceFiles_BucketsCorrectly()
        {
            var resourceFiles = new List<ResourceFile>();
            var sizes = new List<long>();
            for (int i = 0; i < 5; i++)
            {
                string hash = GenerateGuidName(i);
                resourceFiles.Add(new ResourceFile
                {
                    fileName = "uds:/" + hash,
                    fileAlias = hash,
                    serializedFile = true
                });
                sizes.Add(1024L);
            }

            var workItems = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.CreateArchiveWorkItems(m_TestDir, resourceFiles, sizes, 5 * 1024, 1024L * 1024 * 1024);

            Assert.AreEqual(1, workItems.Count, "Small total size should create single bucket");
            Assert.AreEqual(5, workItems[0].files.Count, "All files should be in the bucket");
        }

        [Test]
        public void ContentLayout_Load_EmptyBuildManifestHash_ThrowsInvalidOperation()
        {
            string metadataPath = Path.Combine(m_TestDir, "metadata");
            Directory.CreateDirectory(metadataPath);
            string json = @"{""BuildManifestHash"":"""",""BinaryArtifacts"":[{""ContentHash"":""h1"",""Category"":""contentfile"",""Size"":100}]}";
            File.WriteAllText(Path.Combine(metadataPath, "ContentLayout.json"), json);

            var ex = Assert.Throws<InvalidOperationException>(() => ContentDirectorySchemaBuilder.ContentLayout.Load(metadataPath));
            Assert.That(ex.Message, Does.Contain("BuildManifestHash is missing or empty"));
        }

        [Test]
        public void ContentLayout_Load_NullBuildManifestHash_ThrowsInvalidOperation()
        {
            string metadataPath = Path.Combine(m_TestDir, "metadata");
            Directory.CreateDirectory(metadataPath);
            string json = @"{""BinaryArtifacts"":[{""ContentHash"":""h1"",""Category"":""contentfile"",""Size"":100}]}";
            File.WriteAllText(Path.Combine(metadataPath, "ContentLayout.json"), json);

            var ex = Assert.Throws<InvalidOperationException>(() => ContentDirectorySchemaBuilder.ContentLayout.Load(metadataPath));
            Assert.That(ex.Message, Does.Contain("BuildManifestHash is missing or empty"));
        }

        [Test]
        public void ArchiveFromUDS_WritesBuildManifestHashFile()
        {
            string metadataPath = Path.Combine(m_TestDir, "metadata");
            Directory.CreateDirectory(metadataPath);
            string json = @"{""BuildManifestHash"":""expected_hash_value_12345"",""BinaryArtifacts"":[]}";
            File.WriteAllText(Path.Combine(metadataPath, "ContentLayout.json"), json);

            var layout = ContentDirectorySchemaBuilder.ContentLayout.Load(metadataPath);
            var createdFiles = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.ArchiveFromUDS(layout, m_TestDir, 4096L * 1024 * 1024);

            string hashFilePath = Path.Combine(m_TestDir, ContentDirectorySchemaBuilder.ContentDirectoryArchiver.kBuildManifestHashFileName);
            Assert.IsTrue(File.Exists(hashFilePath), "BuildManifestHash.txt should be created");
            Assert.AreEqual("expected_hash_value_12345", File.ReadAllText(hashFilePath),
                "BuildManifestHash.txt content should match layout.BuildManifestHash");
            Assert.That(createdFiles, Contains.Item(hashFilePath));
        }

        [Test]
        public void ArchiveFromUDS_EmptyArtifacts_OnlyCreatesHashFile()
        {
            string metadataPath = Path.Combine(m_TestDir, "metadata");
            Directory.CreateDirectory(metadataPath);
            string json = @"{""BuildManifestHash"":""hash123"",""BinaryArtifacts"":[]}";
            File.WriteAllText(Path.Combine(metadataPath, "ContentLayout.json"), json);

            var layout = ContentDirectorySchemaBuilder.ContentLayout.Load(metadataPath);
            var createdFiles = ContentDirectorySchemaBuilder.ContentDirectoryArchiver.ArchiveFromUDS(layout, m_TestDir, 4096L * 1024 * 1024);

            Assert.AreEqual(1, createdFiles.Length, "Only BuildManifestHash.txt should be created");
            Assert.That(createdFiles[0], Does.EndWith(ContentDirectorySchemaBuilder.ContentDirectoryArchiver.kBuildManifestHashFileName));
        }

        [Test]
        public void ArchiveAndUpdateRegistry_NamedArgsAppearOnStepNotAsTopLevelKeys()
        {
            var registry = new FileRegistry();
            var filePaths = new List<string>();
            for (int i = 0; i < 3; i++)
                CreateGuidFile(registry, seed: i, filePaths: filePaths);

            var log = new BuildLog();
            ContentDirectorySchemaBuilder.ContentDirectoryArchiver.ArchiveAndUpdateRegistry(
                m_TestDir, 4096L * 1024 * 1024, filePaths, registry, log);

            string tep = log.FormatForTraceEventProfiler();
            int traceEventsPos = tep.IndexOf("traceEvents", System.StringComparison.Ordinal);
            Assert.Greater(traceEventsPos, -1, "TEP output should contain traceEvents");

            // "Compression" must appear inside the traceEvents array (as a named arg on a step),
            // not only as a bare top-level key before traceEvents.
            int compressionInEvents = tep.IndexOf("Compression", traceEventsPos, System.StringComparison.Ordinal);
            Assert.Greater(compressionInEvents, -1,
                "Compression arg should appear inside the traceEvents array, not only at the top-level");

            // "File count" likewise must appear inside traceEvents
            int fileCountInEvents = tep.IndexOf("File count", traceEventsPos, System.StringComparison.Ordinal);
            Assert.Greater(fileCountInEvents, -1,
                "File count arg should appear inside the traceEvents array");
        }
    }
}
#endif
