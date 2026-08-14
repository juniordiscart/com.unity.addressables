#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.Build.Content;
using UnityEngine;
using BuildCompression = UnityEngine.BuildCompression;

namespace UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders
{
    internal static class ContentDirectoryArchiver
    {
        internal static void ArchiveAndUpdateRegistry(string archiveOutputDirectory, long avgMaxSize, FileRegistry registry)
        {
            var filteredFiles = FilterFiles(registry, out var totalSize);
            var workItems = CreateArchiveWorkItems(archiveOutputDirectory, filteredFiles, totalSize, avgMaxSize);
            ProcessArchiveWorkItems(workItems);
            CleanupRegistry(registry, filteredFiles, workItems);
        }

        internal static void CleanupRegistry(FileRegistry registry, List<ResourceFile> removed, List<(string archivePath, List<ResourceFile> files)> added)
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

        internal static List<ResourceFile> FilterFiles(FileRegistry registry, out long totalSize)
        {
            totalSize = 0;
            var filteredFiles = new List<ResourceFile>();
            foreach (var file in registry.GetFilePaths())
            {
                var name = Path.GetFileNameWithoutExtension(file);

                //this is needed since the CAH file system only works with guid/hash values
                if (!GUID.TryParse(name, out var _))
                    continue;

                totalSize += new FileInfo(file).Length;
                var resourceFile = new ResourceFile();
                resourceFile.fileName = file;
                resourceFile.fileAlias = Path.GetFileName(file);
                resourceFile.serializedFile = file.EndsWith(".cf");
                filteredFiles.Add(resourceFile);
            }
            return filteredFiles;
        }

        internal static List<(string archivePath, List<ResourceFile> files)> CreateArchiveWorkItems(string archiveOutputDirectory, List<ResourceFile> filteredFiles, long totalSize, long avgMaxSize)
        {
            var workItems = new List<(string archivePath, List<ResourceFile> files)>();
            int numBuckets = (int)Math.Max(1, (totalSize + avgMaxSize) / avgMaxSize);

            var buckets = new List<ResourceFile>[numBuckets];
            for (int i = 0; i < numBuckets; i++)
                buckets[i] = new List<ResourceFile>();

            foreach (var rf in filteredFiles)
            {
                //items are bucketed by their hash code (the 0x7FFFFFFF part is to ensure positive values)
                var name = Path.GetFileNameWithoutExtension(rf.fileName);
                int bucketIndex = (Hash128.Parse(name).GetHashCode() & 0x7FFFFFFF) % numBuckets;
                buckets[bucketIndex].Add(rf);
            }

            for (int i = 0; i < numBuckets; i++)
            {
                if (buckets[i].Count > 0)
                {
                    var archivePath = Path.Combine(archiveOutputDirectory, $"content{i}.archive");
                    workItems.Add((archivePath, buckets[i]));
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

        internal static void ProcessArchiveWorkItems(List<(string archivePath, List<ResourceFile> files)> workItems)
        {
            var comparer = new ResourceFileComparer();
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount) };

            Parallel.ForEach(workItems, parallelOptions, work =>
            {
                work.files.Sort(comparer);
                ContentBuildInterface.ArchiveAndCompress(
                    work.files.ToArray(),
                    work.archivePath,
                    BuildCompression.LZ4);
            });
        }
    }
}
#endif
