# Safely edit a loaded asset

You can safely edit a loaded Asset in the following situations:

* The Asset is loaded from an AssetBundle.
* The application is running in a Player, not in the Editor.
* When you enable the **Use Existing Build (requires built groups)** option in [Play mode scripts](xref:addressables-groups-window).

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
