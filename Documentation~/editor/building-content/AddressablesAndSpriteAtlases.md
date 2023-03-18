---
uid: addressables-and-sprite-atlases
---

# Addressables and SpriteAtlases

Some SpriteAtlas options can change how Unity loads Sprites. This is important to consider if you want to use the **Use Asset Database** [Play Mode Script].

The following examples demonstrate how Addressables handles SpriteAtlases differently than other Assets:

### Addressable Sprites 

__Example 1:__


You have three Addressable textures in three separate groups, where each texture builds to around 500KB. Since they exist in separate groups, Unity builds them into three separate AssetBundles. Each AssetBundle uses around 500KB and only contains the sprite texture and associated metadata, with no dependencies.

__Example 2:__

The three textures in Example 1 are put into a non-Addressable SpriteAtlas. In this case, Unity still generates three AssetBundles, but they are not the same size .One of the AssetBundles contains the atlas texture and uses about 1500KB. The other two AssetBundles only contain Sprite metadata and list the atlas AssetBundle as a dependency. 

Although you can't control which AssetBundle contains the texture, the process is deterministic, so the same AssetBundle will contain the texture through different rebuilds. This is the main difference from the standard duplication of dependencies. The sprites are dependent on the SpriteAtlas texture to load, and yet that texture is not built into all three AssetBundles, but is instead built only into one.

__Example 3:__

This time, the SpriteAtlas from Example 2 is marked as Addressable in its own AssetBundle. Unity now creates four AssetBundles. If you use Unity version 2020.x or newer, this builds as you would expect. The three AssetBundles with the sprites are each only a few KB and have a dependency on the fourth AssetBundle, which contains the SpriteAtlas and is about 1500KB. If you are using 2019.4 or older, the texture itself may end up elsewhere. The three Sprite AssetBundles still depend on the SpriteAtlas AssetBundle. However, the SpriteAtlas AssetBundle may only contain metadata, and the texture may be in one of the other Sprite AssetBundles.

### Addressable Prefabs With Sprite dependencies 

__Example 1:__

You have three Addressable sprite prefabs - each prefab has a dependency on its own sprite (about 500KB). Building the three prefabs separately results in three AssetBundles of about 500KB each.

__Example 2:__

The three textures from the previous example are added to a SpriteAtlas, and that atlas is not marked as Addressable. In this scenario, the SpriteAtlas texture is duplicated. All three AssetBundles are approximately 1500KB. This is expected based on the general rules about duplication of dependencies, but goes against the behavior seen in "Addressable Sprite Example 2".

__Example 3:__

The SpriteAtlas from the previous example is now also marked as Addressable. Conforming to the rules of explicit inclusion, the SpriteAtlas texture is included only in the AssetBundle containing the SpriteAtlas. The AssetBundles with prefabs reference this fourth AssetBundle as a dependency. This will lead to three AssetBundles of about 500KB and one of approximately 1500KB.

[AssetBundle dependencies manual page]: xref:AssetBundles-Dependencies
[Builds]: xref:addressables-builds
[Bundle Layout Preview]: xref:addressables-analyze-tool#unfixable-rules
[Build Layout Report]: xref:addressables-build-layout-report
[Check Duplicate Bundle Dependencies]: xref:addressables-analyze-tool#fixable-rules
[content build]: xref:addressables-builds
[Include In Build]: https://docs.unity3d.com/Manual/SpriteAtlasDistribution.html#Dontinclbuild
[Graphics Settings]: xref:class-GraphicsSettings
[Memory implications of loading AssetBundle dependencies]: xref:addressables-memory-management#memory-implications-of-loading-assetbundle-dependencies
[Mixed Lights]: xref:LightMode-Mixed
[Play Mode Script]: xref:addressables-groups-window#play-mode-scripts
[Sprite Packer Mode]: https://docs.unity3d.com/Manual/SpritePackerModes.html
[strips shaders variants]: xref:shader-variant-stripping
[Quality Settings]: xref:class-QualitySettings
