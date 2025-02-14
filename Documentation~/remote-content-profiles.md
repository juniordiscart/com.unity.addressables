# Remote content profiles

A [Profile](profiles-introduction.md) defines variables that you can use to set the build and load paths for both your local and remote content.

When you distribute content remotely, you typically need to set different URLs (load paths) for your remote content depending on why you are making a build. Some examples of such situations include:

## Builds for testing general game play and function

Early in development or when you need to test without access to a host, you might find it convenient to treat all your content as local content. For this situation set the __Local__ and __Remote__ profile variables using the __Built-In__ location.

## Builds for local hosting

Later, when you set up a host on your local network (or localhost), you will need to change the Load Path for your remote groups to reflect the URL of the host. For example if using [Editor Hosting], set the __Remote__ profile variable using the __Editor Hosting__ location.

## Builds for CDN

As you get closer to production, you might use a staging server and then, your production Content Delivery Network (CDN). For example if using [Cloud Content Delivery], set the __Remote__ profile variable using the __Cloud Content Delivery__ location.

## Other profiles

Even after release, you might want to use different host URLs for beta testing or other purposes.

Rather than hand configuring the build and load paths every time you build, you can create a different Profile and set the variables appropriately. Then, you can easily switch between Profiles before making a content build without worrying about misconfiguring the paths.

If you use a script to launch your content builds, then you can use the Addressables API to choose a specific Profile for a build. Refer to [Starting an Addressables build from a script](build-scripting-start-build.md).

If you have complex URLs, you can reference static fields or properties in your Profile variables that are evaluated at build- or runtime. For example, rather than entering your CCD ProjectID as a string, you could create an Editor class that provides the information as a static property and reference it as `CCDInfo.ProjectID`. Refer to [Profile variable syntax](xref:addressables-profile-variables) for more information.

[`InternalIdTransformFunc`](xref:addressables-api-transform-internal-id) methods provide an additional method of handling complex URL requirements.

> [!NOTE]
> If your remote URL requires cannot be expressed as a static string see [Custom URL evaluation](xref:addressables-api-transform-internal-id) for information about how you can rewrite the URL for assets, including AssetBundles, at runtime.