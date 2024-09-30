namespace AddressableAssets.DocExampleCode
{
	using System.IO;
    using UnityEngine.AddressableAssets;

    internal class UsingStreamingAssetsSubFolder
    {
		#region SAMPLE
        class StreamingAssetBuilds
        {
            public static string[] GetBuiltPlatforms()
            {
                // list all platform folders in the addressable asset build directory:
                var addressableBuildPath = Addressables.LibraryPath + Addressables.StreamingAssetsSubFolder;
                return Directory.GetDirectories(addressableBuildPath);                
            }
        }
        
		#endregion
	}
}
