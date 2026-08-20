# Wait for asynchronous loads with async and await

[`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) provides a [`Task`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Task) object that you can use with the C# `async` and `await` keywords to sequence code that calls asynchronous methods and handles the results.

The following example loads Addressable assets using a list of keys. The differences between this task-based approach and the [coroutine](load-wait-asynchronous-coroutines.md) or [event-based approaches](load-wait-asynchronous-events.md) are in the signature of the calling method. This method must include the `async` and `await` keywords with the operation handle's `Task` property. The calling method, `Start` in this case, suspends operation while the task finishes. Execution then resumes and the example instantiates all the loaded prefabs in a grid pattern.

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithTask.cs#doc_LoadWithTask)]

When you use `Task`-based operation handling, you can use the C# `Task` class methods such as [`WhenAll`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.whenall) to control which operations you run in parallel and which you want to run in sequence. The following example illustrates how to wait for more than one operation to finish before moving onto the next task:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithTask.cs#doc_useWhenAll)]

> [!NOTE]
> Awaiting `Task` never throws - it resolves to `default` on most failures, though a `LoadAssetsAsync` call with `releaseDependenciesOnFailure: false` can return a non-null partial result instead. Check [`AsyncOperationHandle.Status`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Status) or [`OperationException`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.OperationException) to detect failure this way.

## Await an operation handle directly

You can also `await` an [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) or [`AsyncOperationHandle<T>`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle`1) directly, without going through `Task`. This is built on Unity's [`Awaitable`](xref:UnityEngine.Awaitable) type and, unlike `Task`, throws an [`AsyncOperationHandleException`](xref:UnityEngine.ResourceManagement.Exceptions.AsyncOperationHandleException) on failure, so a normal `try`/`catch` works:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithAwait.cs#doc_LoadWithAwait)]

A failed operation's handle isn't released automatically - catch the typed [`AsyncOperationHandleException<T>`](xref:UnityEngine.ResourceManagement.Exceptions.AsyncOperationHandleException`1) (or [`AsyncOperationHandleException`](xref:UnityEngine.ResourceManagement.Exceptions.AsyncOperationHandleException) for non-generic handles) and release `e.Handle`, which is exactly the handle that failed. Release it in the `catch` block itself; waiting for a later lifecycle event (`OnDisable`, `OnDestroy`, a cancellation token) leaves it unreleased until then.

This example keeps the instantiated result alive past the call that created it. Releasing the handle in `OnDisable` alone isn't enough: `OnDisable` can run while the load from `OnEnable` is still pending, and releasing the handle there doesn't stop the `await` from resuming later against a disabled object.

[`AsyncOperationHandle.ToAwaitable(CancellationToken)`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle`1.ToAwaitable(System.Threading.CancellationToken)) closes that gap: canceling the token always releases the handle, and also throws `OperationCanceledException` if the load is still pending. If the load already resolved successfully, the cancellation just releases the handle with no throw. `OnDisable` above cancels a `CancellationTokenSource` created fresh each `OnEnable`, so a disable at any point cleans everything up.

> [!NOTE]
> `OnEnable`/`OnDisable` can run many times over a component's life, so the token must be created fresh each `OnEnable` and canceled in the matching `OnDisable`. [`destroyCancellationToken`](xref:UnityEngine.MonoBehaviour.destroyCancellationToken) only cancels on final destruction, so it doesn't fit here - but it's exactly right for a one-shot load, as in the next example.

For a one-shot load started from `Start()`, [`ToAwaitable(MonoBehaviour)`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle`1.ToAwaitable(UnityEngine.MonoBehaviour)) is simpler: it ties cancellation to `destroyCancellationToken` for you, so no cleanup method - not even `OnDestroy` - is needed:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadOnceWithAwait.cs#doc_LoadOnceWithAwait)]

> [!NOTE]
> `AsyncOperationHandleException`'s [`InnerException`](xref:System.Exception.InnerException) is the operation's [`OperationException`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.OperationException). Releasing `e.Handle` matters even more for [`LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*) with `releaseDependenciesOnFailure: false`: it can fail with a partial result (loaded assets alongside `null` entries), reachable through `e.Handle.Result` before you release it:
>
> ```csharp
> try
> {
>     var loaded = await Addressables.LoadAssetsAsync<GameObject>(locations, null, releaseDependenciesOnFailure: false);
> }
> catch (AsyncOperationHandleException<IList<GameObject>> e)
> {
>     // e.Handle.Result is the partial list; e.Handle.Status is Failed.
>     e.Handle.Release();
> }
> ```

When you load multiple assets with [`LoadAssetsAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync*) and don't need to keep them past the call site, you don't need to keep the handle either: [`Addressables.Release`](xref:UnityEngine.AddressableAssets.Addressables.Release*) can look up the handle from the result object it returned, so releasing the awaited result in the same scope is enough. Doing the load, use, and release in one method avoids any window where the object could be disabled before the `await` completes. This only applies on success, though - on failure there's no result to release by, so the `catch` block releases `e.Handle` instead:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadMultipleWithAwait.cs#doc_LoadMultipleWithAwait)]

> [!IMPORTANT]
> `Addressables.Release` finds the handle by looking up the exact object instance the load returned. Copying the result (for example with `.ToList()`) and releasing the copy has no effect and logs an error - always release the same instance the `await` produced.
>
> To keep the loaded assets alive past the method that loaded them, see the two single-asset examples above: a `CancellationTokenSource` scoped to `OnEnable`/`OnDisable` for a repeatable load, or `ToAwaitable(MonoBehaviour)` for a one-shot load.

## Additional resources

* [Asynchronous programming scenarios](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/)
* [Wait for asynchronous loads to complete](AddressableAssetsAsyncOperationHandle.md)
* [Wait for asynchronous loads with coroutines](load-wait-asynchronous-coroutines.md)
* [Wait for asynchronous loads with events](load-wait-asynchronous-events.md)