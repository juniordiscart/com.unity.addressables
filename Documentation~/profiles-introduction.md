# Profiles introduction

A profile contains a set of variables that the Addressables build scripts uses. These variables define information such as where to save build artifacts and where to load data at runtime. You can add custom profile variables to use in your own build scripts.

To manage the profiles in your project, use the [Addressables Profiles window](addressables-profiles-window.md) (menu: __Window > Asset Management > Addressables > Profiles__).

You can set up different profiles for the different phases or tasks in your development process. For example, you might create a profile to use while developing your project, another to use for testing, and one to use for final publishing or release. Setting up the profiles in advance and swapping between them is best practice, rather than editing the values individually when you move to a different phase or perform a different task.

> [!TIP]
> Using multiple profiles is most helpful when you distribute content for your application remotely. If you distribute all content as part of your application install, then the single, default profile might be the only profile you need.

## Profile examples

Consider the following example, which demonstrates the local development phase of your content:

![](images/profiles-example-local.png)<br/>*Content with local and remote bundles stored locally for development.*

While in development, you would have both your local and remote bundles using local paths:

![](images/profiles-example-local-paths.png)<br/>*Paths set for local development.*

In this instance, the local and remote paths are in fact local, which makes it unnecessary to set up a remote server just for local development. 

Once the content is ready for production, you can move the remote bundles to a server:

![](images/profiles-example-remote.png)<br/>*Content with the remote bundles moved to a server for production.*

In this case, if you use profiles, you can change the remote load path for `Production` to that server. Without having to change your asset groups, you can change all of your remote bundles to remote.

![](images/profiles-example-remote-paths.png)<br/>*Paths set for hosting remote content*

The Addressables system only copies data from [Addressables.BuildPath](xref:UnityEngine.AddressableAssets.Addressables.BuildPath) to the StreamingAssets folder during a Player build. It doesn't handle arbitrary paths specified through the LocalBuildPath or LocalLoadPath variables. If you build data to a different location or load data from a different location than the default, you must copy the data manually. 

Similarly, you must manually upload remote AssetBundles and associated catalog and hash files to your server so that they can be accessed at the URL defined by __RemoteLoadPath__.
