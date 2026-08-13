# Define remote content profiles

A [profile](profiles-introduction.md) defines variables that you can use to set the build and load paths for both local and remote content.

When you distribute content remotely, you typically need to set different URLs (load paths) for remote content depending on why you want to make a build.

Some examples of such situations include:

* [Testing builds](#testing-builds)
* [Local hosting builds](#local-hosting-builds)
* [CDN builds]

## Testing builds

In development, or if you need to test without access to a host, you can treat all the content in your project as local content. For this situation, set the __Local__ and __Remote__ profile variables using the [__Built-In__ location](addressables-profiles-window.md).

## Local hosting builds

When you set up a host on your local network (or localhost), you need to change the __Load Path__ for the remote groups to reflect the URL of the host. For more information, refer to [Set a build and load path](profiles-build-load-paths.md).

## Builds for CDN

As you get closer to production, you might use a staging server and then, your production Content Delivery Network (CDN). For example if using Cloud Content Delivery, set the __Remote__ profile variable using the [__Cloud Content Delivery__](addressables-profiles-window.md#default-variables) location.

## Other profiles

After release, you might want to use different host URLs for beta testing or other purposes.

Rather than configuring the build and load paths every time you build, you can create a [different profile](profiles-create.md) and set the variables appropriately. Then, you can change profiles before making a content build without needing to reconfigure the paths.

If you use a script to launch content builds, then you can use the `Addressables` API to choose a specific profile for a build. For more information, refer to [Starting an Addressables build from a script](build-scripting-start-build.md).

If you have complex URLs, you can reference static fields or properties in profile variables that are evaluated at build or runtime. For example, rather than entering the CCD `ProjectID` as a string, you can create an Editor class that provides the information as a static property and reference it as `CCDInfo.ProjectID`. For more information, refer to [Profile variable syntax](xref:addressables-profile-variables).

[`InternalIdTransformFunc`](xref:addressables-api-transform-internal-id) methods provide an additional method of handling complex URL requirements.

> [!NOTE]
> If your remote URL can't be expressed as a static string, refer to [Change Addressable load URLs](TransformInternalId.md) for information about how you can rewrite the URL of assets, including AssetBundles, at runtime.

## Additional resources

* [Pre-download remote content](remote-content-predownload.md)
* [Use Addressables with Cloud Content Delivery](AddressablesCCD.md)
* [Change Addressable load URLs](TransformInternalId.md)