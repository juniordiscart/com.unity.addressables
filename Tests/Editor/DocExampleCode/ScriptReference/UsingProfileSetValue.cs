namespace AddressableAssets.DocExampleCode
{
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings;

    internal class UsingProfileSetValue
    {
        #region SAMPLE
        public void UpdateRemoteLoadPath()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetProfileSettings profileSettings = settings.profileSettings;
            string activeProfileId = settings.activeProfileId;

            string variableName = AddressableAssetSettings.kRemoteLoadPath;
            profileSettings.SetValue(activeProfileId, variableName, "https://myhost/mycontent");
            AssetDatabase.SaveAssetIfDirty(settings);
        }
        #endregion
    }
}
