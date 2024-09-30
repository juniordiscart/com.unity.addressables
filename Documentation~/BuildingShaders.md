---
uid: addressables-shaders
---

# Build shaders

By default, Unity [strips shaders variants](xref:shader-variant-stripping) that aren't used in any scenes. This can exclude variants that are only used in AssetBundles. To ensure that certain variants are not stripped, include them in the **Shader Stripping** properties in [Graphics Settings](xref:class-GraphicsSettings). 

For example, if you have Addressable assets that use lightmap-related shaders such as [Mixed Lights](xref:LightMode-Mixed), go to **Edit** &gt; **Project Settings** &gt; **Graphics** &gt; **Shader Stripping** and set the **Lightmap Mode** property to **Custom**.

[Quality Settings](xref:class-QualitySettings) also affect shader variants used in AssetBundles.
