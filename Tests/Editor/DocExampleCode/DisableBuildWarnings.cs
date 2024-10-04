namespace AddressableAssets.DocExampleCode
{
    #region SAMPLE
#if (UNITY_EDITOR && ENABLE_CCD)
using UnityEditor;
using UnityEditor.AddressableAssets.Build;

public class DisableBuildWarning
{
    static void DisableWarning()
    {
        CcdBuildEvents.OnPreBuildEvents -= CcdBuildEvents.Instance.VerifyBuildVersion;
        CcdBuildEvents.OnPreUpdateEvents -= CcdBuildEvents.Instance.VerifyBuildVersion;
    }
}
#endif
    #endregion
}
