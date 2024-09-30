---
uid: addressables-report-potential-issues
---

# The Potential Issues View

![](images/BuildReportPotentialIssuesView.png)

The Potential Issues Tab scans the selected build report for any potential issues or problems that might arise as part of your build, similar to [Analyze rules](AnalyzeTool.md). Currently, there is only one view present within the Potential Issues View.

The **Duplicated Assets View** displays a list of all of the non-addressable assets that are duplicated between multiple bundles in your build. This often happens when two addressable assets are in different bundles, but both reference a common asset that's not marked as addressable.

This kind of issue can be fixed by either moving the addressable assets in question into the same bundle, or by making the duplicated asset addressable. Either method will have implications on your build dependencies. Use whichever method minimizes the impact on having the asset duplicated.

Selecting an asset in the Potential Issues Tab will open its information in [the Inspector panel](xref:addressables-report-inspector-reference).
