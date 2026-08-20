namespace AddressableAssets.DocExampleCode
{
    #region doc_LoadOnceWithAwait

    using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.Exceptions;

    internal class LoadOnceWithAwait : MonoBehaviour
    {
        public string address;

        GameObject m_Instance;

        async void Start()
        {
            try
            {
                // ToAwaitable(MonoBehaviour) ties cancellation to this object's destruction
                // (MonoBehaviour.destroyCancellationToken), so no separate OnDestroy release is
                // needed: whether this object is destroyed before or after the instantiate
                // finishes, the handle is released automatically.
                m_Instance = await Addressables.InstantiateAsync(address, transform).ToAwaitable(this);
            }
            catch (OperationCanceledException)
            {
                // Only runs if destruction happens before the load finishes; a cancel after
                // success just releases the handle without throwing.
            }
            catch (AsyncOperationHandleException<GameObject> e)
            {
                // Release immediately rather than waiting for destroyCancellationToken to
                // eventually fire: the failed handle stays valid (and unreleased) until this
                // object is actually destroyed, which could be arbitrarily far in the future.
                Debug.LogError($"Failed to load '{address}': {e.Message}");
                e.Handle.Release();
            }
        }
    }

    #endregion
}
