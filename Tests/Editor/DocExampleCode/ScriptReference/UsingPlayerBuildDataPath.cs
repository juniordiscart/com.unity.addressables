namespace AddressableAssets.DocExampleCode
{
    #region SAMPLE
    using System.IO;
    using UnityEditor;
    using UnityEditor.AddressableAssets.Build;
    using UnityEditor.AddressableAssets.Build.DataBuilders;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEngine;
    using UnityEngine.AddressableAssets;


    [CreateAssetMenu(fileName = "CustomLocationBuildScript.asset", menuName = "Addressables/Custom Build/Custom Location Build Script")]
    public class CustomLocationBuildScript : BuildScriptPackedMode
    {

        // BuildPath is set to [CustomLocationBuildScript.CustomLocationBuildRoot]/SpecialGroup/[BuildTarget]
        // LoadPath is set to {UnityEngine.AddressableAssets.Addressables.PlayerBuildDataPath}/SpecialGroup/[BuildTarget]
        public static string CustomLocationBuildRoot
        {
            get { return "CustomBuildPath"; }
        }

        public override string Name
        {
            get { return "Custom Location Build Script"; }
        }

        protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput context)
        {
            var result = base.BuildDataImplementation<TResult>(context);

            AddressableAssetSettings settings = context.AddressableSettings;
            CopyBundles(settings);
            return result;
        }

        void CopyBundles(AddressableAssetSettings settings)
        {

            // if the PlayerBuildDataPath does not exist, create it
            var streamingAssetsPath = Addressables.PlayerBuildDataPath;
            if (!Directory.Exists(streamingAssetsPath))
            {
                Directory.CreateDirectory(streamingAssetsPath);
            }

            // Copy all directories from the CustomLocationBuildRoot to the PlayerBuildDataPath
            var directories = Directory.GetDirectories(CustomLocationBuildRoot);
            foreach (var directory in directories)
            {
                var fileName = Path.GetFileName(directory);
                FileUtil.ReplaceFile($"{CustomLocationBuildRoot}/{fileName}", $"{streamingAssetsPath}/{fileName}");
            }
        }
    }
    #endregion
}
