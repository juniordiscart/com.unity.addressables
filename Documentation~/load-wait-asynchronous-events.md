# Wait for asynchronous loads with events

You can add a delegate function to the [`Completed`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) event of an [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle). The operation calls the delegate function when it's finished.

The following script performs the same function as the example in [Wait for asynchronous loads with coroutines](load-wait-asynchronous-coroutines.md), but uses an event delegate instead of a coroutine:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithEvent.cs#doc_LoadWithEvent)]

The handle instance passed to the event delegate is the same as that returned by the original method call. You can use either to access the results and status of the operation and to release the operation handle and loaded assets.


## Additional resources

* [Wait for asynchronous loads to complete](AddressableAssetsAsyncOperationHandle.md)
* [Wait for asynchronous loads with coroutines](load-wait-asynchronous-coroutines.md)
* [Wait for asynchronous loads with async and await](load-wait-asynchronous-async-await.md)