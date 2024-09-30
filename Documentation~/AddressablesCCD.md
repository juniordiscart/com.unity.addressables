---
uid: addressables-ccd
---

# Use Addressables with Cloud Content Delivery

You can use Addressables in conjunction with [Unity Cloud Content Delivery](https://docs.unity.com/ccd/UnityCCD.html) (CCD) to distribute your remote Addressables content.

To set up Addressable assets to work with CCD:
1. Configure a profile to include your CCD URL
2. Build your AssetBundles, then upload them to CCD

## Configure a profile with CCD URL

The `BuildPath` and `LoadPath` variables stored in [Profiles](profiles-create.md) specify where the Addressables system creates your build artifacts and where it looks for your assets at runtime. Configure the remote paths to work with CCD. Leave the local paths with their standard, default values, unless you have a specific reason to change them.

If necessary, create a new profile for publishing builds to CCD on the [Profiles window](addressables-profiles-window.md). Configure the remote path variables in this profile to access your content at the correct URL.

You can set the remote `BuildPath` to a convenient value. If you have multiple profiles, consider using a unique build path for each of them so that the build artifacts don't get mixed together, especially if you're hosting them from a different remote URL.

Set the remote `LoadPath` to one of the following two paths:

* If you publish content using a badge: 

```
  https://(ProjectID).client-api.unity3dusercontent.com/client_api/v1/environments/(EnvironmentName)/buckets/(BucketID)/release_by_badge/(BadgeName)/entry_by_path/content/?path=

```

* If you publish using a release: 

```
  https://(ProjectID).client-api.unity3dusercontent.com/client_api/v1/environments/(EnvironmentName)/buckets/(BucketID)/releases/(ReleaseID)/entry_by_path/content/?path=

```

* `(ProjectID)` is your CCD project's ID string
* `(EnvironmentName)` is the name of the Environment of your project
* `(BucketID)` is the Bucket ID string for a CCD bucket within your project
* `(ReleaseID)` is the ID of a specific release within a bucket
* `(BadgeName)` is the name of the specific CCD badge

Refer to [Profiles](profiles-introduction.md) for information about how to create and edit profiles.

> [!IMPORTANT]
> You must perform a full rebuild your Addressables content when you change the remote load path.

### Use the Cloud Content Delivery Bundle Location option

If your project is set up to use the CCD service, you can set the profile's remote path pair to publish content to a designated bucket and badge.

This feature requires the Content Delivery Management API package.

To set up a Profile variable to use the CCD bundle location:

1. Open the Profile window (menu: __Window > Asset Management > Addressables > Profiles__).
2. Select the profile to change.
3. Change the __Remote__ variable to use the __Cloud Content Delivery__ __Bundle Location__.
4. Choose `Automatic (set using CcdManager)` or `Specify the Environment, Bucket, and Badge` option. The `CcdManager` is a static class that is used to notify Addressables which Environment, Bucket, and Badge to load from at Runtime. .
   * If choosing Automatic, select the environment you wish to use.
   * If choosing to specify, select the environment you wish to use
5. Choose the Bucket to use. If no buckets are present, you will be presented with a window where you can create one.
6. Choose the Badge.

Make this the active profile when building content for delivery with CCD.

Refer to [Profiles](profiles-introduction.md) for information about how to modify profiles.

## Configure groups with CCD URL

Configure groups to use __Remote__ as their __Build & Load Path__ in the Inspector window.

Refer to [Groups](Groups.md) for information about how to modify groups.

## Build, upload and release Addressable content

### Use CCD Dashboard/CLI

To generate and upload Addressable content to your CCD project:

1. Set the profile you have set up for CCD as the active profile.
2. Build your Addressables content. 
   * If you are making a full content build, refer to [Building your Addressable content](builds-full-build.md).
   * If your are updating an existing build with modified remote content, refer to [Building for content updates](content-update-build-create.md).
3. Upload the files created at the remote build path using the [CCD dashboard](https://docs.unity.com/ccd/Content/UnityCCDDashboard.htm) or [command-line interface](https://docs.unity.com/ccd/Content/UnityCCDCLI.htm).
4. Create a release and update the badge using the CCD dashboard or command-line interface.

Building your Addressable content generates a content catalog  (.json), a hash file (.hash), and one or more AssetBundle (.bundle) files. Upload these files to the bucket corresponding to the URL used in your profile load path.

If you have made changes to local content, you must create a new Player build.

### Use CCD Management package

To generate, upload, and release Addressable content to your CCD project:

1. Open the Groups window (menu: __Window > Asset Management > Addressables > Groups__).
2. Use the __Build & Release__ option.

The CCD Management package will use the default build script behavior to generate the Addressable bundles.

Then, all groups associated with a path pair that is connected to a CCD bucket and badge via the drop-down window will have their generated bundles uploaded by the management package to those remote target.

Finally, the management package will a create release for those remote target and update their badge.

#### CcdManager

When setting up the project profile path pairs and utilizing CCD, there is an option to use `Automatic`. This option utilizes the `CcdManager` to set static properties at Runtime to tell Addressables which Environment, Bucket, and Badge to reach out to for loading assets. The `CcdManager` has the following properties: `EnvironmentName`, `BucketId`, and `Badge`. Setting these properties at runtime before Addressables initializes will tell Addressables to look at these locations within CCD. To learn more about environments, buckets, and badges, refer to [CCD organization](https://docs.unity.com/ccd/UnityCCD.html#CCD_organization).

Example Snippet of setting CcdManager Properties:
```c#
   CcdManager.EnvironmentName = ENV_NAME;
   CcdManager.BucketId = BUCKET_ID;
   CcdManager.Badge = BADGE;

   // Addressables call to load or instantiate asset
```
>[!Note]
> ANY Addressables call initializes the system so be sure to set the `CcdManager` prior to any Addressables call to ensure that there are no race conditions or unexpected behaviors.

## Use build events
CCD provides a means of wrapping the build and upload service to provide additional functionality.

### Add a build event
You can add additional events to PreUpdate and PreBuild event chains.

[!code-cs[sample](../Tests/Editor/DocExampleCode/PrintBucketInformation.cs#SAMPLE)]

### Disable version override warnings
If you are getting warnings about overriding the player version and would like to keep your current setup, you can disable the warnings by removing the corresponding build events.

[!code-cs[sample](../Tests/Editor/DocExampleCode/DisableBuildWarnings.cs#SAMPLE)]
