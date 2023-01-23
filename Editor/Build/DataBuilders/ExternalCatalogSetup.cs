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
		private string buildPath = string.Empty;
		[SerializeField, Tooltip("Runtime load path for assets associated with this catalog.")]
		private string runtimeLoadPath = string.Empty;
		[SerializeField, Tooltip("Catalog name. This will also be the name of the exported catalog file.")]
		private string catalogName = string.Empty;

		public string CatalogName
		{
			get => catalogName;
			set => catalogName = value;
		}

		public string BuildPath
		{
			get => buildPath;
			set => buildPath = value;
		}

		public string RuntimeLoadPath
		{
			get => runtimeLoadPath;
			set => runtimeLoadPath = value;
		}

		public IReadOnlyList<AddressableAssetGroup> AssetGroups
		{
			get => assetGroups;
			set => assetGroups = new List<AddressableAssetGroup>(value);
		}

		public bool IsPartOfCatalog(ContentCatalogDataEntry loc, AddressableAssetsBuildContext aaContext)
		{
			if ((assetGroups != null) && (assetGroups.Count > 0))
			{
				if ((loc.ResourceType == typeof(IAssetBundleResource)))
				{
					AddressableAssetEntry entry = aaContext.assetEntries.Find(ae => string.Equals(ae.BundleFileId, loc.InternalId));
					if (entry != null)
					{
						return assetGroups.Exists(ag => ag.entries.Contains(entry));
					}

					// If no entry was found, it may refer to a folder asset.
					return assetGroups.Exists(ag => ag.entries.Any(e => e.IsFolder && e.BundleFileId.Equals(loc.InternalId)));
				}
				else
				{
					return assetGroups.Exists(ag => ag.entries.Any(e => (e.IsFolder && e.SubAssets.Any(a => loc.Keys.Contains(a.guid))) || loc.Keys.Contains(e.guid)));
				}
			}
			else
			{
				return false;
			}
		}
	}
}
