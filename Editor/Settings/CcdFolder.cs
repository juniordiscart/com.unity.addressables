using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityEditor.AddressableAssets.Settings
{
    internal interface ICcdFolder<T> where T : class
    {
        void GetChildren(DirectoryInfo startDirectory);
    }

    internal class CcdBuildDataFolder : ICcdFolder<CcdEnvironmentFolder>
    {
        public string Name;
        public string Location;
        public List<CcdEnvironmentFolder> Environments = new List<CcdEnvironmentFolder>();

        public void GetChildren(DirectoryInfo startDirectory)
        {
            var envDirs = startDirectory.GetDirectories().Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden));
            foreach (var dir in envDirs)
            {
                var envFolder = new CcdEnvironmentFolder();
                envFolder.Name = dir.Name;
                envFolder.Location = dir.FullName;
                envFolder.GetChildren(dir);
                Environments.Add(envFolder);
            }
        }
    }

    internal class CcdEnvironmentFolder : ICcdFolder<CcdBucketFolder>
    {
        public string Name;
        public string Location;
        public List<CcdBucketFolder> Buckets = new List<CcdBucketFolder>();

        public void GetChildren(DirectoryInfo startDirectory)
        {
            var bucketDirs = startDirectory.GetDirectories().Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden));
            foreach (var dir in bucketDirs)
            {
                var bucketFolder = new CcdBucketFolder();
                bucketFolder.Name = dir.Name;
                bucketFolder.Location = dir.FullName;
                bucketFolder.GetChildren(dir);
                Buckets.Add(bucketFolder);
            }
        }
    }

    internal class CcdBucketFolder : ICcdFolder<CcdBadgeFolder>
    {
        public string Name;
        public string Location;
        public List<CcdBadgeFolder> Badges = new List<CcdBadgeFolder>();

        public void GetChildren(DirectoryInfo startDirectory)
        {
            var badgeDirs = startDirectory.GetDirectories().Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden));
            foreach (var dir in badgeDirs)
            {
                var badgeFolder = new CcdBadgeFolder();
                badgeFolder.Name = dir.Name;
                badgeFolder.Location = dir.FullName;
                badgeFolder.GetChildren(dir);
                Badges.Add(badgeFolder);
            }
        }
    }

    internal class CcdBadgeFolder : ICcdFolder<FileInfo>
    {
        public string Name;
        public string Location;
        public List<FileInfo> Files = new List<FileInfo>();

        public void GetChildren(DirectoryInfo startDirectory)
        {
            Files = startDirectory.GetFiles().Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden)).ToList();
        }
    }
}
