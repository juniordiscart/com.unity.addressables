# Configure Cloud Content Delivery

You can use Addressables with [Unity Cloud Content Delivery](https://docs.unity.com/ccd/UnityCCD.html) (CCD) to distribute Addressables content.

To set up Addressable assets to work with CCD you must configure a profile to include the CCD URL.

## Configure a profile with a Cloud Content Delivery URL

Use the `BuildPath` and `LoadPath` variables in a [profile](profiles-create.md) to specify where to create build artifacts and where to load assets at runtime. Configure the remote paths to work with CCD and leave the local paths with their default values, unless you have a specific reason to change them.

If necessary, create a new profile for publishing builds to CCD in the [Profiles window](addressables-profiles-window.md). Configure the remote path variables in this profile to access the content at the correct URL.

You can set the remote `BuildPath` to a convenient value. If you have multiple profiles, use a unique build path for each of them so that the build artifacts don't get mixed together, especially if you're hosting them from a different remote URL.

Set the remote `LoadPath` to one of the following paths:

* If you publish content using a badge:
  ```
    https://[ProjectID].client-api.unity3dusercontent.com/client_api/v1/environments/[EnvironmentName]/buckets/[BucketID]/release_by_badge/[BadgeName]/entry_by_path/content/?path=

  ```
* If you publish using a release:
  ```
    https://[ProjectID].client-api.unity3dusercontent.com/client_api/v1/environments/[EnvironmentName]/buckets/[BucketID]/releases/[ReleaseID]/entry_by_path/content/?path=

  ```

Where `[ProjectID]` is the CCD project's ID string, `[EnvironmentName]` is the name of the Environment of the project, `[BucketID]` is the Bucket ID string for a CCD bucket within the project, `[BadgeName]` is the name of the specific CCD badge, and `[ReleaseID]` is the ID of a specific release within a bucket.

For information about how to create and edit profiles, refer to [Organize build information into profiles](profiles-introduction.md).

> [!IMPORTANT]
> You must perform a full rebuild of the Addressables content in your project when you change the remote load path.

## Use the Cloud Content Delivery Bundle Location option

If your project uses the CCD service, you can set the profile's remote path pair to publish content to a designated bucket and badge.

This feature requires the [Content Delivery Management API package](https://services.docs.unity.com/content-delivery-client/index.html).

To set up a profile variable to use the CCD AssetBundle location:

1. Open the Profile window (menu: __Window > Asset Management > Addressables > Profiles__).
2. Select the profile to change.
3. Change the __Remote__ variable to use the __Cloud Content Delivery__ __Bundle Location__.
4. Choose `Automatic (set using CcdManager)` or `Specify the Environment, Bucket, and Badge` option. The `CcdManager` is a static class that is used to notify Addressables which Environment, Bucket, and Badge to load from at runtime.
5. Choose the Bucket to use. If no buckets are present, a window opens where you can create a new one.
6. Choose the Badge.

Make this the active profile when building content for delivery with CCD.

## Configure groups with CCD URL

Configure groups to use __Remote__ as their __Build & Load Path__ in the Inspector window.

Refer to [Organize assets into groups](groups-intro.md) for information about how to modify groups.

## Additional resources

* [Publish content with Cloud Content Delivery](ccd-publish.md)
* [Content Delivery Management API package](https://services.docs.unity.com/content-delivery-client/index.html)
* [CCD documentation](https://docs.unity.com/ugs/manual/ccd/manual/UnityCCD)