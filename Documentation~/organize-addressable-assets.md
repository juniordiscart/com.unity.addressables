# Organize Addressable assets

The best way to organize your assets depends on the specific requirements of each project. Aspects to consider when planning how to manage your assets in a project include:

* **Logical organization**: Keep assets in logical categories to make it easier to understand your organization and discover items that are out of place.
* **Runtime performance**: Performance bottlenecks can happen if your AssetBundles grow in size, or if you have many AssetBundles.
* **Runtime memory management**: Keep assets together that you use together to help lower peak memory requirements.
* **Scale**: Some ways of organizing assets might work well in small games, but not large ones, and vice versa.
* **Platform characteristics**: The characteristics and requirements of a platform can be a large consideration in how to organize your assets. Some examples:
  * Platforms that offer abundant virtual memory can handle large bundle sizes better than those with limited virtual memory.
  * Some platforms don't support downloading content, ruling out remote distribution of assets entirely.
  * Some platforms don't support AssetBundle caching, so putting assets in local bundles, when possible, is more efficient.
* **Distribution**: Whether you distribute your content remotely or not means that you must separate your remote content from your local content.
* **How often assets are updated**: Keep assets that you expect to update often separate from those that you plan to rarely update.
* **Version control**: The more people who work on the same assets and asset groups, the greater the chance for version control conflicts to happen in a project.

## Common strategies

Typical strategies for organizing assets include:

* **Concurrent usage**: Group assets that you load at the same time together, such as all the assets for a given level. This strategy is often the most effective in the long term and can help reduce peak memory use in a project.
* **Logical entity**: Group assets belonging to the same logical entity together. For example, UI layout assets, textures, sound effects. Or character models and animations.
* **Type**: Group assets of the same type together. For example, music files, textures.

Depending on the needs of your project, one of these strategies might make more sense than the others. For example, in a game with many levels, organizing according to concurrent usage might be the most efficient both from a project management and from a runtime memory performance standpoint.

At the same time, you might use different strategies for different types of assets. For example, your UI assets for menu screens might all be grouped together in a level-based game that otherwise groups its level data separately. You might also pack a group that has the assets for a level into bundles that contain a particular asset type.

Refer to [Preparing Assets for AssetBundles](xref:AssetBundles-Preparing) for additional information.

## Safely edit loaded assets

You can safely edit loaded Assets in the following situations:

* The Asset is loaded from an AssetBundle.
* The application is running in a Player, not in the Editor.
* When you enable the **Use Existing Build (requires built groups)** option in [Play Mode Scripts](xref:addressables-groups-window).

In these cases, the assets exist as a copy in active memory. Changes made to these copied assets don't affect the saved AssetBundle on disk and any changes don't persist between sessions.

For other situations, including when you enable the **Use Asset Database (fastest)** property in the Play mode settings, Unity loads the Assets directly from the Project files. This means that Unity saves any modifications to the Asset during runtime to the Project Asset file and that those changes will persist between different sessions.

If you want to make runtime changes to an asset, create a new instance of the GameObject you want to change and use the copy for any runtime changes. This eliminates the risk that you might accidentally change the original asset file. The following code example demonstrates creating a new copy of a loaded asset:

```c#
var op = Addressables.LoadAssetAsync<GameObject>("myKey");
yield return op;
if (op.Result != null)
{
    GameObject inst = UnityEngine.Object.Instantiate(op.Result);
    // can now use and safely make edits to inst, without the source Project Asset being changed.
}
```

If you use this example method to use a copy of an asset, be aware of the following:
* You must use either the original asset or the `AsyncOperationHandle` when you release the asset, not the current instance of the asset.
* When you instantiate an asset that has references to other assets in this way, Unity doesn't create new instances of the referenced assets. The references for the newly instantiated copy target the original project asset.
* Unity invokes `MonoBehaviour` methods like `Start()`, `OnEnable()`, and `OnDisable()` on the new instance.
