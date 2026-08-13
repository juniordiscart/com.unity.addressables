# Convert prefabs to use Addressables

To convert a prefab into an Addressable asset, enable the __Addressables__ option in its Inspector window or drag it to a group in the [Addressables Groups](GroupsWindow.md) window.

To enable the **Addressables** option:

1. Select the prefab in the **Project** window.
1. In its Inspector, enable the **Addressables** option.

You don't always need to make prefabs Addressable when used in an Addressable scene. Addressables automatically includes prefabs that you add to the scene hierarchy as part of the data contained in the scene's AssetBundle.

However, if you use a prefab in more than one scene and don't want to duplicate it across scenes, you can make it Addressable, remove it from the scene, and then use the `Addressables` API or [`AssetReference`](AssetReferences.md) to load it dynamically. You must also make a prefab Addressable if you want to load and instantiate it dynamically at runtime.

> [!NOTE]
> If you use a prefab in a non-Addressable scene, Unity copies the prefab data into the built-in scene data whether the prefab is Addressable or not.

## Additional resources

* [Referencing Addressable assets in code](AssetReferences.md)
* [Organize assets into groups](groups-intro.md)
