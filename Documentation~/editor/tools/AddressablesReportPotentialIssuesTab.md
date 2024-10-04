# The Potential Issues View

![](../../images/BuildReportPotentialIssuesView.png)

The Potential Issues Tab scans the selected build report for any potential issues or problems that may have arisen as part of your build and enables you to see how these issues may have arisen, similar to [Analyze rules](AnalyzeTool.md). Currently, there is only one view present within the Potential Issues View.

The **Duplicated Assets View** displays a list of all of the non-addressable assets that are duplicated between multiple bundles in your build. This often happens when two addressable assets are in different bundles, but both reference a common asset that is not marked as addressable.

This kind of issue can be fixed by either moving the addressable assets in question into the same bundle, or by making the asset that is duplicated addressable. Either means of fixing it will have implications on your build dependencies. You should usually use whichever method minimizes the impact on having the asset duplicated.

Selecting an asset in the Potential Issues Tab will open its information in [The Inspector panel](AddressablesReportInspector.md).
