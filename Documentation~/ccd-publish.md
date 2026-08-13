# Publish content with Cloud Content Delivery

To publish content with Cloud Content Delivery (CCD), you have the following options:

* Use CCD Dashboard/CLI
* Use CCD Management package

## Use CCD Dashboard/CLI

To generate and upload Addressable content to a CCD project:

1. Set the [profile you have set up for CCD](ccd-configure.md#configure-a-profile-with-a-cloud-content-delivery-url) as the active profile.
2. [Build the Addressables content](builds-full-build.md).
3. Upload the files created at the remote build path using the [CCD dashboard](https://docs.unity.com/ccd/Content/UnityCCDDashboard.htm) or [command-line interface](https://docs.unity.com/ccd/Content/UnityCCDCLI.htm).
4. Create a release and update the badge using the CCD dashboard or command-line interface.

Building Addressable content generates a content catalog (`.json`), a hash file (`.hash`), and one or more AssetBundle (`.bundle`) files. Upload these files to the bucket corresponding to the URL used in the profile load path.

If you have made changes to local content, you must create a new Player build.

## Use CCD Management package

To generate, upload, and release Addressable content to your CCD project:

1. Open the [Groups window](GroupsWindow.md) (menu: __Window > Asset Management > Addressables > Groups__).
2. Use the __Build & Release__ option.

The CCD Management package uses the default build script behavior to generate the Addressable bundles.

All groups associated with a path pair connected to a CCD bucket and badge via the drop-down window have their generated AssetBundles uploaded by the management package to the remote target.

Finally, the management package creates a release for the remote target and updates their badge.

### CcdManager

When setting up the project profile path pairs you can use the `Automatic` option. This option uses the `CcdManager` to set static properties at runtime to tell Addressables which Environment, Bucket, and Badge to use for loading assets. The `CcdManager` has the following properties: `EnvironmentName`, `BucketId`, and `Badge`. Setting these properties at runtime before Addressables initializes tells Addressables to look at these locations within CCD. To learn more about environments, buckets, and badges refer to [CCD organization](https://docs.unity.com/ccd/UnityCCD.html#CCD_organization).

Example snippet of setting CcdManager properties:

```c#
   CcdManager.EnvironmentName = ENV_NAME;
   CcdManager.BucketId = BUCKET_ID;
   CcdManager.Badge = BADGE;

   // Addressables call to load or instantiate asset
```

>[!NOTE]
> Any Addressables call initializes the system so be sure to set the `CcdManager` before any Addressables call to ensure that there are no race conditions or unexpected behaviors.

## Use build events
CCD provides a means of wrapping the build and upload service to provide additional functionality.

You can add additional events to PreUpdate and PreBuild event chains.

[!code-cs[sample](../Tests/Editor/DocExampleCode/PrintBucketInformation.cs#SAMPLE)]

### Disable version override warnings
If you get warnings about overriding the player version and want to keep your current setup, you can disable the warnings by removing the corresponding build events.

[!code-cs[sample](../Tests/Editor/DocExampleCode/DisableBuildWarnings.cs#SAMPLE)]


## Additional resources

* [CCD documentation](https://docs.unity.com/ugs/manual/ccd/manual/UnityCCD)
* [Groups window reference](GroupsWindow.md)
* [Enable remote content](remote-content-enable.md)
* [Define remote content profiles](remote-content-profiles.md)