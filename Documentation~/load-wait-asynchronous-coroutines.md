# Wait for asynchronous loads with coroutines

[`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) implements the [`IEnumerator`](xref:System.Collections.IEnumerator) interface and continues iteration until the operation is complete.

In a [coroutine](xref:um-coroutines), you can yield the operation handle to wait for the next iteration. When complete, the execution flow continues to the following statements. You can implement the [`MonoBehaviour.Start`](xref:MonoBehaviour.Start) method as a coroutine, which is a good way to have a GameObject load and instantiate the assets it needs.

The following script loads a prefab as a child of its GameObject using a `Start` method in a coroutine. It yields the `AsyncOperationHandle` until the operation finishes and then uses the same handle to instantiate the prefab.

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithIEnumerator.cs#doc_LoadWithIEnumerator)]

You can't cancel [`Addressables.LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*) once started. However, releasing the handle before it has finished decrements the handle reference count and automatically releases it when the load is complete.

For more information, refer to [Write and run coroutines](xref:um-coroutines).

## Group operations in a coroutine

To perform several operations before moving on to the next step in your game logic, such as loading prefabs and other assets before you start a level, you can combine them with a single call to the [`Addressables.LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*) method, if all the operations load assets.

The `AsyncOperationHandle` for this method works the same as [`LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*). You can yield the handle in a coroutine to wait until all the assets in the operation load. You can also pass a callback method to `LoadAssetsAsync` and the operation calls that method when it finishes loading a specific asset. Refer to [Loading multiple assets](load-assets.md#load-multiple-assets) for an example.

You can also use the [`ResourceManager.CreateGenericGroupOperation`](xref:UnityEngine.ResourceManagement.ResourceManager.CreateGenericGroupOperation*) to create a group operation that completes when all its members finish.

## Additional resources

* [Wait for asynchronous loads with events](load-wait-asynchronous-events.md)
* [Wait for asynchronous loads with async and await](load-wait-asynchronous-async-await.md)
* [Write and run coroutines](xref:um-coroutines)