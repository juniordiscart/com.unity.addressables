---
uid: addressables-shaders
---

# Shaders

By default, Unity [strips shaders variants] that aren't used in any scenes. This can exclude variants that are only used in AssetBundles. To ensure that certain variants are not stripped, include them in the **Shader Stripping** properties in [Graphics Settings]. 

For example, if you have Addressable assets that use lightmap-related shaders such as [Mixed Lights], go to **Edit** &gt; **Project Settings** &gt; **Graphics** &gt; **Shader Stripping** and set the **Lightmap Mode** property to **Custom**.

[Quality Settings] also affect shader variants used in AssetBundles.

[strips shaders variants]: xref:shader-variant-stripping
[Quality Settings]: xref:class-QualitySettings
[Mixed Lights]: xref:LightMode-Mixed
[Graphics Settings]: xref:class-GraphicsSettings