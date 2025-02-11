namespace AddressableAssets.DocExampleCode
{
    using UnityEditor.AddressableAssets.Settings;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Settings.GroupSchemas;

    internal class UsingCreateGroup
    {
        #region SAMPLE
        public AddressableAssetGroup CreateNewGroup()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings.CreateGroup("MyNewGroup", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            return group;
        }
        #endregion
    }
}
