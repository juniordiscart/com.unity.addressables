# Create custom rules for Addressables Analyze

You can create custom rules for the [**Addressables Analyze** window](analyze-addressables-window-reference.md) with the [`AnalyzeRule`](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.AnalyzeRule) class.

## Create a custom rule

To create a custom rule, create a new child class of the [`AnalyzeRule`](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.AnalyzeRule) class, and override the following properties:

* [`CanFix`](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.AnalyzeRule.CanFix): Set whether the rule is fixable or not.
* [`ruleName`](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.AnalyzeRule.ruleName): Set the display name of the rule, which is displayed in the **Addressables Analyze** window.

You'll also need to override the following methods:

### RefreshAnalysis

[`RefreshAnalysis`](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.AnalyzeRule.RefreshAnalysis*) is the analyze operation. In this method, perform any calculations and cache any data you might need for a potential fix. The return value is a `List<AnalyzeResult>` list. Create a new [`AnalyzeResult`](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.AnalyzeRule.AnalyzeResult) for each entry in your analysis, containing the data as a string for the first parameter and a [message type](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.AnalyzeRule.AnalyzeResult.severity) for the second (to optionally designate the message type as a warning or error). Return the list of objects you create.

If you need to make child elements in the `TreeView` for a particular `AnalyzeResult` object, you can delineate the parent item and any children with [`kDelimiter`](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.AnalyzeRule.kDelimiter). Include the delimiter between the parent and child items.

### FixIssues

[`FixIssues`](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.AnalyzeRule.FixIssues*) is the fix operation. If there's an appropriate action to take in response to the analyze step, execute it here.

> [!TIP]
> If you set `CanFix` to `false`, you don't have to override the `FixIssues` method.

### ClearAnalysis

[`ClearAnalysis`](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.AnalyzeRule.ClearAnalysis) is the clear operation. Any data you cached in the analyze step can be cleaned or removed in this method. The `TreeView` will update to reflect the lack of data.

## Adding custom rules to the Addressables Analyze window

A custom rule must register itself with the GUI class using [`AnalyzeSystem.RegisterNewRule<TRule>`](xref:UnityEditor.AddressableAssets.Build.AnalyzeSystem.RegisterNewRule*), to display in the **Addressables Analyze** window. For example:

[!code-cs[sample](../Tests/Editor/DocExampleCode/MyRule.cs#doc_CustomRule)]

## AnalyzeRule classes

To make it faster to setup custom rules, Addressables includes the following classes, which inherit from `AnalyzeRule`:

* [`BundleRuleBase`](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.BundleRuleBase) is a base class for handling `AnalyzeRule` tasks. It includes some basic methods to retrieve information about AssetBundle and resource dependencies.
* __Check bundle duplicates__ base classes help check for AssetBundle dependency duplicates. Override the `FixIssues` method  implementation to perform a custom action:
  * [`CheckBundleDupeDependencies`](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.CheckBundleDupeDependencies) inherits from `BundleRuleBase` and includes further methods for `AnalyzeRule` to check AssetBundle dependencies for duplicates and a method to attempt to resolve these duplicates.
  * [`CheckResourcesDupeDependencies`](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.CheckResourcesDupeDependencies) is the same, but resource dependencies specific.
  * [`CheckSceneDupeDependencies`](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.CheckSceneDupeDependencies) is the same, but for scene dependencies specific.

## Additional resources

* [`AnalyzeRule` API reference](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.AnalyzeRule)
* [Analyze window](analyze-addressables-window.md)
* [Analyze window reference](analyze-addressables-window-reference.md)
* [Analyze Addressable layouts](analyze-addressable-layouts.md)
