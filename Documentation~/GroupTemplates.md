---
uid: group-templates
---

# Create a group template

Create reusable group templates that define which schema objects Unity creates for new groups.

A group template defines which types of schema objects Unity creates for a new group. The Addressables system includes the following default templates:

* **Packed Assets**: Includes all the settings needed to build and load Addressables using the [AssetBundle system](xref:um-asset-bundles).
* **Content Directories**: Includes all the settings needed to build and load Addressables using the [content directory system](xref:um-content-directories).
* **Blank (no schema)**: A group with [no schema](GroupSchemas) attached to it.

## Create a custom group template

To create your own build scripts, define the additional settings in a custom schema object and create a custom group template. To create a group template, perform the following steps:

1. In the **Project** window, navigate to the folder you want to save the new group template to. The default template is in the `AssetGroups` subfolder of `AddressablesAssetsData`.
1. Right-click and select **Create** > **Addressables** > **Group Templates** > **Blank Group Template**.
1. Optionally rename the template and add a description to it.
1. Select the new template, and in the **Inspector** select **Add Schema** to start adding [schema objects](GroupSchemas.md) to the template.

Repeat these steps to add as many new schemas as needed.

> [!NOTE]
> If you use the default build script, a group must use the __Content Directories__ or __Content Packing & Loading__ schema. If you use content update builds, a group must include the __Content Update Restrictions__ schema. For more information, refer to [Builds](Builds.md).

## Additional resources

* [Define group settings](GroupSchemas.md)
* [Content packing settings reference](group-inspector-settings-reference.md)
* [Addressables Asset Settings reference](AddressableAssetSettings.md)