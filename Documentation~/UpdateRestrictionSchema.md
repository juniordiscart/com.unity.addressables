---
uid: addressables-update-restriction-schema
---

# Content Update Restriction schema reference

The Content Update Restriction schema determine how the [Check for Content Update Restrictions](xref:addressables-content-update-builds) tool treats assets in the group. To prepare your groups for a differential content update build rather than a full content build, on the [Addressables Groups window](xref:addressables-groups-window), go to **Tools** and run the **Check for Content Update Restrictions** command. The tool moves modified assets in any groups with the __Prevent Updates__ property enabled, to a new group.

The **Prevent Updates** property acts in the following way:

* **Enabled**: The tool doesn't move any assets. When you make the update build, if any assets in the bundle have changed, then the entire bundle is rebuilt.
* **Disabled**: If any assets in the bundle have changed, then the [Check for Content Update Restrictions](xref:addressables-content-update-builds) tool moves them to a new group created for the update. When you make the update build, the assets in the AssetBundles created from this new group override the versions found in the existing bundles.

See [Content update builds](xref:addressables-content-update-builds) for more information.
