namespace AddressableAssets.DocExampleCode
{
#if UNITY_EDITOR
    using UnityEditor.AddressableAssets.Build;
    using UnityEditor.AddressableAssets.Build.DataBuilders;

    internal class CustomBuildScript : BuildScriptBase
    {
        #region doc_CustomBuildScript

        public override bool CanBuildData<T>()
        {
            return typeof(T).IsAssignableFrom(typeof(AddressablesPlayerBuildResult));
        }

        #endregion
    }

    internal class CustomPlayModeScript : BuildScriptBase
    {
        #region doc_CustomPlayModeScript

        public override bool CanBuildData<T>()
        {
            return typeof(T).IsAssignableFrom(typeof(AddressablesPlayModeBuildResult));
        }

        #endregion
    }
#endif
}
