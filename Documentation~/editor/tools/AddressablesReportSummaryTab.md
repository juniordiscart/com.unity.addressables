# The Summary Tab

![](../../images/BuildReportSummaryView.png)

The Summary Tab contains general information about the currently selected build report, along with information about any detected potential issues within the build. This information includes the locations of the catalogs created by the build, which profile the current build was built with, how long the build took, and the version of both the Addressables package and the Unity Editor that the build was created with.

The Summary Tab also contains some high level aggregated information about the build, including the number of AssetBundles created as part of the build, the size of the bundles built, and the number of assets in the build.

The count of assets pulled into the build by an Addressable asset is an important property in this view. This count includes assets that are referenced by an Addressable asset, but that are not marked as addressable. These assets must be included in the build to allow for assets that depend on them to be loaded.

Note that if multiple addressable assets in different bundles depend on the same non-addressable asset, then that non-addressable asset will be duplicated in multiple bundles. For more information about duplicated assets, see [Duplicated Assets View](AddressablesReportPotentialIssuesTab.md).
