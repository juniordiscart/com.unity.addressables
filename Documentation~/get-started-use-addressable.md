# Use an Addressable asset

To load and use an Addressable asset, you can:

* [Use an AssetReference that references the asset](#use-assetreferences)
* [Use its address string](#load-by-address)
* [Use a label assigned to the asset](#load-by-address)

Refer to [Loading assets](xref:addressables-api-load-asset-async) for more detailed information about loading Addressable assets.

Loading Addressable assets uses asynchronous operations. Refer to [Operations](xref:addressables-async-operation-handling) for information about the different ways to approach asynchronous programming in Unity scripts.

> [!TIP]
> You can find more involved examples of how to use Addressable assets in the [Addressables Sample repository](https://github.com/Unity-Technologies/Addressables-Sample).

## Use AssetReferences

To use an `AssetReference`, add an `AssetReference` field to a `MonoBehaviour` or `ScriptableObject`. After you create an object of that type, you can assign an asset to the field in your object's Inspector window. 

> [!NOTE]
> If you assign a non-Addressable asset to an AssetReference field, Unity automatically makes that asset Addressable and adds it to your default Addressables group. AssetReferences also let you use Addressable assets in a Scene that isn't itself Addressable.

Unity doesn't load or release the referenced asset automatically; you must load and release the asset using the `Addressables` API:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithReference.cs#doc_LoadWithReference)]

Refer to [Loading an AssetReference](xref:addressables-loading-asset-reference) for additional information about loading AssetReferences.

## Load by address

You can use the address string to load an asset:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithAddress.cs#doc_LoadWithAddress)]


Remember that every time you load an asset, you must also release it.

Refer to [Loading a single asset](load-assets.md#load-a-single-asset) for more information.

## Load by label

You can load sets of assets that have the same label in one operation:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadWithLabels.cs#doc_LoadWithLabels)]

See [Loading multiple assets](load-assets.md#load-multiple-assets) for more information.
