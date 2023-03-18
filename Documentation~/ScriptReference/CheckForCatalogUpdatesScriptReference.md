---
uid: scriptreference-checkforcatalogupdates
---

# CheckForCatalogUpdates

### Declaration

[!code-cs[sample](../../Tests/Editor/DocExampleCode/ScriptReference/UsingCheckForCatalogUpdates.cs#DECLARATION)]

### Parameters

| Name                      | Description                                                                             |
|:--------------------------|:----------------------------------------------------------------------------------------|
| __autoReleaseHandle__     | If the handle should be released on completion, or manually released. Defaults to true. |

### Returns

__AsyncOperationHandle<List<string>>__ : AsyncOperationHandle that is used to check when the operation has completed. The result of the operation is a list of all catalog ids that have an available update.

### Description

Checks all updatable content catalogs for a new version. Returning a list of catalog ids with available updates.
This can be used to filter which catalogs to update when using UpdateCatalogs.

Note: Catalogs loaded with LoadContentCatalogAsync must be released in order to be updated. See [loading additional catalogs manual] for more information.

Using CheckForCatalogUpdate to print out each catalog with an available update:
[!code-cs[sample](../../Tests/Editor/DocExampleCode/ScriptReference/UsingCheckForCatalogUpdates.cs#SAMPLE)]

[loading additional catalogs manual]: xref:addressables-api-load-content-catalog-asyncloading-additional-catalogs
