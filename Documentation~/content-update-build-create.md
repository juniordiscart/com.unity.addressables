# Build a content update

There are two tools you need to run to create a content update build:

1. The [Check for Content Update Restrictions tool](#check-for-content-update-restrictions-tool)
2. The [Update a Previous Build script](#update-a-previous-build)

The Check For Content Update Restrictions tool prepares your groups for a new content build. The Update a Previous Build script performs the build.

The build generates a content catalog, a hash file, and AssetBundles.

The generated content catalog has the same name as the catalog in the original application build, overwriting the old catalog and hash file. The application loads the hash file at runtime to decide if a new catalog is available. The system loads unmodified assets from existing bundles that shipped with the application or that the application has already downloaded.

The system uses the content version string and location information from the addressables_content_state.bin file to create the AssetBundles. Asset bundles that don't contain updated content are written using the same file names as those in the build selected for the update. If an AssetBundle contains updated content, a new bundle is generated that contains the updated content, with a new file name so that it can coexist with the original on your content hosting service. Only AssetBundles with new file names must be copied to the location that hosts your content though you can safely upload them all.

The system also builds AssetBundles for content that can't change, such as any local AssetBundles, but you don't need to upload them to the content hosting location, as no Addressables Asset entries reference them.

You shouldn't change the build scripts between building a new player and making content updates, such as player code or Addressables. This might cause unpredictable behavior in your application.

If you delete the local content bundles created by your Addressables build from the Project Library folder, attempts to load Assets in those bundles fail when you run your game or application in the Unity Editor and use the __Use Existing Build (requires built groups)__ Play mode script.

## Update a Previous Build
Before you can build a content update, you need to run the __Update a Previous Build__ script. To run this script:

1. Open the __Addressables Groups__ window in the Editor (__Window__ > __Asset Management__ > __Addressables__ > __Groups__).
2. From the __Build__ menu on the toolbar, run the __Update a Previous Build__ script.

If you don't want the __Update a Previous Build__ script to run the [Check for Content Update Restrictions](#check-for-content-update-restrictions-tool) check automatically, run the tool manually before you begin your build.

## Check for Content Update Restrictions tool

The __Check for Content Update Restrictions__ tool prepares your group organization for a content update build. The tool examines the `addressables_content_state.bin` file and group settings.

>[!IMPORTANT]
> Before you run the __Check for Content Update Restrictions__ tool, make a new branch with your version control system. The tool rearranges your asset groups in a way suited for updating content. Branching ensures that next time you ship a full player build, you can return to your preferred content arrangement.

If you set a group's __Update Restrictions__ property to __Prevent Updates__ in the previous build, the tool gives you the option to move any changed assets to a new remote group. Apply the suggested changes, or revert changes to these assets, unless you have a specific reason not to.

When you create the update build, the new catalog maps the changed assets to their new, remote AssetBundles, while still mapping the unchanged assets to their original AssetBundles. Checking for content update restrictions doesn't check groups with __Prevent Updates__ disabled.

An asset is considered as changed based on the hash returned by [AssetDatabase.GetAssetDependencyHash]. This editor API has limitations and may not accurately reflect AssetBundle changes that are calculated at build time. For example it computes hash of the content of .cs files. This means that performing whitespace changes in a .cs file will result in a different hash, but the actual AssetBundle containing the file is unchanged. See [Changes to scripts that require rebuilding Addressables] for more information.

To run the tool:

1. Open the __Addressables Groups__ window in the Unity Editor (__Window__ > __Asset Management__ > __Addressables__ > __Groups__).
2. In the groups window, run the __Check for Content Update Restrictions__ from the toolbar __Tools__ menu.
3. Review the group changes made by the tool, if desired. You can change the names of any new remote groups the tool created, but moving assets to different groups can have unintended consequences.

[AssetDatabase.GetAssetDependencyHash](xref:UnityEditor.AssetDatabase.GetAssetDependencyHash(System.String))
[Changes to scripts that require rebuilding Addressables](builds-update-build.md##changes-to-scripts-that-require-rebuilding-addressabes)
