# Addressables Preferences reference

Reference for Unity Editor preferences specific to Addressables, covering global settings that apply across all Unity projects.

The Addressables package adds its own section to the Unity Editor [Preferences](xref:Preferences) window (**File &gt; Settings** &gt; **Preferences** &gt; **Addressables**).

|**Preference**|**Description**|
|---|---|
|__Debug Build Layout__| Generates a [build layout report](xref:addressables-build-layout-report) as part of the build process. The build report contains a detailed description of each AssetBundle produced by the build. This preference is disabled by default because it increases the total build time. For more information, refer to [Create a build report](BuildLayoutReport.md).|
|__Build Addressables on build Player__|Determines how Unity builds Addressables in a Player build: <ul><li>**Build Addressables on Player Build**: Builds Addressables content as part of the Player build. Building Addressables content together with the Player can be convenient, but increases build time, especially on large projects because this rebuilds the Addressables content even when you haven't modified any assets.</li><li>**Do Not Build Addressables on Player Build**: Choose this option if you don't change your Addressables content between most builds. If you modify Addressables content, you must rebuild it manually before building the Player.</li></ul>These preferences override the global preference for the current project and affect all contributors who build the project.|

## Additional resources

* [Building Addressables content with Player builds](build-player-builds.md)