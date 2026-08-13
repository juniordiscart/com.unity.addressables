# Create a custom wait operation

To create a custom operation, extend the [`AsyncOperationBase`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1) class and override its virtual methods.

You can pass the derived operation to the [`ResourceManager.StartOperation`](xref:UnityEngine.ResourceManagement.ResourceManager.StartOperation*) method to start the operation and receive an [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) struct. The [`ResourceManager`](xref:UnityEngine.ResourceManagement.ResourceManager) registers operations started this way.

## Execute a custom operation

The [`ResourceManager`](xref:UnityEngine.ResourceManagement.ResourceManager) invokes the [`AsyncOperationBase.Execute`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1.Execute*) method for the custom operation once the optional dependent operation completes.

## Completion handling

When the custom operation completes, call [`AsyncOperationBase.Complete`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1.Complete*) on the custom operation object. You can call this in the [`Execute`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1.Execute*) method or defer it to outside the call. `AsyncOperationBase.Complete` notifies the `ResourceManager` that the operation has finished. `The ResourceManager` invokes the associated [`AsyncOperationHandle.Completed`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle.Completed) events for the relevant instances of the custom operation.

## Terminate the custom operation

`ResourceManager` invokes the [`AsyncOperationBase.Destroy`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1.Destroy*) method for your custom operation when the operation [`AsyncOperationBase.ReferenceCount`]( xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1.DecrementReferenceCount*) reaches zero. `AsyncOperationBase.ReferenceCount` is decreased when the [`AsyncOperationHandle`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle) that references it is released using [`Addressables.Release`](xref:UnityEngine.AddressableAssets.Addressables.Release*) or when [`AsyncOperationBase.DecrementReferenceCount`](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1.DecrementReferenceCount*) is called by a custom operation internally. `AsyncOperationBase.Destroy` is where you should release any memory or resources associated with your custom operation.

## Additional resources

* [`AsyncOperationBase` API reference](xref:UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationBase`1)
* [Monitor wait operations](load-monitor-wait-operations.md)