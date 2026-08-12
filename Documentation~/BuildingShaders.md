---
uid: addressables-shaders
---

# Build shaders

By default, Unity [strips shaders variants](https://docs.unity3d.com/Manual/shader-variant-stripping.html) that aren't used in any scenes. This can exclude variants that are only used in AssetBundles. To ensure that certain variants aren't stripped, include them in the **Shader Stripping** properties in [Graphics Settings](https://docs.unity3d.com/Manual/class-GraphicsSettings.html).

For example, if you have Addressable assets that use lightmap-related shaders such as [Mixed Lights](https://docs.unity3d.com/Manual/LightModes-introduction.html#mixed), go to **Edit** &gt; **Project Settings** &gt; **Graphics** &gt; **Shader Stripping** and set the **Lightmap Mode** property to **Custom**.

[Quality Settings](https://docs.unity3d.com/Manual/class-QualitySettings.html) also affect shader variants used in AssetBundles.

## Additional resources

* [Build sprite atlases](AddressablesAndSpriteAtlases.md)
* [Graphics Settings reference](https://docs.unity3d.com/Manual/class-GraphicsSettings.html)