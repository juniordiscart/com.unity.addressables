---
uid: addressables-modification-events
---

# Modification events

You can use modification events to signal to parts of the Addressables system when certain data is manipulated, such as when an `AddressableAssetGroup` or an `AddressableAssetEntry` is added or removed.

Modification events are triggered as part of `SetDirty` calls inside of Addressables. `SetDirty` is used to indicate when an asset needs to be re-serialized by the `AssetDatabase`.  As part of `SetDirty`, two modification event callbacks can trigger: 

* `public static event Action<AddressableAssetSettings, ModificationEvent, object> OnModificationGlobal`
* `public Action<AddressableAssetSettings, ModificationEvent, object> OnModification { get; set; }`

These callbacks are found on [`AddressableAssetSettings`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings) through a static, or instance, accessors respectively.

## Modification event example

```c#
AddressableAssetSettings.OnModificationGlobal += (settings, modificationEvent, data) =>
        {
            if(modificationEvent == AddressableAssetSettings.ModificationEvent.EntryAdded)
            {
                //Do work
            }
        };

        AddressableAssetSettingsDefaultObject.Settings.OnModification += (settings, modificationEvent, data) =>
        {
            if (modificationEvent == AddressableAssetSettings.ModificationEvent.EntryAdded)
            {
                //Do work
            }
        };
```
Modification events pass in a generic `object` for the data associated with the event. The following table outlines a list of the modification events and the data types that are passed with them.

|**Modification event**|**Data passed**|
|---|---|
|GroupAdded|[`AddressableAssetGroup`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroup), or list of groups that were added.|
|GroupRemoved| [`AddressableAssetGroup`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroup), or list of groups that were removed.|
|GroupRenamed| [`AddressableAssetGroup`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroup), or list of groups that were renamed.|
|GroupSchemaAdded| [`AddressableAssetGroup`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroup), or list of groups that had schemas added to them.|
|GroupSchemaRemoved|[`AddressableAssetGroup`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroup), or list of groups that had schemas removed from them.|
|GroupSchemaModified| [`AddressableAssetGroupSchema`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroupSchema) that was modified.|
|GroupTemplateAdded| `ScriptableObject`, typically one that implements [`IGroupTemplate`](xref:UnityEditor.AddressableAssets.Settings.IGroupTemplate), that was the added Group Template object.|
|GroupTemplateRemoved|`ScriptableObject`, typically one that implements [`IGroupTemplate`](xref:UnityEditor.AddressableAssets.Settings.IGroupTemplate), that was the removed Group Template object.|
|GroupTemplateSchemaAdded| [`AddressableAssetGroupTemplate`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroupTemplate) that had a schema added.|
|GroupTemplateSchemaRemoved|[`AddressableAssetGroupTemplate`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroupTemplate) that had a schema removed.|
|EntryCreated|[`AddressableAssetEntry`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetEntry) that was created.|
|EntryAdded|[`AddressableAssetEntry`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetEntry), or list of entries that were added.|
|EntryMoved|[`AddressableAssetEntry`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetEntry), or list of entries that were moved from one group to another.|
|EntryRemoved|[`AddressableAssetEntry`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetEntry), or list of entries that were removed.|
|LabelAdded|`string` label that was added.|
|LabelRemoved|`string` label that was removed.|
|ProfileAdded|`BuildProfile` that was added.|
|ProfileRemoved|`string` of the profile ID that was removed.|
|ProfileModified|`BuildProfile` that was modified, or `null` if a batch of `BuildProfile` objects were modified.|
|ActiveProfileSet|The data passed with this event if the `string` of the profile ID that is set as the active profile.|
|EntryModified|[`AddressableAssetEntry`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetEntry), or list of entries that were modified.|
|BuildSettingsChanged|[`AddressableAssetBuildSettings`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetBuildSettings) object that was modified.|
|ActiveBuildScriptChanged|[`IDataBuilder`](xref:UnityEditor.AddressableAssets.Build.IDataBuilder) build script that was set as the active builder.|
|DataBuilderAdded|`ScriptableObject`, typically one that implements [`IDataBuilder`](xref:UnityEditor.AddressableAssets.Build.IDataBuilder), that was added to the list of DataBuilders.|
|DataBuilderRemoved|`ScriptableObject`, typically one that implements [`IDataBuilder`](xref:UnityEditor.AddressableAssets.Build.IDataBuilder), that was removed from the list of DataBuilders.|
|InitializationObjectAdded|`ScriptableObject`, typically one that implements [`IObjectInitializationDataProvider`](xref:UnityEngine.ResourceManagement.Util.IObjectInitializationDataProvider), that was added to the list of InitializationObjects.|
|InitializationObjectRemoved|`ScriptableObject`, typically one that implements [`IObjectInitializationDataProvider`](xref:UnityEngine.ResourceManagement.Util.IObjectInitializationDataProvider), that was removed from the list of InitializationObjects.|
|ActivePlayModeScriptChanged|[`IDataBuilder`](xref:UnityEditor.AddressableAssets.Build.IDataBuilder) that was set as the new active Play mode data builder.|
|BatchModification|`null`.  This event is primarily used to indicate several modification events happening at the same time and the [`AddressableAssetSettings`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetSettings) object needed to be marked dirty.|
|HostingServicesManagerModified| [`HostingServicesManager`](xref:UnityEditor.AddressableAssets.HostingServices.HostingServicesManager), or [`HttpHostingService`](xref:UnityEditor.AddressableAssets.HostingServices.HttpHostingService) that were modified.|
|GroupMoved|Full list of [`AddressableAssetGroups`](xref:UnityEditor.AddressableAssets.Settings.AddressableAssetGroup).|
|CertificateHandlerChanged|New `System.Type` of the certificate handler to be used.|