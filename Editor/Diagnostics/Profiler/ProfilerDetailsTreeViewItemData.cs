#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEngine;
using UnityEngine.ResourceManagement.Profiling;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class ContentData
    {
        public string Name;
        public int AddressableHandles;
        public ContentStatus Status;
        protected float m_PercentComplete;

        public float PercentComplete
        {
            get
            {
                if (Status == ContentStatus.Loading)
                    return m_PercentComplete;
                if (Status == ContentStatus.Active)
                    return 1f;
                return -1f;
            }
        }

        public virtual int TreeViewID
        {
            get { return Name.GetHashCode(); }
        }

        public readonly List<ContentData> Children = new List<ContentData>();
        public ContentData Parent = null;

        public readonly List<ContentData> ThisReferencesOther = new List<ContentData>();
        public readonly List<ContentData> ReferencesToThis = new List<ContentData>();

        public void AddReferenceTo(ContentData value)
        {
            if (!ThisReferencesOther.Contains(value))
                ThisReferencesOther.Add(value);
        }

        public void AddReferenceBy(ContentData value)
        {
            if (!ReferencesToThis.Contains(value))
                ReferencesToThis.Add(value);
        }

        public void AddChild(ContentData child)
        {
            Children.Add(child);
            child.Parent = this;
        }

        public virtual string GetCellContent(string colName)
        {
            return colName switch
            {
                TreeColumnNames.TreeColumnName => Name,
                TreeColumnNames.TreeColumnType => "",
                TreeColumnNames.TreeColumnAddressedCount => AddressableHandles.ToString(),
                TreeColumnNames.TreeColumnStatus => Status.ToString(),
                TreeColumnNames.TreeColumnPercentage => PercentComplete < 0.5f ? "" : (int)(PercentComplete*100f)+"%",
                TreeColumnNames.TreeColumnReferencedBy => ReferencesToThis.Count.ToString(),
                TreeColumnNames.TreeColumnReferencesTo => ThisReferencesOther.Count.ToString(),
                _ => ""
            };
        }
    }

    internal class GroupData : ContentData
    {
        public BuildLayout.Group m_ReportGroup;

        public GroupData(BuildLayout.Group reportGroup)
        {
            m_ReportGroup = reportGroup;
            if (m_ReportGroup == null)
            {
                Name = "Missing group";
                return;
            }
            Name = reportGroup.Name;
        }

        public override string GetCellContent(string colName)
        {
            return colName switch
            {
                TreeColumnNames.TreeColumnName => Name,
                _ => ""
            };
        }
    }

    internal class BundleData : ContentData
    {
        public readonly int BundleCode;
        public readonly BundleSource Source;
        public bool CheckSumEnabled;
        public bool CachingEnabled;
        internal BuildLayout.Bundle ReportBundle;

        public List<ContentData> NotLoadedChildren = new List<ContentData>();
        public Dictionary<string, int> AssetGuidToChildIndex = new Dictionary<string, int>();

        public override int TreeViewID
        {
            get { return BundleCode; }
        }

        public override string GetCellContent(string colName)
        {
            if (colName == TreeColumnNames.TreeColumnType)
                return "Asset Bundle";
            return base.GetCellContent(colName);
        }

        public BundleData(BuildLayout.Bundle reportBundle, BundleFrameData frameData)
        {
            ReportBundle = reportBundle;
            Name = reportBundle != null ? reportBundle.Name : frameData.BundleCode.ToString();
            BundleCode = frameData.BundleCode;
            Source = frameData.Source;
            m_PercentComplete = frameData.PercentComplete;
            CachingEnabled = frameData.LoadingOptions.HasFlag(BundleOptions.CachingEnabled);
            CheckSumEnabled = frameData.LoadingOptions.HasFlag(BundleOptions.CheckSumEnabled);
            AddressableHandles = frameData.ReferenceCount;
            Status = frameData.Status;
        }

        public AssetData GetOrCreateAssetData(BuildLayout.ExplicitAsset asset)
        {
            if (AssetGuidToChildIndex.TryGetValue(asset.Guid, out int value))
                return Children[value] as AssetData;

            AssetData assetData = new AssetData(asset);
            AssetGuidToChildIndex.Add(asset.Guid, Children.Count);
            Children.Add(assetData);
            assetData.Parent = this;
            return assetData;
        }

        public AssetData GetOrCreateAssetData(BuildLayout.DataFromOtherAsset asset)
        {
            if (AssetGuidToChildIndex.TryGetValue(asset.AssetGuid, out int value))
                return Children[value] as AssetData;

            AssetData assetData = new AssetData(asset);
            AssetGuidToChildIndex.Add(asset.AssetGuid, Children.Count);
            Children.Add(assetData);
            assetData.Parent = this;
            return assetData;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="assetData"></param>
        /// <returns>true if new assetData is created, else false is already a child of bundle</returns>
        public bool GetOrCreateAssetData(BuildLayout.DataFromOtherAsset asset, out AssetData assetData)
        {
            if (AssetGuidToChildIndex.TryGetValue(asset.AssetGuid, out int value))
            {
                assetData = Children[value] as AssetData;
                return false;
            }

            assetData = new AssetData(asset);
            AssetGuidToChildIndex.Add(asset.AssetGuid, Children.Count);
            Children.Add(assetData);
            assetData.Parent = this;
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="assetData"></param>
        /// <returns>true if new assetData is created, else false is already a child of bundle</returns>
        public bool GetOrCreateAssetData(BuildLayout.ExplicitAsset asset, out AssetData assetData)
        {
            if (AssetGuidToChildIndex.TryGetValue(asset.Guid, out int value))
            {
                assetData = Children[value] as AssetData;
                return false;
            }

            assetData = new AssetData(asset);
            AssetGuidToChildIndex.Add(asset.Guid, Children.Count);
            Children.Add(assetData);
            assetData.Parent = this;
            return true;
        }
    }

    internal class AssetData : ContentData
    {
        public readonly string AssetGuid;
        public readonly AssetType MainAssetType;
        public bool IsImplicit => ReportImplicitData != null;
        public bool FullyLoaded => LoadedObjectIndices.Count == 0 || LoadedObjectIndices.Count == Children.Count;

        public readonly BuildLayout.ExplicitAsset ReportExplicitData;
        public readonly BuildLayout.DataFromOtherAsset ReportImplicitData;

        public List<BuildLayout.ObjectData> ReportObjects
        {
            get
            {
                if (ReportExplicitData != null)
                    return ReportExplicitData.Objects;
                return ReportImplicitData.Objects;
            }
        }

         // if empty = all
        public HashSet<int> LoadedObjectIndices = new HashSet<int>();
        public Dictionary<long, int> LocalIdToChildIndex = new Dictionary<long, int>();

        public string AssetPath => ReportExplicitData != null ? ReportExplicitData.AssetPath : ReportImplicitData != null ? ReportImplicitData.AssetPath : null;

        public override int TreeViewID
        {
            get
            {
                if (IsImplicit && Parent != null)
                {
                    BundleData bundleData = (BundleData)Parent;
                    return HashCode.Combine(bundleData.BundleCode, AssetGuid.GetHashCode());
                }
                return HashCode.Combine(AssetGuid.GetHashCode(), AssetPath?.GetHashCode());
            }
        }

        public override string GetCellContent(string colName)
        {
            if (colName == TreeColumnNames.TreeColumnType)
                return MainAssetType.ToString();
            return base.GetCellContent(colName);
        }

        public AssetData(AssetFrameData frameData)
        {
            Name = frameData.AssetCode.ToString();
            AddressableHandles = frameData.ReferenceCount;
            Status = frameData.Status;
            AssetGuid = "";
        }

        public AssetData(BuildLayout.ExplicitAsset reportAsset)
        {
            Debug.Assert(reportAsset != null);
            Name = reportAsset.AddressableName == null ? reportAsset.AssetPath : reportAsset.AddressableName;
            AddressableHandles = 0;
            AssetGuid = reportAsset.Guid;
            MainAssetType = reportAsset.MainAssetType;
            ReportExplicitData = reportAsset;
        }

        public AssetData(BuildLayout.DataFromOtherAsset reportAsset)
        {
            Debug.Assert(reportAsset != null);
            Name = reportAsset.AssetPath;
            AddressableHandles = 0;
            AssetGuid = reportAsset.AssetGuid;
            MainAssetType = reportAsset.MainAssetType;
            ReportImplicitData = reportAsset;
        }

        public void Update(AssetFrameData frameData)
        {
            AddressableHandles = frameData.ReferenceCount;
            Status = frameData.Status;
            m_PercentComplete = frameData.PercentComplete;
        }

        public void AddLoadedObjects(List<int> indicesAdding)
        {
            // if (this.AddressedCount > 0) // fully loaded
            //     return;
            this.LoadedObjectIndices.UnionWith(indicesAdding);
        }

        public ObjectData GetOrCreateObjectData(BuildLayout.ObjectData obj)
        {
            if (LocalIdToChildIndex.TryGetValue(obj.LocalIdentifierInFile, out int value))
                return Children[value] as ObjectData;

            ObjectData objectData = new ObjectData(obj);
            LocalIdToChildIndex.Add(obj.LocalIdentifierInFile, Children.Count);
            Children.Add(objectData);
            objectData.Parent = this;
            return objectData;
        }
    }

    internal class ObjectData : ContentData
    {
        public readonly AssetType AssetType;
        public readonly string ComponentName;
        private readonly long LocalIdentifierInFile;

        public override int TreeViewID
        {
            get
            {
                if (Parent != null)
                {
                    AssetData assetData = (AssetData)Parent;
                    return HashCode.Combine(assetData.TreeViewID, LocalIdentifierInFile.GetHashCode());
                }
                return LocalIdentifierInFile.GetHashCode();
            }
        }

        public override string GetCellContent(string colName)
        {
            return colName switch
            {
                TreeColumnNames.TreeColumnType => AssetType.ToString(),
                TreeColumnNames.TreeColumnAddressedCount => "",
                _ => base.GetCellContent(colName)
            };
        }

        public ObjectData(BuildLayout.ObjectData obj)
        {
            LocalIdentifierInFile = obj.LocalIdentifierInFile;
            if (string.IsNullOrEmpty(obj.ObjectName))
                Name = obj.LocalIdentifierInFile.ToString();
            else if (!string.IsNullOrEmpty(obj.ComponentName))
                Name = $"{obj.ObjectName} ({obj.ComponentName})";
            else
                Name = obj.ObjectName;
            AssetType = obj.AssetType;
            ComponentName = obj.ComponentName;
        }
    }

    internal class ContainerData : ContentData
    {
        public override int TreeViewID
        {
            get
            {
                if (Parent != null)
                {
                    if (Parent is BundleData bundleParent)
                        return HashCode.Combine(bundleParent.BundleCode, Name.GetHashCode());
                    return HashCode.Combine(Parent.GetHashCode(), Name.GetHashCode());
                }
                return Name.GetHashCode();
            }
        }

        public override string GetCellContent(string colName)
        {
            return colName switch
            {
                TreeColumnNames.TreeColumnType => "",
                TreeColumnNames.TreeColumnAddressedCount => "",
                TreeColumnNames.TreeColumnStatus => "",
                TreeColumnNames.TreeColumnReferencedBy => "",
                TreeColumnNames.TreeColumnReferencesTo => "",
                _ => base.GetCellContent(colName)
            };
        }
    }
}

#endif
