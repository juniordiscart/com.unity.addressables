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

        string CreateGuidFile(FileRegistry registry, int seed, long sizeInBytes = 1024)
        {
            string guidName = GenerateGuidName(seed);
            string filePath = Path.Combine(m_TestDir, guidName);
            WriteFile(filePath, sizeInBytes, seed);
            registry.AddFile(filePath);
            return filePath;
        }

        string CreateNonGuidFile(FileRegistry registry, string name, long sizeInBytes = 512)
        {
            string filePath = Path.Combine(m_TestDir, name);
            WriteFile(filePath, sizeInBytes, 0);
            registry.AddFile(filePath);
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
            string guidFile1 = CreateGuidFile(registry, seed: 1, sizeInBytes: 1024);
            string guidFile2 = CreateGuidFile(registry, seed: 2, sizeInBytes: 2048);
            CreateNonGuidFile(registry, "metadata.json");
            CreateNonGuidFile(registry, "BuildManifestHash.txt");

            var result = ContentDirectoryArchiver.FilterFiles(registry, out long totalSize);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(guidFile1, result[0].fileName);
            Assert.AreEqual(guidFile2, result[1].fileName);
            Assert.AreEqual(1024 + 2048, totalSize);
        }

        [Test]
        public void CreateArchiveWorkItems_SmallTotalSize_CreatesSingleBucket()
        {
            var registry = new FileRegistry();
            for (int i = 0; i < 5; i++)
                CreateGuidFile(registry, seed: i, sizeInBytes: 1024);

            var filtered = ContentDirectoryArchiver.FilterFiles(registry, out long totalSize);
            var result = ContentDirectoryArchiver.CreateArchiveWorkItems("", filtered, totalSize, 1024L * 1024 * 1024);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(5, result[0].files.Count);
        }

        [Test]
        public void CreateArchiveWorkItems_LargeTotalSize_CreatesMultipleBuckets()
        {
            var registry = new FileRegistry();
            for (int i = 0; i < 10; i++)
                CreateGuidFile(registry, seed: i, sizeInBytes: 1024);

            var filtered = ContentDirectoryArchiver.FilterFiles(registry, out long totalSize);
            // 10KB total with 2KB avgMax => expect ~5-6 buckets
            var result = ContentDirectoryArchiver.CreateArchiveWorkItems("", filtered, totalSize, 2 * 1024);

            Assert.Greater(result.Count, 1);
        }

        [Test]
        public void CreateArchiveWorkItems_AllFilesDistributed()
        {
            var registry = new FileRegistry();
            for (int i = 0; i < 10; i++)
                CreateGuidFile(registry, seed: i, sizeInBytes: 1024);

            var filtered = ContentDirectoryArchiver.FilterFiles(registry, out long totalSize);
            var result = ContentDirectoryArchiver.CreateArchiveWorkItems("", filtered, totalSize, 3 * 1024);

            int totalFiles = result.Sum(wi => wi.files.Count);
            Assert.AreEqual(10, totalFiles, "All files should be distributed across work items");
        }

        [Test]
        public void CreateArchiveWorkItems_NoEmptyBuckets()
        {
            var registry = new FileRegistry();
            for (int i = 0; i < 10; i++)
                CreateGuidFile(registry, seed: i, sizeInBytes: 1024);

            var filtered = ContentDirectoryArchiver.FilterFiles(registry, out long totalSize);
            var result = ContentDirectoryArchiver.CreateArchiveWorkItems("", filtered, totalSize, 2 * 1024);

            foreach (var wi in result)
                Assert.IsNotEmpty(wi.files, $"Work item {wi.archivePath} should not be empty");
        }

        [Test]
        public void CreateArchiveWorkItems_DeterministicBucketing()
        {
            var registry1 = new FileRegistry();
            var registry2 = new FileRegistry();
            for (int i = 0; i < 10; i++)
            {
                CreateGuidFile(registry1, seed: i, sizeInBytes: 1024);
                CreateGuidFile(registry2, seed: i, sizeInBytes: 1024);
            }

            var filtered1 = ContentDirectoryArchiver.FilterFiles(registry1, out long totalSize1);
            var filtered2 = ContentDirectoryArchiver.FilterFiles(registry2, out long totalSize2);
            var result1 = ContentDirectoryArchiver.CreateArchiveWorkItems("", filtered1, totalSize1, 3 * 1024);
            var result2 = ContentDirectoryArchiver.CreateArchiveWorkItems("", filtered2, totalSize2, 3 * 1024);

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
            string guidFile1 = CreateGuidFile(registry, seed: 1);
            string guidFile2 = CreateGuidFile(registry, seed: 2);

            var removed = ContentDirectoryArchiver.FilterFiles(registry, out _);
            ContentDirectoryArchiver.CleanupRegistry(registry, removed, new List<(string archivePath, List<ResourceFile> files)>());

            Assert.IsFalse(File.Exists(guidFile1));
            Assert.IsFalse(File.Exists(guidFile2));
            Assert.IsEmpty(registry.GetFilePaths());
        }

        [Test]
        public void CleanupRegistry_AddsArchivePathsToRegistry()
        {
            var registry = new FileRegistry();
            var added = new List<(string archivePath, List<ResourceFile> files)> { ("path/to/content0.archive", null), ("path/to/content1.archive", null) };

            ContentDirectoryArchiver.CleanupRegistry(registry, new List<ResourceFile>(), added);

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

            ContentDirectoryArchiver.CleanupRegistry(registry, removed, new List<(string archivePath, List<ResourceFile> files)> { ("content0.archive", null) });

            var paths = registry.GetFilePaths().ToList();
            Assert.That(paths, Contains.Item(metaFile));
            Assert.That(paths, Contains.Item("content0.archive"));
            Assert.That(paths, Does.Not.Contain(guidFile));
        }

        [Test]
        public void ArchiveAndUpdateRegistry_EmptyRegistry_DoesNothing()
        {
            var registry = new FileRegistry();

            ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 4096L * 1024 * 1024, registry);

            Assert.IsEmpty(registry.GetFilePaths());
            Assert.IsEmpty(Directory.GetFiles(m_TestDir));
        }

        [Test]
        public void ArchiveAndUpdateRegistry_NonGuidFilesOnly_DoesNothing()
        {
            var registry = new FileRegistry();
            string metaFile = CreateNonGuidFile(registry, "BuildManifestHash.txt");
            string jsonFile = CreateNonGuidFile(registry, "somehash.json");

            ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 4096L * 1024 * 1024, registry);

            var remaining = registry.GetFilePaths().ToList();
            Assert.AreEqual(2, remaining.Count);
            Assert.That(remaining, Contains.Item(metaFile));
            Assert.That(remaining, Contains.Item(jsonFile));
        }

        [Test]
        public void ArchiveAndUpdateRegistry_CreatesArchivesAndCleansUp()
        {
            var registry = new FileRegistry();
            var guidFiles = new List<string>();
            for (int i = 0; i < 5; i++)
                guidFiles.Add(CreateGuidFile(registry, seed: i));
            string metaFile = CreateNonGuidFile(registry, "BuildManifestHash.txt");

            ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 4096L * 1024 * 1024, registry);

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
            for (int i = 0; i < 3; i++)
                CreateGuidFile(registry, seed: i);

            ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 4096L * 1024 * 1024, registry);

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
            for (int i = 0; i < 10; i++)
                CreateGuidFile(registry, seed: i, sizeInBytes: 1024);

            ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 2 * 1024, registry);

            var archives = registry.GetFilePaths().ToList();
            Assert.Greater(archives.Count, 1);
        }

        [Test]
        public void ArchiveAndUpdateRegistry_LargeAvgMaxSize_CreatesSingleArchive()
        {
            var registry = new FileRegistry();
            for (int i = 0; i < 5; i++)
                CreateGuidFile(registry, seed: i, sizeInBytes: 1024);

            ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 1024L * 1024 * 1024, registry);

            Assert.AreEqual(1, registry.GetFilePaths().Count());
        }

        [Test]
        public void ArchiveAndUpdateRegistry_Deterministic()
        {
            int archiveCount1;
            int archiveCount2;

            {
                var registry = new FileRegistry();
                for (int i = 0; i < 10; i++)
                    CreateGuidFile(registry, seed: i, sizeInBytes: 1024);
                ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 3 * 1024, registry);
                archiveCount1 = registry.GetFilePaths().Count();
            }

            Directory.Delete(m_TestDir, true);
            Directory.CreateDirectory(m_TestDir);

            {
                var registry = new FileRegistry();
                for (int i = 0; i < 10; i++)
                    CreateGuidFile(registry, seed: i, sizeInBytes: 1024);
                ContentDirectoryArchiver.ArchiveAndUpdateRegistry(m_TestDir, 3 * 1024, registry);
                archiveCount2 = registry.GetFilePaths().Count();
            }

            Assert.AreEqual(archiveCount1, archiveCount2);
        }
    }
}
#endif
