namespace AddressableAssets.DocExampleCode
{
    #region doc_LoadMultipleWithAwait

    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.Exceptions;

    internal class LoadMultipleWithAwait : MonoBehaviour
    {
        public string label = "characters";

        // Load, use, and release in one self-contained scope - no lifecycle callback can run
        // before the await completes, so there's no window for a disable to leave it unreleased.
        // To keep assets past this method, store the handle instead - see LoadWithAwait.
        async void Start()
        {
            IList<GameObject> loaded = null;
            try
            {
                loaded = await Addressables.LoadAssetsAsync<GameObject>(label);
                foreach (var prefab in loaded)
                    Debug.Log($"Loaded '{prefab.name}' for label '{label}'.");
            }
            catch (AsyncOperationHandleException<IList<GameObject>> e)
            {
                // The awaited handle releases itself on failure - only e.Handle needs releasing.
                Debug.LogError($"Failed to load label '{label}': {e.Message}");
                e.Handle.Release();
            }
            finally
            {
                // Addressables.Release(obj) looks up the handle by the exact result object
                // returned, so releasing it is enough - releasing a copy (e.g. via .ToList())
                // instead logs an error and leaks the real handle.
                if (loaded != null)
                    Addressables.Release(loaded);
            }
        }
    }

    #endregion
}
