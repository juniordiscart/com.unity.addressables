using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AddressableAssets.DocExampleCode
{
	using System;
    using UnityEngine;

	internal class UsingCleanBundleCache
    {
        #region DECLARATION
        public static AsyncOperationHandle<bool> CleanBundleCache(IEnumerable<string> catalogsIds = null)
        #endregion
        {
            return default;
        }

		#region SAMPLE_ALL
		public void UsingCleanBundleCacheForAllCatalogs()
		{
			// clear for all currently loaded catalogs
			// if catalogIds are provided, only those catalogs are used from the currently loaded
			AsyncOperationHandle<bool> cleanBundleCacheHandle = Addressables.CleanBundleCache();
			cleanBundleCacheHandle.Completed += op =>
			{
                // during caching a reference is added to the catalogs.
                // release is needed to reduce the reference and allow catalog to be uncached for updating
				Addressables.Release(op);
			};
		}
		#endregion

		#region SAMPLE_SPECIFY
		public void UsingCleanBundleCacheWithcatalogIds()
		{
			HashSet<string> catalogsIds = new HashSet<string>();
			foreach (var locator in Addressables.ResourceLocators)
			{
				if (locator.LocatorId == "AddressablesMainContentCatalog")
				{
					catalogsIds.Add(locator.LocatorId);
					break;
				}
			}

			if (catalogsIds.Count == 0)
				return;

			var cleanBundleCacheHandle = Addressables.CleanBundleCache(catalogsIds);
			cleanBundleCacheHandle.Completed += op =>
			{
				// during caching a reference is added to the catalogs.
				// release is needed to reduce the reference and allow catalog to be uncached for updating
				Addressables.Release(op);
			};
		}
		#endregion
	}
}
