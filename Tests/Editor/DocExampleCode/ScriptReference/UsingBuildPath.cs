namespace AddressableAssets.DocExampleCode
{
    using UnityEngine;
	using UnityEngine.AddressableAssets;
	using UnityEngine.AddressableAssets.Initialization;
	
	internal class UsingBuildPath
    {
	    #region GET_SETTINGS_FROM_BUILDPATH
		public string GetBuiltContentAddressablesVersion()
		{
			string settingsPath = Addressables.BuildPath + "/settings.json";
			if (System.IO.File.Exists(settingsPath))
			{
				string json = System.IO.File.ReadAllText(settingsPath);
				ResourceManagerRuntimeData activeRuntimeSettings =
					JsonUtility.FromJson<ResourceManagerRuntimeData>(json);
				return activeRuntimeSettings.AddressablesVersion;
			}
			return null;
		}
		#endregion
	}
}
