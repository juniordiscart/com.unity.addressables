using UnityEditor.AddressableAssets.Settings;

namespace Tests.Editor.GUI
{
    internal class SOTestSchema : AddressableAssetGroupSchema
    {
        public enum CustomEnum
        {
            OptionA,
            OptionB,
            OptionC
        }

        public CustomEnum CustomDescription;
    }
}
