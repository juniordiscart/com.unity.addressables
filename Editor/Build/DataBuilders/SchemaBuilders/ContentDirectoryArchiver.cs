#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using BuildCompression = UnityEngine.BuildCompression;

namespace UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders
{
    public partial class ContentDirectorySchemaBuilder
    {
        internal class ContentDirectoryFileCategory
        {
            public const string ContentFile = "contentfile";
            public const string Manifest = "manifest";
        }

        /// <summary>
        /// Utility class for loading and accessing ContentLayout.json data from a Content Directory build.
        /// </summary>
        internal class ContentLayout
        {
            const string kContentLayoutFileName = "ContentLayout.json";

            public string BuildManifestHash { get; }
            public IReadOnlyList<BinaryArtifact> BinaryArtifacts { get; }

            public class BinaryArtifact
            {
                public string ContentHash { get; }
                public string Category { get; }
                public ulong Size { get; }

                internal BinaryArtifact(string contentHash, string category, ulong size)
                {
                    ContentHash = contentHash;
                    Category = category;
                    Size = size;
                }
            }

            ContentLayout(string buildManifestHash, List<BinaryArtifact> artifacts)
            {
                BuildManifestHash = buildManifestHash;
                BinaryArtifacts = artifacts;
            }

            /// <summary>
            /// Loads ContentLayout.json from the specified metadata directory.
            /// </summary>
            /// <param name="buildReportDirectory">Path to the build metadata directory containing ContentLayout.json</param>
            /// <returns>A ContentLayout instance with the loaded data</returns>
            /// <exception cref="InvalidOperationException">If the file cannot be parsed or is missing required data</exception>
            public static ContentLayout Load(string buildReportDirectory)
            {
                var layoutPath = Path.Combine(buildReportDirectory, kContentLayoutFileName);
                var dto = JsonUtility.FromJson<ContentLayoutDto>(File.ReadAllText(layoutPath));
                if (dto == null)
                    throw new InvalidOperationException($"Failed to deserialize ContentLayout from {layoutPath}.");
                if (string.IsNullOrEmpty(dto.BuildManifestHash))
                    throw new InvalidOperationException($"BuildManifestHash is missing or empty in {layoutPath}.");

                var artifacts = new List<BinaryArtifact>(dto.BinaryArtifacts?.Length ?? 0);
                if (dto.BinaryArtifacts != null)
                {
                    foreach (var art in dto.BinaryArtifacts)
                        artifacts.Add(new BinaryArtifact(art.ContentHash, art.Category, art.Size));
                }

                return new ContentLayout(dto.BuildManifestHash, artifacts);
            }

            [Serializable]
            class ContentLayoutDto
            {
                public string BuildManifestHash;
                public BinaryArtifactDto[] BinaryArtifacts;
            }

            [Serializable]
            class BinaryArtifactDto
            {
                public string ContentHash;
                public string Category;
                public ulong Size;
            }
        }

        internal static class ContentDirectoryArchiver
        {
            // Inflates a desired post-compression archive size into the raw bucket size needed
            // to land near it. Update both this constant AND the compression call in
            // ProcessArchiveWorkItems together when changing or extending compression support.
            //   LZ4  : ~1.5x (typical Unity content compresses to ~60-75% of raw)
            //   LZMA : ~3.0x (would apply if LZMA is added to ProcessArchiveWorkItems)
            const double k_LZ4InflationFactor = 1.5;

            internal static void ArchiveAndUpdateRegistry(string archiveOutputDirectory, long targetCompressedSize, List<string> filePaths, FileRegistry registry, IBuildLogger log = null)
            {
                var filteredFiles = FilterFiles(filePaths, out var sizes, out var totalSize);
                long targetUncompressedSize = (long)(targetCompressedSize * k_LZ4InflationFactor);
                var workItems = CreateArchiveWorkItems(archiveOutputDirectory, filteredFiles, sizes, totalSize, targetUncompressedSize);
                ProcessArchiveWorkItems(workItems, log);
                CleanupRegistry(registry, filteredFiles, workItems);
            }

            internal const string kBuildManifestHashFileName = "BuildManifestHash.txt";
            const string kUdsScheme = "uds:/";

            /// <summary>
            /// Archives content from UDS based on ContentLayout data and returns the list of created files.
            /// </summary>
            /// <param name="contentLayout">The loaded ContentLayout containing artifact information</param>
            /// <param name="archiveOutputDirectory">Directory where archives will be written</param>
            /// <param name="targetCompressedSize">Target size for each archive in bytes</param>
            /// <returns>Array of file paths that were created (BuildManifestHash.txt and archive files)</returns>
            internal static string[] ArchiveFromUDS(ContentLayout contentLayout, string archiveOutputDirectory, long targetCompressedSize, IBuildLogger log = null)
            {
                var createdFiles = new List<string>();

                var manifestHashFilePath = Path.Combine(archiveOutputDirectory, kBuildManifestHashFileName);
                File.WriteAllText(manifestHashFilePath, contentLayout.BuildManifestHash);
                createdFiles.Add(manifestHashFilePath);

                var (resourceFiles, sizes, totalSize) = CreateResourceFilesFromLayout(contentLayout);

                long targetUncompressedSize = (long)(targetCompressedSize * k_LZ4InflationFactor);
                var workItems = CreateArchiveWorkItems(archiveOutputDirectory, resourceFiles, sizes, totalSize, targetUncompressedSize);
                ProcessArchiveWorkItems(workItems, log);

                foreach (var item in workItems)
                    createdFiles.Add(item.archivePath);

                return createdFiles.ToArray();
            }

            static (List<ResourceFile> resourceFiles, List<long> sizes, long totalSize) CreateResourceFilesFromLayout(ContentLayout layout)
            {
                var artifacts = layout?.BinaryArtifacts;
                var resourceFiles = new List<ResourceFile>(artifacts?.Count ?? 0);
                var sizes = new List<long>(artifacts?.Count ?? 0);
                long totalSize = 0;
                if (artifacts != null)
                {
                    foreach (var art in artifacts)
                    {
                        var file = new ResourceFile
                        {
                            fileName = kUdsScheme + art.ContentHash,
                            fileAlias = art.ContentHash,
                            serializedFile = art.Category == ContentDirectoryFileCategory.ContentFile,
                        };
                        resourceFiles.Add(file);
                        var size = (long)art.Size;
                        sizes.Add(size);
                        totalSize += size;
                    }
                }

                return (resourceFiles, sizes, totalSize);
            }

            internal static void CleanupRegistry(FileRegistry registry, List<ResourceFile> removed, List<(string archivePath, long uncompressedSize, List<ResourceFile> files)> added)
            {
                foreach (var file in removed)
                {
                    File.Delete(file.fileName);
                    registry.RemoveFile(file.fileName);
                }

                foreach (var file in added)
                {
                    registry.AddFile(file.archivePath);
                }
            }

            internal static List<ResourceFile> FilterFiles(List<string> filePaths, out List<long> sizes, out long totalSize)
            {
                totalSize = 0;
                var filteredFiles = new List<ResourceFile>();
                sizes = new List<long>();
                foreach (var file in filePaths)
                {
                    var name = Path.GetFileNameWithoutExtension(file);

                    //this is needed since the CAH file system only works with guid/hash values
                    if (!GUID.TryParse(name, out var _))
                        continue;

                    var uncompressSize = new FileInfo(file).Length;
                    sizes.Add(uncompressSize);
                    totalSize += uncompressSize;
                    var resourceFile = new ResourceFile();
                    resourceFile.fileName = file;
                    resourceFile.fileAlias = Path.GetFileName(file);
                    resourceFile.serializedFile = file.EndsWith(".cf");
                    filteredFiles.Add(resourceFile);
                }

                return filteredFiles;
            }

            internal static List<(string archivePath, long uncompressedSize, List<ResourceFile> files)> CreateArchiveWorkItems(string archiveOutputDirectory, List<ResourceFile> filteredFiles,
                IReadOnlyList<long> sizes, long totalSize, long targetUncompressedSize)
            {
                var workItems = new List<(string archivePath, long uncompressedSize, List<ResourceFile> files)>();
                int numBuckets = (int)Math.Max(1, (totalSize + targetUncompressedSize) / targetUncompressedSize);

                var buckets = new List<ResourceFile>[numBuckets];
                var bucketSizes = new long[numBuckets];
                for (int i = 0; i < numBuckets; i++)
                    buckets[i] = new List<ResourceFile>();

                for (int i = 0; i < filteredFiles.Count; i++)
                {
                    var rf = filteredFiles[i];
                    //items are bucketed by their hash code (the 0x7FFFFFFF part is to ensure positive values)
                    var name = Path.GetFileNameWithoutExtension(rf.fileName);
                    int bucketIndex = (Hash128.Parse(name).GetHashCode() & 0x7FFFFFFF) % numBuckets;
                    buckets[bucketIndex].Add(rf);
                    bucketSizes[bucketIndex] += sizes[i];
                }

                for (int i = 0; i < numBuckets; i++)
                {
                    if (buckets[i].Count > 0)
                    {
                        var archivePath = Path.Combine(archiveOutputDirectory, $"content{i}.archive");
                        workItems.Add((archivePath, bucketSizes[i], buckets[i]));
                    }
                }

                return workItems;
            }

            internal class ResourceFileComparer : IComparer<ResourceFile>
            {
                public int Compare(ResourceFile x, ResourceFile y)
                {
                    return x.fileName.CompareTo(y.fileName);
                }
            }

            internal static void ProcessArchiveWorkItems(List<(string archivePath, long uncompressedSize, List<ResourceFile> files)> workItems, IBuildLogger log)
            {
                var comparer = new ResourceFileComparer();
                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount) };
                var compression = BuildCompression.LZ4;

                using (log.ScopedStep(LogLevel.Info, "Archiving work items", true))
                {
                    Parallel.ForEach(workItems, parallelOptions, work =>
                    {
                        using (log.ScopedStep(LogLevel.Verbose, $"Writing {Path.GetFileName(work.archivePath)}",
                            ("ArchivePath", work.archivePath),
                            ("Compression", compression.compression.ToString()),
                            ("File count", work.files.Count.ToString()),
                            ("UncompressedSize", work.uncompressedSize.ToString()),
                            ("Thread", Thread.CurrentThread.ManagedThreadId.ToString())))
                        {
                            work.files.Sort(comparer);
                            var result = ContentBuildInterface.ArchiveAndCompress(
                                work.files.ToArray(),
                                work.archivePath,
                                compression);
                            if (result == 0)
                                throw new IOException("Archiving content directory failed.");
                            var compressedSize = new FileInfo(work.archivePath).Length;
                            log.AddArgSafe("CompressedSize", compressedSize.ToString());
                        }
                    });
                }
            }
        }
    }
}
#endif
