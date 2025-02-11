namespace AddressableAssets.DocExampleCode
{
    using System.Collections.Generic;
    using UnityEditor.AddressableAssets.Settings.GroupSchemas;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEditor;
    using static UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema;

    internal class UsingDefaultSchemaSettingsBuildTargetGroup
    {
        #region SAMPLE
        public DefaultSchemaSettings[] GetDefaultSchemaSettings(AddressableAssetGroup group)
        {
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Dictionary<DefaultSchemaSettingsBuildTargetGroup, DefaultSchemaSettings[]> defaultSettings = schema.CreateDefaultSchemaSettings();
            DefaultSchemaSettingsBuildTargetGroup targetGroup = schema.GetDefaultSchemaSettingsBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            return defaultSettings[targetGroup];
        }
        #endregion
    }
}
