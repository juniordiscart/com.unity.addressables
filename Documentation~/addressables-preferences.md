# Addressables Preferences reference
The Addressables package adds its own section to the Unity Editor [Preferences](xref:Preferences) window (**File &gt; Settings** &gt; **Preferences** &gt; **Addressables**). The Addressables preferences include:

|**Preference**|**Description**|
|---|---|
|__Debug Build Layout__| Enable this preference to make the build system produce the [build layout report](xref:addressables-build-layout-report). This preference is disabled by default because it increases the time need to create a build. The build report contains a detailed description of each AssetBundle produced by the build. Refer to [Diagnostic tools](DiagnosticTools.md) for a description of this and other analysis tools.|
|__Build Addressables on Player Build__<br/><br/>Only available in Unity 2021.2+|Choose whether Unity builds Addressables content as part of your Player build. <br/><br/> Building Addressables content together with the Player can be convenient, but increases build time, especially on large projects because this rebuilds the Addressables content even when you haven't modified any assets. If you don't change your Addressables content between most builds, then select the __Do not Build Addressables content on Player Build__ mode.<br/><br/>The options include:<br/><br/>- __Build Addressables content on Player Build__: Always build Addressables content when building the Player.<br/>- __Do not Build Addressables content on Player Build__: Never build Addressables content when building the Player. If you modify Addressables content, you must rebuild it manually before building the Player. <br/><br/>These preferences override the global preference for the current project and affect all contributors who build the project.|

## Additional resources

* [Building Addressables content with Player builds](build-player-builds.md)