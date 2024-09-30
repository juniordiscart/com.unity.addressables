namespace AddressableAssets.DocExampleCode
{
	using System;
    using UnityEngine;

    #region SAMPLE

    using UnityEngine.Networking;
    using UnityEngine.AddressableAssets;
    using System.Text;

    internal class PrivateWebRequestOverride : MonoBehaviour
    {
        [SerializeField]
        private String bucketAccessToken;

        //Register to override WebRequests Addressables creates to download
        private void Start()
        {
            Addressables.WebRequestOverride = AddPrivateToken;
        }

        // Demonstrate adding an Authorization header to access a Cloud Content Delivery private bucket
        private void AddPrivateToken(UnityWebRequest request)
        {
            var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{bucketAccessToken}"));
            request.SetRequestHeader("Authorization", $"Bearer: {encodedToken}");
        }
    }

    #endregion
}
