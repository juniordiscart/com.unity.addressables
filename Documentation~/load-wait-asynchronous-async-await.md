# Wait for asynchronous loads with async and await

[`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) provides a [`Task`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Task) object that you can use with the C# `async` and `await` keywords to sequence code that calls asynchronous methods and handles the results.

The following example loads Addressable assets using a list of keys. The differences between this task-based approach and the [coroutine](load-wait-asynchronous-coroutines.md) or [event-based approaches](load-wait-asynchronous-events.md) are in the signature of the calling method. This method must include the `async` and `await` keywords with the operation handle's `Task` property. The calling method, `Start` in this case, suspends operation while the task finishes. Execution then resumes and the example instantiates all the loaded prefabs in a grid pattern.

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithTask.cs#doc_LoadWithTask)]

When you use `Task`-based operation handling, you can use the C# `Task` class methods such as [`WhenAll`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.whenall) to control which operations you run in parallel and which you want to run in sequence. The following example illustrates how to wait for more than one operation to finish before moving onto the next task:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithTask.cs#doc_useWhenAll)]

## Additional resources

* [Asynchronous programming scenarios](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/)
* [Wait for asynchronous loads to complete](AddressableAssetsAsyncOperationHandle.md)
* [Wait for asynchronous loads with coroutines](load-wait-asynchronous-coroutines.md)
* [Wait for asynchronous loads with events](load-wait-asynchronous-events.md)