# Convert prefabs

To convert a prefab into an Addressable asset, enable the __Addressables__ option in its Inspector window or drag it to a group in the [Addressables Groups](xref:addressables-groups) window.

You don't always need to make prefabs Addressable when used in an Addressable scene. Addressables automatically includes prefabs that you add to the scene hierarchy as part of the data contained in the scene's AssetBundle. If you use a prefab in more than one scene, make the prefab into an Addressable asset so that the prefab data isn't duplicated in each scene that uses it. You must also make a prefab Addressable if you want to load and instantiate it dynamically at runtime.

> [!NOTE]
> If you use a Prefab in a non-Addressable Scene, Unity copies the Prefab data into the built-in Scene data whether the Prefab is Addressable or not.
