---
uid: addressables-packing-groups
---

# Pack groups into AssetBundles

You have three options when you choose how to pack the assets in a group into AssetBundles:

* You can pack all Addressables assigned to a group together in a single bundle. This corresponds to the **Pack Together** bundle mode. 
* You can pack each Addressable assigned to a group separately in its own bundle. This corresponds to the **Pack Separately** bundle mode. 
* You can pack all Addressables sharing the same set of labels into their own bundles. This corresponds to the **Pack Together By Label** bundle mode.

For more information on bundle modes, refer to [Advanced Group Settings](xref:addressables-content-packing-and-loading-schema).

Scene assets are always packed separately from other Addressable assets in the group. For this reason, a group containing a mix of scene and non-scene assets always produces at least two bundles when built: one for scenes and one for everything else.

When you choose to pack each Addressable asset separately, Unity treats compound assets (such as sprite sheets) and assets in folders marked as Addressables differently:

* All the assets in a folder that are marked as Addressable are packed together in the same folder (except for assets in the folder that are individually marked as Addressable themselves). 
* Sprites in an Addressable Sprite Atlas are included in the same bundle.

Refer to [Content Packing & Loading settings](xref:addressables-content-packing-and-loading-schema) for more information.

> [!NOTE]
> Keeping many assets in the same group can increase the chance of version control conflicts when many people work on the same project.

