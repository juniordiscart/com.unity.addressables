using System;
using System.Runtime.InteropServices;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.Profiling
{
    [System.Flags]
    internal enum ContentStatus
    {
        None = 0,
        // 1
        Queue = 2,
        Downloading = 4,
        // 8
        Released = 16,
        // 32
        Loading = 64,
        // 128
        Active = 256,
    }

    [System.Flags]
    internal enum BundleOptions : short
    {
        None = 0,
        CachingEnabled = 1,
        CheckSumEnabled = 2
    };

    [StructLayout(LayoutKind.Sequential)]
    internal struct CatalogFrameData
    {
        public Hash128 BuildResultHash;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BundleFrameData
    {
        public int BundleCode;
        public int ReferenceCount;
        public float PercentComplete;
        public ContentStatus Status;
        public BundleSource Source;
        public BundleOptions LoadingOptions;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AssetFrameData
    {
        public int AssetCode;
        public int BundleCode;
        public int ReferenceCount;
        public float PercentComplete;
        public ContentStatus Status;

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is AssetFrameData other)
            {
                return AssetCode == other.AssetCode &&
                       BundleCode == other.BundleCode;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AssetCode.GetHashCode(), BundleCode.GetHashCode(), ReferenceCount.GetHashCode(), PercentComplete.GetHashCode(), Status.GetHashCode());
        }
    }
}
