---
uid: addressables-group-schemas
---

# Group settings and schemas overview

Group settings set how Unity treats the assets in a group in content builds. Group settings control properties such as the location where Unity builds AssetBundles or bundle compression settings.

To open a group's settings, open the [Addressables Groups window](GroupsWindow.md) (**Window &gt; Asset Management &gt; Addressables &gt; Groups**), then select a group. The group's settings are displayed in the Inspector.

A group's settings are declared in Schema objects attached to the group. When you create a group with the [Packed Assets template](xref:group-templates), the Content Packing & Loading and Content Update Restriction schemas define the settings for the group. The default [Build scripts](xref:addressables-builds) expect these settings.

![The Inspector window for the Default Local Group.](images/groups-group-settings.png)<br/>*The Inspector window for the Default Local Group.*

> [!NOTE]
> If you create a group with the Blank template, then Unity doesn't attach any schemas to the group. The default build script can't process assets in a blank group.

## Schemas

A group schema is a `ScriptableObject` that defines a collection of settings for an Addressables group. You can assign any number of schemas to a group. The Addressables system defines a number of schemas for its own purposes. You can also create custom schemas to support your own build scripts and utilities.

The built-in schemas include:

* __Content Packing & Loading__: The main Addressables schema used by the default build script and defines the settings for building and loading Addressable assets. For information on the settings for this schema, refer to [Content Packing & Loading schema reference](ContentPackingAndLoadingSchema.md).
* __Content Update Restrictions__: Defines settings for making differential updates of an earlier build. For more information about this schema refer to [Content Update Restriction schema reference](UpdateRestrictionSchema.md).

## Create a custom schema

To create your own schema, extend the [`AddressableAssetGroupSchema`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroupSchema) class, which is a kind of `ScriptableObject`:

```csharp
using UnityEditor.AddressableAssets.Settings;

public class __CustomSchema __: AddressableAssetGroupSchema
{
   public string CustomDescription;
}
```

Once you've defined a custom schema object, you can add it to existing groups and group templates using the Add Schema buttons in the Inspector windows of those entities.

You might also want to create a custom Unity Editor script to help users interact with your custom settings. For more information, refer to [Custom Inspector scripts](xref:VariablesAndTheInspector).

In a build script, you can access the schema settings for a group using its [`AddressableAssetGroup`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroup) object.

## Additional resources

* [Content Packing & Loading schema reference](ContentPackingAndLoadingSchema.md).
