namespace AddressableAssets.DocExampleCode
{
	using System;
    using System.Collections.Generic;
    using Unity.IO.LowLevel.Unsafe;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceProviders;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;

    internal class UsingUnloadScene
    {
        #region SAMPLE
        // The idea with this sample is to show a simple streaming world. This has 3 events
        // that work to load and unload scenes as required. Load would be triggered when
        // the player gets close to the entrypoint. Activate would be called by doing
        // something like "opening a door". And then unload could be called when the player
        // crosses the entrypoint again in the other direction.
        public class SceneGatewayManager {

            // the Addressables key for the scene to load
            [SerializeField]
            public string sceneKey;

            private AsyncOperationHandle<SceneInstance> sceneHandle;

            public void EnterBoundary()
            {
                if (sceneHandle.IsValid() && sceneHandle.IsDone)
                {
                    // do not reload if they have already passed the load boundary
                    return;
                }

                // load, but do not activate
                var activateOnLoad = false;

                // the scene is additive to keep our base scene and simply add a new area to it
                sceneHandle = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Additive, activateOnLoad);
            }

            public IEnumerator<AsyncOperationHandle<SceneInstance>> EnterScene()
            {
                // at this point we have to finish waiting for the scene to load if it
                // hasn't already
                yield return sceneHandle;

                // this will activate the scene
                sceneHandle.Result.ActivateAsync();
            }

            // exist boundary takes an optional cleanup callback that could be
            // used to cleanup Assets after scene unload
            public void ExitBoundary(Action cleanupCallback = null)
            {

                if (!sceneHandle.Result.Scene.isLoaded)
                {
                    // scene has not been activated and cannot be unloaded
                    return;
                }
                var unloadHandle = Addressables.UnloadSceneAsync(sceneHandle, UnloadSceneOptions.None);
                unloadHandle.Completed += (s) =>
                {                    
                    if (cleanupCallback != null)
                    {
                        cleanupCallback();
                    }
                };
            }


        }
		
		#endregion
	}
}
