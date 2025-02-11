---
uid: addressables-get-address
---

# Get addresses at runtime

By default, Addressables uses the address you assign to an asset as the [`PrimaryKey`](xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation.PrimaryKey) value of its [`IResourceLocation`](xref:UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation) instance.

If you disable the [Include Addresses in Catalog](xref:addressables-content-packing-and-loading-schema) option of the Addressables group to which the asset belongs, the `PrimaryKey` can be a GUID, label, or an empty string. If you want to get the address of an asset that you load with an `AssetReference` or label, you can load the asset's locations, as described in [Load assets by location](xref:addressables-api-load-asset-async) documentation. You can then use the `IResourceLocation` instance to both access the `PrimaryKey` value and to load the asset.

The following example gets the address of the asset assigned to an [`AssetReference`](xref:UnityEngine.AddressableAssets.AssetReference) object named `MyRef1`:

[!code-cs[sample](../Tests/Editor/DocExampleCode/MiscellaneousTopics.cs#doc_AddressFromReference)]

Labels often refer to multiple assets. The following example illustrates how to load multiple prefab assets and use their primary key value to add them to a dictionary:

[!code-cs[sample](../Tests/Editor/DocExampleCode/MiscellaneousTopics.cs#doc_PreloadHazards)]
