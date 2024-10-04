using System;
using System.Linq;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.BuildReportVisualizer;
using UnityEditor.AddressableAssets.Diagnostics;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests.Diagnostics.Profiler
{

    public class BuildLayoutBuilder
    {
        private BuildLayout m_BuildLayout;
        private BuildLayout.Group m_GroupContext;
        private BuildLayout.Bundle m_BundleContext;
        private BuildLayout.File m_FileContext;

        public BuildLayoutBuilder()
        {
            m_BuildLayout = new BuildLayout();
        }

        public BuildLayoutBuilder AddGroup(string name, string guid = null)
        {
            m_BundleContext = null;
            m_FileContext = null;
            if (guid == null)
            {
                guid = new Guid().ToString();
            }

            m_GroupContext = new BuildLayout.Group { Name = name, Guid = guid };
            m_BuildLayout.Groups.Add(m_GroupContext);
            return this;
        }

        public BuildLayout.Group FindGroup(string name)
        {
            return m_BuildLayout.Groups.First((g) => g.Name == name);
        }

        public BuildLayoutBuilder AddBundle(string name)
        {
            if (m_GroupContext == null)
                throw new System.Exception("You must add a group before adding a bundle.");

            var internalName = Hash128.Compute(name);
            var bundle = new BuildLayout.Bundle { Name = name, InternalName = internalName + ".bundle", Group = m_GroupContext };
            m_GroupContext.Bundles.Add(bundle);
            m_BundleContext = bundle;
            m_FileContext = new BuildLayout.File { Bundle = bundle, Name = name };
            m_BundleContext.Files.Add(m_FileContext);
            return this;
        }

        public BuildLayout.Bundle FindBundle(string groupName, string bundleName)
        {
            var group = FindGroup(groupName);
            return group.Bundles.First((b) => b.Name == bundleName);
        }

        public BuildLayout.ExplicitAsset FindExplicitAsset(string groupName, string bundleName, string addressableName)
        {
            var group = FindGroup(groupName);
            var bundle = FindBundle(groupName, bundleName);
            foreach (var file in bundle.Files)
            {
                var asset = file.Assets.FirstOrDefault((a) => a.AddressableName == addressableName);
                if (asset != null)
                {
                    return asset;
                }
            }
            return null;
        }

        public BuildLayoutBuilder AddAsset(string name, string internalId, AssetType assetType = AssetType.Mesh)
        {
            if (m_FileContext == null)
                throw new System.Exception("You must add a bundle before adding an asset.");

            var guid = new Guid().ToString();
            return AddAsset(new BuildLayout.ExplicitAsset { AddressableName = name, Bundle = m_BundleContext, InternalId = internalId, Guid = guid, MainAssetType = AssetType.Mesh});
        }

        public BuildLayoutBuilder AddAsset(BuildLayout.ExplicitAsset asset)
        {
            if (m_FileContext == null)
                throw new System.Exception("You must add a bundle before adding an asset.");

            asset.Objects.Add(new BuildLayout.ObjectData
            {
                AssetType = asset.MainAssetType
            });
            m_FileContext.Assets.Add(asset);
            return this;
        }

        public BuildLayoutBuilder AddAssetReference(string addressableName)
        {
            return this;
        }

    public BuildLayoutBuilder AddToLayoutManager()
        {
            var hash = new Hash128();
            m_BuildLayout.m_BodyRead = true;
            m_BuildLayout.BuildResultHash = hash.ToString();
            AddressablesProfilerViewController.LayoutsManager.AddActiveLayout(m_BuildLayout);
            return this;
        }
    }

}
