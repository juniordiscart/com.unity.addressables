---
uid: addressables-instantiating
---

<a name="instantiate"></a>
## Instantiating objects from Addressables

You can load an asset, such as a Prefab, and then create an instance of it with [Instantiate]. You can also load and create an instance of an asset with [Addressables.InstantiateAsync]. The main difference between these two ways of instantiating objects is how the asset reference counts are affected.

When you use InstantiateAsync, Unity increments the reference counts of the loaded assets each time you call the method. For example, if you instantiate a Prefab five times, Unity increments the reference count for the Prefab asset and any of its dependencies by five. You can then release each instance separately as they are destroyed in the application.

When you use LoadAssetAsync and Object.Instantiate, Unity only increments the asset reference counts once, during the initial load. If you release the loaded asset (or its operation handle) and the reference count decrements to zero, then Unity unloads the asset. At this point, all the additional instantiated copies lose their subassets. The copies still exist as GameObjects in the scene, but without Meshes, Materials, or other assets that they might depend on.

Both ways are useful and you should choose based on how you organize your object code. For example, if you have a single manager object that supplies a pool of Prefab enemies to spawn into a game level, it might be most convenient to release them all at the completion of the level with a single operation handle stored in the manager class. In other situations, you might want to use InstantiateAsync to load and release assets individually.  

The following example calls [InstantiateAsync] to instantiate a Prefab. The example adds a component to the instantiated GameObject that releases the asset when the GameObject is destroyed.

[!code-cs[sample](../../Tests/Editor/DocExampleCode/InstantiateAsset.cs#doc_Instantiate)]

<!--
```csharp
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class InstantiateFromKey : MonoBehaviour
{
  public string key; // Identify the asset

  void Start() {
    // Load and instantiate
    Addressables.InstantiateAsync(key).Completed += instantiate_Completed;
  }

  private void instantiate_Completed(AsyncOperationHandle<GameObject> obj) {
    // Add component to release asset in GameObject OnDestroy event
    obj.Result.AddComponent(typeof(SelfCleanup));
    Destroy(obj.Result, 12); // Destroy to trigger release
  }
}

// Releases asset (trackHandle must be true in InstantiateAsync)
public class SelfCleanup : MonoBehaviour
{
  void OnDestroy() {
    Addressables.ReleaseInstance(gameObject);
  }
}
```
-->

When you call [InstantiateAsync] you have the same options as the [Object.Instantiate] method, and also the following additional parameters:

* __instantiationParameters__: this parameter takes a [InstantiationParameters] struct that you can use to specify the instantiation options instead of specifying them in every call to the InstantiateAsync call. This can be convenient if you use the same values for multiple instantiations.
* __trackHandle__:  If true, which is the default, then the Addressables system keeps track of the operation handle for the instantiated instance. This allows you to release the asset with the [Addressables.ReleaseInstance] method. If false, then the operation handle is not tracked for you and you must store a reference to the handle returned by InstantiateAsync in order to release the instance when you destroy it.