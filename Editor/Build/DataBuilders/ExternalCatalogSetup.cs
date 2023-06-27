using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
	/// <summary>
	/// Separate catalog for the assigned asset groups.
	/// </summary>
	[CreateAssetMenu(menuName = "Addressables/External Catalog", fileName = "ExternalCatalogSetup")]
	public class ExternalCatalogSetup : ScriptableObject
	{
		[SerializeField, Tooltip("Assets groups that belong to this catalog. Entries found in these will get extracted from the default catalog.")]
		private List<AddressableAssetGroup> assetGroups = new List<AddressableAssetGroup>();
		[SerializeField, Tooltip("Build path for the produced files associated with this catalog.")]
		private ProfileValueReference buildPath = new ProfileValueReference();
		[SerializeField, Tooltip("Runtime load path for assets associated with this catalog.")]
		private ProfileValueReference runtimeLoadPath = new ProfileValueReference();
		[SerializeField, Tooltip("Catalog name. This will also be the name of the exported catalog file.")]
		private string catalogName = string.Empty;

		/// <summary>
		/// Name of the catalog. This will also be the name of the exported catalog file.
		/// </summary>
		public string CatalogName
		{
			get => catalogName;
			set => catalogName = value;
		}

		/// <summary>
		/// Build path for the produced files associated with this catalog.
		/// </summary>
		public ProfileValueReference BuildPath
		{
			get => buildPath;
			set => buildPath = value;
		}

		/// <summary>
		/// Runtime load path for assets associated with this catalog.
		/// </summary>
		public ProfileValueReference RuntimeLoadPath
		{
			get => runtimeLoadPath;
			set => runtimeLoadPath = value;
		}

		/// <summary>
		/// Assets groups that belong to this catalog. Entries found in these will get extracted from the default catalog.
		/// </summary>
		public IReadOnlyList<AddressableAssetGroup> AssetGroups
		{
			get => assetGroups;
			set => assetGroups = new List<AddressableAssetGroup>(value);
		}

		/// <summary>
		/// Is the data entry part of this external catalog?
		/// </summary>
		/// <param name="loc">The data entry location.</param>
		/// <param name="aaContext">The Addressables build context.</param>
		/// <returns>True if it's part of the catalog. False otherwise.</returns>
		public bool IsPartOfCatalog(ContentCatalogDataEntry loc, AddressableAssetsBuildContext aaContext)
		{
			// Don't bother if the asset groups is empty.
			if (assetGroups == null || assetGroups.Count <= 0)
			{
				return false;
			}

			if ((loc.ResourceType == typeof(IAssetBundleResource)))
			{
				AddressableAssetEntry entry = aaContext.assetEntries.Find(ae => string.Equals(ae.BundleFileId, loc.InternalId));
				if (entry != null)
				{
					return assetGroups.Exists(ag => ag.entries.Contains(entry));
				}

				// If no entry was found, it may refer to a folder asset.
				return assetGroups.Exists(ag => ag.entries.Any(e => e.IsFolder && string.Equals(e.BundleFileId, loc.InternalId)));
			}
			else
			{
				return assetGroups.Exists(ag => ag.entries.Any(e => (e.IsFolder && e.SubAssets.Any(a => loc.Keys.Contains(a.guid))) || loc.Keys.Contains(e.guid)));
			}
		}

		/// <summary>
		/// Add an Addressable asset group to this external content catalog.
		/// </summary>
		/// <param name="assetGroup">The Addressable asset group to add.</param>
		public void AddAssetGroupToCatalog(AddressableAssetGroup assetGroup)
		{
			if (assetGroup == null)
			{
				throw new ArgumentNullException(nameof(assetGroup));
			}

			if (!assetGroups.Contains(assetGroup))
			{
				Undo.RecordObject(this, nameof(AddAssetGroupToCatalog));
				assetGroups.Add(assetGroup);
				EditorUtility.SetDirty(this);
			}
		}

		/// <summary>
		/// Removes all instances of the provided Addressable asset group from this external catalog.
		/// </summary>
		/// <param name="assetGroup">The Addressable asset group to remove.</param>
		public void RemoveAssetGroupFromCatalog(AddressableAssetGroup assetGroup)
		{
			if (assetGroups.Exists(aag => aag == assetGroup))
			{
				Undo.RecordObject(this, nameof(RemoveAssetGroupFromCatalog));
				assetGroups.RemoveAll(aag => aag == assetGroup);
				EditorUtility.SetDirty(this);
			}
		}

		private void OnEnable()
		{
			buildPath.OnValueChanged += OnProfileValueChanged;
			runtimeLoadPath.OnValueChanged += OnProfileValueChanged;
		}

		private void OnDisable()
		{
			buildPath.OnValueChanged -= OnProfileValueChanged;
			runtimeLoadPath.OnValueChanged -= OnProfileValueChanged;
		}

		private void OnProfileValueChanged(ProfileValueReference valueReference)
		{
			EditorUtility.SetDirty(this);
		}
	}
}
