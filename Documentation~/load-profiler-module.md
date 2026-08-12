# Use the Addressables Profiler module

You can use the Addressables [Profiler](xref:um-profiler) module to inspect what content Addressables loaded. To open the Profiler, go to __Window__ > __Analysis__ > __Profiler__.

## Prerequisites

* [Build Reports](BuildLayoutReport.md) must be enabled and the runtime being profiled requires a build report. To enable build reports, go to the Editor preferences, select [Addressables preferences](addressables-preferences.md), then enable **Debug Build Layout**.
* Collecting information about the running content requires build time data collection information for the debug build layout. These files are stored in the folder `<Project Directory>/Library/com.unity.addressables/buildReports`. Each build you make creates a new build report file in this directory. When running the Profiler, any incoming Profiler data from the Profiler target is synced and looks for the build report file for that runtime in the `buildReports` folder. If the build report doesn't exist, such as if the project was built using a different machine, then the Profiler doesn't display the information for that data. Select **Find in file system** to open a file select window, which can you can use to locate a build report file on disk elsewhere.
* The Profiler module doesn't support the Play Mode Scripts **Use Asset Database (Fastest)**. The content must be built and using **Use Existing Build** based Play Mode Scripts.

## Open the Profiler module

To open the Addressables Profiler module:

1. Open the Profiler window (__Window__ > __Analysis__ > __Profiler__).
1. In the top left of the Profiler window select the dropdown button labeled __Profiler Modules__.
1. Enable the __Addressable Assets__ module.

## View the module

Use the module view can to track how many AssetBundles, assets, scenes, and catalogs are loaded at the frame in time.

The following screenshot displays three assets and one scene, from one catalog, and six AssetBundles:

![The Profiler module window displaying the three assets, one Scene, one catalog, and six AssetBundles loaded in the current frame.](images/profiler-module.png)

When you select a frame, the detail pane fills with information for that frame and displays a tree view for the loaded content.

![The Profiler details pane with a Prefab asset selected in the Tree view and its details visible in the Details Inspector.](images/profiler-details-pane.png)

## Additional resources

* [Connecting the Profiler to a data source](xref:um-profiler-profiling-applications)
* [Profiler window reference](xref:um-profiler-window)