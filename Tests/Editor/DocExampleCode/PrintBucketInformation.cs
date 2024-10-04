namespace AddressableAssets.DocExampleCode
{
    #region SAMPLE
#if (UNITY_EDITOR && ENABLE_CCD)
    using System.Threading.Tasks;
    using UnityEditor.AddressableAssets.Build;

    public class BuildHooks
    {

        static void AddBuildHook()
        {
            CcdBuildEvents.PrependPreBuildEvent(PrintBucketInformation);
            CcdBuildEvents.PrependPreUpdateEvent(PrintBucketInformation);
        }

        static async Task<bool> PrintBucketInformation(AddressablesDataBuilderInput input)
        {
            UnityEngine.Debug.Log($"Environment: {CcdManager.EnvironmentName}");
            UnityEngine.Debug.Log($"Bucket: {CcdManager.BucketId}");
            UnityEngine.Debug.Log($"Badge: {CcdManager.Badge}");
            return true;
        }
    }
#endif
    #endregion
}
