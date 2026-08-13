---
uid: addressables-analyze-tool
---

# Addressables Analyze window

You can use the **Addressables Analyze** window to gather information on your project's Addressables layout. When you run a report in the **Addressables Analyze** window, Unity checks the layout of the Addressables in your project and uses rules to determine whether any issues it finds can be automatically fixed or not. You can then use the options in the window to automatically fix issues such as duplicate AssetBundle dependencies.

## Use Addressables Analyze

To use the Addressables Analyze window, open it in one of the following ways:

* From the Editor main menu, go to **Window** > **Asset Management** > **Addressables** > **Analyze**.
* From the [Addressables Groups](GroupsWindow.md) window, go to **Tools** > **Window** > **Analyze**.

The Analyze window displays a list of Analyze rules, along with the following operations:

![The Addressables Analyze window displaying expanded lists of fixable and unfixable rules.](images/addressables-analyze-window-data.png)*The Addressables Analyze window with some data loaded.*

The **Analyze Selected Rules** operation gathers the information needed by the rule. To run the operation:

1. Select the rules you want to run in the table. To run all rules, select the **Analyze Rules** top level item. To run specific rules, expand the **Analyze Rules** section and select the rule you want to analyze.
1. Select the **Analyze Selected Rules** button.

Unity then displays any data it finds related to the rules in the window. The data is represented as a list of [`AnalyzeResult`](xref:UnityEditor.AddressableAssets.Build.AnalyzeRules.AnalyzeRule.AnalyzeResult) objects.

If any issues are a **Fixable Rule**, you can use the **Fix Selected Rules** operation to automatically fix the issues Unity found.

### Fixing a rule

If the **Check Duplicate Bundle Dependencies** check discovers any issues, you can run the fix operation on this rule to create a new Addressable group to move all dependent assets to.

Duplicated assets result from assets in different groups sharing dependencies, for example two prefabs that share a material existing in different Addressable groups. That material (and any of its dependencies) is pulled into both groups containing the prefabs. To prevent this, the material must be marked as Addressable, either with one of the prefabs, or in its own space, which puts the material and its dependencies in a separate Addressable group.

However, there are some situations where you might not want to fix this issue. For example, if you have an asset containing multiple objects, different groups might only pull in portions of the asset, and not an actual duplicate. An FBX file with many meshes is an example of this. If one mesh is in `GroupA` and another is in `GroupB`, this rule identifies that the FBX is shared, and extracts it into its own group if you run the fix operation. In this case, running the fix operation is harmful, as neither group has the full FBX asset.

Also note that duplicate assets might not always be an issue. If assets are never requested by the same set of users (for example, region-specific assets), then you might need duplicate dependencies.


## Additional resources

* [Create custom rules for Addressables Analyze](analyze-custom-rules.md)
* [Addressables Analyze rules reference](analyze-addressables-window-reference.md)