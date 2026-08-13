---
uid: addressables-loading-asset-reference
---

# Load asset references

The [`AssetReference`](xref:UnityEngine.AddressableAssets.AssetReference) class has its own load method, [`LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*). The following is an example of loading an asset assigned to an `AssetReference` field in the Inspector:

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadReference.cs#doc_Load)]

You can also use the `AssetReference` object as a key to the [`Addressables.LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*) methods. If you need to spawn multiple instances of the asset assigned to an AssetReference, use [`Addressables.InstantiateAsync`](xref:UnityEngine.AddressableAssets.AssetReference.InstantiateAsync*), which gives you an operation handle that you can use to release each instance.

## Additional resources

* [`LoadAssetAsync` API reference](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*)
* [Create an asset reference field](asset-reference-create.md)