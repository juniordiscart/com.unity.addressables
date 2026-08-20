namespace AddressableAssets.DocExampleCode
{
    #region doc_LoadWithAwait

    using System;
    using System.Threading;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.Exceptions;

    internal class LoadWithAwait : MonoBehaviour
    {
        public string address;

        GameObject m_Instance;
        CancellationTokenSource m_Cts;

        async void OnEnable()
        {
            // A fresh token each OnEnable: OnDisable below cancels it, so a load or instantiate
            // still in flight when the component is disabled is stopped and its handle released
            // automatically - unlike destroyCancellationToken, this also reacts to a mere disable,
            // not just final destruction.
            m_Cts = new CancellationTokenSource();

            try
            {
                // Unlike handle.Task (resolves to null on failure, never throws), awaiting the
                // handle throws AsyncOperationHandleException on failure. A cancellation only
                // throws OperationCanceledException if OnDisable runs before the load finishes.
                m_Instance = await Addressables.InstantiateAsync(address, transform).ToAwaitable(m_Cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Only runs if OnDisable fires before the load finishes; a cancel after success
                // just releases the handle without throwing.
            }
            catch (AsyncOperationHandleException<GameObject> e)
            {
                // Release immediately rather than waiting for OnDisable to eventually cancel
                // m_Cts: the failed handle stays valid (and unreleased) until then, and the
                // component could stay enabled indefinitely after a failed load.
                Debug.LogError($"Failed to load '{address}': {e.Message}");
                e.Handle.Release();
            }
        }

        void OnDisable()
        {
            // Cancels the pending await (if the instantiate hasn't finished yet) and releases the
            // handle - whether it's still pending or already completed - so there is no separate
            // cleanup call needed for either case.
            m_Cts.Cancel();
            m_Cts.Dispose();
        }
    }

    #endregion
}
