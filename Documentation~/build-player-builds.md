# Build Addressables content with Player builds

When you modify Addressable assets during development, you must rebuild your Addressables content before you build the Player. You can run the Addressables content build as a separate step before building a Player or you can run both the Addressables content build and the Player build together. 

Building Addressables content together with the Player can be convenient, but increases build time, especially on large projects. This is because Unity rebuilds the Addressables content even when you haven't modified any assets. If you don't change your Addressables content between most builds, consider disabling this option.

The __Build Addressables on Player Build__ setting in the Project [Addressable Asset Settings](xref:addressables-asset-settings#build) specifies which option to use for building Addressables content. You can choose the appropriate option for each project or defer to the [global Preferences setting](addressables-preferences.md). When you set a project-level setting, it applies to all contributors who build the project. The Preferences setting applies to all Unity Projects that don't set a specific value.

> [!NOTE] 
> Building Addressables on Player Build requires Unity 2021.2+. In earlier versions of Unity, you must build Addressables content as a separate step.

## Build commands

Access build commands from the __Build__ menu on the toolbar at the top of the [Groups window](GroupsWindow.md) (**Window &gt; Asset Management &gt; Addressables &gt; Groups**).

The menu provides the following items:

* __New Build__: Choose a build script to run a full content build. The Addressables package includes one build script, __Default Build Script__. If you create custom build scripts, you can access them here. For more information, refer to [Build scripting](BuildPlayerContent.md).
* __Update a Previous Build__: Run a differential update based on an earlier build. An update build can produce smaller downloads when you support remote content distribution and publish updated content. Refer tp [Content update builds](ContentUpdateWorkflow.md) for more information.
* __Clean Build__: Choose a command to clean existing build cache files. Each build script can provide a clean up function, which you can invoke from this menu.
