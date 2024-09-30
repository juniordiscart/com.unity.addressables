# Asynchronous loading

The Addressables system API is asynchronous and returns an [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) to use to manage operation progress and completion.

Addressables is designed to be content location agnostic. The content might need to be downloaded first or use other methods that can take a long time. To force synchronous execution, refer to [Synchronous Addressables](xref:synchronous-addressables) for more information.

When loading an asset for the first time, the handle is complete after a minimum of one frame. If the content has already loaded, execution times might differ between the various asynchronous loading options shown below. You can wait until the load has completed as follows:

* [Coroutine](xref:UnityEngine.Coroutine): Always delayed at a minimum of one frame before execution continues.
* [`Completed` callback](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed): A minimum of one frame if the content hasn't already loaded, otherwise the callback is invoked in the same frame.
* Awaiting [`AsyncOperationHandle.Task`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Task): A minimum of one frame if the content hasn't already loaded, otherwise the execution continues in the same frame.

[!code-cs[sample](../Tests/Editor/DocExampleCode/AsynchronousLoading.cs#doc_asyncload)]
