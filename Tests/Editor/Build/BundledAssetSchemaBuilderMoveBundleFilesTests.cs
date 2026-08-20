using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.AddressableAssets.Tests
{
    /// <summary>
    /// Tests for BundledAssetSchemaBuilder.MoveBundleFiles, the parallel bundle
    /// file-move helper extracted from PostProcessBundles.
    /// </summary>
    [TestFixture]
    public class BundledAssetSchemaBuilderMoveBundleFilesTests
    {
        string m_TestDir;

        [SetUp]
        public void SetUp()
        {
            m_TestDir = Path.Combine("Temp", $"BundledAssetSchemaBuilderTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(m_TestDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_TestDir))
                Directory.Delete(m_TestDir, true);
        }

        /// <summary>
        /// Minimal IBuildLogger that discards all log output; keeps tests hermetic.
        /// </summary>
        class NullBuildLogger : IBuildLogger
        {
            public void AddEntry(LogLevel level, string msg) { }
            public void BeginBuildStep(LogLevel level, string stepName, bool subStepsCanBeThreaded) { }
            public void EndBuildStep() { }
        }

        // ── happy-path ─────────────────────────────────────────────────────────

        [Test]
        public void MoveBundleFiles_MovesAllFilesToDestinations()
        {
            // Arrange
            string srcDir = Path.Combine(m_TestDir, "src");
            string dstDir = Path.Combine(m_TestDir, "dst");
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(dstDir);

            const int fileCount = 20;
            var work = new List<(string src, string dst)>(fileCount);
            for (int i = 0; i < fileCount; i++)
            {
                string src = Path.Combine(srcDir, $"bundle_{i}.bundle");
                string dst = Path.Combine(dstDir, $"bundle_{i}.bundle");
                File.WriteAllText(src, $"bundle content {i}");
                work.Add((src, dst));
            }

            // Act
            BundledAssetSchemaBuilder.MoveBundleFiles(work, new NullBuildLogger());

            // Assert
            for (int i = 0; i < fileCount; i++)
            {
                Assert.IsFalse(File.Exists(work[i].src),
                    $"Source file should be removed after move: {work[i].src}");
                Assert.IsTrue(File.Exists(work[i].dst),
                    $"Destination file should exist after move: {work[i].dst}");
                Assert.AreEqual($"bundle content {i}", File.ReadAllText(work[i].dst),
                    $"File content should be preserved for bundle_{i}");
            }
        }

        // ── edge cases ────────────────────────────────────────────────────────

        [Test]
        public void MoveBundleFiles_EmptyWorkList_DoesNothing()
        {
            // Arrange
            var work = new List<(string src, string dst)>();

            // Act & Assert — must not throw
            Assert.DoesNotThrow(() =>
                BundledAssetSchemaBuilder.MoveBundleFiles(work, new NullBuildLogger()));
        }

        [Test]
        public void MoveBundleFiles_SkipsMove_WhenSourceEqualsDestination()
        {
            // Arrange: src == dst, MoveFileToDestinationWithTimestampIfDifferent returns early.
            string path = Path.Combine(m_TestDir, "same.bundle");
            File.WriteAllText(path, "content");
            var work = new List<(string src, string dst)> { (path, path) };

            // Act
            BundledAssetSchemaBuilder.MoveBundleFiles(work, new NullBuildLogger());

            // Assert: file is untouched
            Assert.IsTrue(File.Exists(path), "File should still exist when src == dst");
            Assert.AreEqual("content", File.ReadAllText(path), "File content should be unchanged");
        }

        [Test]
        public void MoveBundleFiles_CreatesDestinationDirectory_WhenMissing()
        {
            // Arrange: destination lives in a subdirectory that does not yet exist.
            string srcDir = Path.Combine(m_TestDir, "src");
            Directory.CreateDirectory(srcDir);
            string src = Path.Combine(srcDir, "bundle.bundle");
            string dst = Path.Combine(m_TestDir, "dst", "nested", "bundle.bundle");
            File.WriteAllText(src, "content");
            var work = new List<(string src, string dst)> { (src, dst) };

            // Act
            BundledAssetSchemaBuilder.MoveBundleFiles(work, new NullBuildLogger());

            // Assert
            Assert.IsTrue(File.Exists(dst),
                "MoveBundleFiles should create intermediate directories as needed");
        }

        [Test]
        public void MoveBundleFiles_SkipsMove_WhenDestinationTimestampMatchesSource()
        {
            // Arrange: both files exist and have the same last-write timestamp,
            // so MoveFileToDestinationWithTimestampIfDifferent leaves both intact.
            string srcDir = Path.Combine(m_TestDir, "src");
            string dstDir = Path.Combine(m_TestDir, "dst");
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(dstDir);

            string src = Path.Combine(srcDir, "bundle.bundle");
            string dst = Path.Combine(dstDir, "bundle.bundle");
            File.WriteAllText(src, "content");
            File.WriteAllText(dst, "existing content");

            // Stamp both files with the same time so the method's early-out triggers.
            DateTime stamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(src, stamp);
            File.SetLastWriteTimeUtc(dst, stamp);

            var work = new List<(string src, string dst)> { (src, dst) };

            // Act
            BundledAssetSchemaBuilder.MoveBundleFiles(work, new NullBuildLogger());

            // Assert: both files still present, destination unchanged
            Assert.IsTrue(File.Exists(src), "Source should not be removed when timestamps match");
            Assert.IsTrue(File.Exists(dst), "Destination should still exist");
            Assert.AreEqual("existing content", File.ReadAllText(dst),
                "Destination content should not be overwritten when timestamps match");
        }

        [Test]
        public void MoveBundleFiles_OverwritesDestination_WhenTimestampDiffers()
        {
            // Arrange: dest exists with stale content and an older timestamp.
            string srcDir = Path.Combine(m_TestDir, "src");
            string dstDir = Path.Combine(m_TestDir, "dst");
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(dstDir);

            string src = Path.Combine(srcDir, "bundle.bundle");
            string dst = Path.Combine(dstDir, "bundle.bundle");
            File.WriteAllText(src, "new content");
            File.WriteAllText(dst, "stale content");

            // Give dst a clearly older timestamp so the early-out does NOT trigger.
            File.SetLastWriteTimeUtc(dst, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            // src gets a recent timestamp (default = now, definitely different).

            var work = new List<(string src, string dst)> { (src, dst) };

            // Act
            BundledAssetSchemaBuilder.MoveBundleFiles(work, new NullBuildLogger());

            // Assert: source gone, destination holds new content
            Assert.IsFalse(File.Exists(src),
                "Source should be removed after successful move");
            Assert.IsTrue(File.Exists(dst),
                "Destination should exist after move");
            Assert.AreEqual("new content", File.ReadAllText(dst),
                "Destination should contain the moved (new) content, not the stale content");
        }

        [Test]
        public void MoveBundleFiles_ParallelBatch_MixedCases_AllResolveCorrectly()
        {
            // Exercises the Parallel.ForEach path with four distinct scenarios
            // in one batch: normal move, src==dst no-op, missing nested dir,
            // and timestamp-match skip.

            string srcDir = Path.Combine(m_TestDir, "src");
            string dstDir = Path.Combine(m_TestDir, "dst");
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(dstDir);

            // Case A — normal move
            string srcA = Path.Combine(srcDir, "a.bundle");
            string dstA = Path.Combine(dstDir, "a.bundle");
            File.WriteAllText(srcA, "content-A");

            // Case B — src == dst (no-op)
            string pathB = Path.Combine(m_TestDir, "b.bundle");
            File.WriteAllText(pathB, "content-B");

            // Case C — dest directory does not exist yet
            string srcC = Path.Combine(srcDir, "c.bundle");
            string dstC = Path.Combine(m_TestDir, "nested", "sub", "c.bundle");
            File.WriteAllText(srcC, "content-C");

            // Case D — timestamps match → skip
            string srcD = Path.Combine(srcDir, "d.bundle");
            string dstD = Path.Combine(dstDir, "d.bundle");
            File.WriteAllText(srcD, "new-D");
            File.WriteAllText(dstD, "old-D");
            DateTime stamp = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(srcD, stamp);
            File.SetLastWriteTimeUtc(dstD, stamp);

            var work = new List<(string src, string dst)>
            {
                (srcA, dstA),
                (pathB, pathB),
                (srcC, dstC),
                (srcD, dstD),
            };

            BundledAssetSchemaBuilder.MoveBundleFiles(work, new NullBuildLogger());

            // A: moved
            Assert.IsFalse(File.Exists(srcA), "A: source should be removed");
            Assert.AreEqual("content-A", File.ReadAllText(dstA), "A: destination content");

            // B: untouched
            Assert.IsTrue(File.Exists(pathB), "B: file should still exist when src == dst");
            Assert.AreEqual("content-B", File.ReadAllText(pathB), "B: content unchanged");

            // C: moved with dir creation
            Assert.IsFalse(File.Exists(srcC), "C: source should be removed");
            Assert.IsTrue(File.Exists(dstC), "C: destination in nested dir should exist");
            Assert.AreEqual("content-C", File.ReadAllText(dstC), "C: content preserved");

            // D: skipped
            Assert.IsTrue(File.Exists(srcD), "D: source should remain when timestamps match");
            Assert.AreEqual("old-D", File.ReadAllText(dstD), "D: destination should not be overwritten");
        }
    }
}
