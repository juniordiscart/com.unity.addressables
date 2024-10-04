namespace AddressableAssets.DocExampleCode
{
	using System.Collections;
	using System.IO;
	using UnityEngine;
	using UnityEngine.AddressableAssets;
	using UnityEngine.AddressableAssets.ResourceLocators;
	using UnityEngine.ResourceManagement.AsyncOperations;
	using UnityEngine.ResourceManagement.ResourceLocations;
	using UnityEngine.ResourceManagement.ResourceProviders;
	
	internal class UsingAddResourceLocator
    {
	    #region DECLARATION
	    public static void AddResourceLocator(IResourceLocator locator, string localCatalogHash = null, IResourceLocation remoteCatalogLocation = null)
	    #endregion
	    { }

	    #region SAMPLE_ADDLOCATOR
		private string m_SourceFolder = "dataFiles";
		
		public void AddFileLocatorToAddressables()
		{
			if (!Directory.Exists(m_SourceFolder))
				return;
			
			ResourceLocationMap locator = new ResourceLocationMap(m_SourceFolder + "_FilesLocator", 12);
			string providerId = typeof(TextDataProvider).ToString();
        
			string[] files = Directory.GetFiles(m_SourceFolder);
			foreach (string filePath in files)
			{
				if (!filePath.EndsWith(".json"))
					continue;
				string keyForLoading = Path.GetFileNameWithoutExtension(filePath);
				locator.Add(keyForLoading, new ResourceLocationBase(keyForLoading, filePath, providerId, typeof(string)));
			}
			Addressables.AddResourceLocator(locator);
		}
		#endregion

		#region SAMPLE_LOADING
		private string m_DataFileName = "settings";
		
		public IEnumerator LoadDataUsingAddedLocator()
		{
			var loadingHandle = Addressables.LoadAssetAsync<string>(m_DataFileName);
			yield return loadingHandle;
			Debug.Log("Load completed " + loadingHandle.Status + (loadingHandle.Status == AsyncOperationStatus.Succeeded ? ", with result " + loadingHandle.Result : ""));
		}
		#endregion
	}
	
}
