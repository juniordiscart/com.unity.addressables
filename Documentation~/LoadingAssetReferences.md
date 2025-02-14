---
uid: addressables-loading-asset-reference
---

# Load an AssetReference

The [`AssetReference`](xref:UnityEngine.AddressableAssets.AssetReference) class has its own load method, [`LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*):

[!code-cs[sample](../Tests/Editor/DocExampleCode/LoadReference.cs#doc_Load)]

You can also use the `AssetReference` object as a key to the [`Addressables.LoadAssetAsync`](xref:UnityEngine.AddressableAssets.Addressables.LoadAssetAsync*) methods. If you need to spawn multiple instances of the asset assigned to an AssetReference, use `Addressables.LoadAssetAsync`, which gives you an operation handle that you can use to release each instance.
