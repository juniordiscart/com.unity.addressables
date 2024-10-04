using System.Collections.Generic;

namespace AddressableAssets.DocExampleCode
{
	using System;
    using UnityEngine;
	using UnityEngine.AddressableAssets;
	using UnityEngine.ResourceManagement.AsyncOperations;

	internal class UsingCheckForCatalogUpdates
	{
        #region DECLARATION
        public static AsyncOperationHandle<List<string>> CheckForCatalogUpdates(bool autoReleaseHandle = true)
        #endregion
        {
            return default;
        }

	    public void UsingCheckForCatalogUpdatesSample()
		{
			#region SAMPLE
			Addressables.CheckForCatalogUpdates().Completed += op =>
			{
				if (op.Status == AsyncOperationStatus.Succeeded)
				{
					foreach (string catalogWithUpdate in op.Result)
						Debug.Log($"Catalog {catalogWithUpdate} has an available update.");
				}
			};
			#endregion
		}
	}
}
