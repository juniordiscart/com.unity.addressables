---
uid: addressables-and-sprite-atlases
---

# Build sprite atlases

Some `SpriteAtlas` options can change how Unity loads sprites. This is important to consider if you want to use the **Use Asset Database** [Play mode Script](xref:addressables-groups-window).

The following examples explain how Addressables handles a `SpriteAtlas` differently than other assets:

## Addressable sprites 

### Sprites in separate groups

You have three Addressable textures in three separate groups, where each texture builds to around 500KB. Because they exist in separate groups, Unity builds them into three separate AssetBundles. Each AssetBundle uses around 500KB and only contains the sprite texture and associated metadata, with no dependencies.

### Sprites in a non-Addressable SpriteAtlas

The three textures in the previous example are put into a non-Addressable `SpriteAtlas`. In this case, Unity still generates three AssetBundles, but they're not the same size. One of the AssetBundles contains the atlas texture and uses about 1500KB. The other two AssetBundles only contain sprite metadata and list the atlas AssetBundle as a dependency. 

Although you can't control which AssetBundle contains the texture, the process is deterministic, so the same AssetBundle contains the texture through different rebuilds. This is the main difference from the standard duplication of dependencies. The sprites are dependent on the SpriteAtlas texture to load, and yet that texture is not built into all three AssetBundles, but is instead built only into one.

### Sprites in a SpriteAtlas AssetBundle

This time, the `SpriteAtlas` from the previous example is marked as Addressable in its own AssetBundle. Unity now creates four AssetBundles. The three AssetBundles with the sprites are each only a few KB and have a dependency on the fourth AssetBundle, which contains the `SpriteAtlas` and is about 1500KB. If you are using 2019.4 or older, the texture itself might end up elsewhere. The three sprite AssetBundles still depend on the `SpriteAtlas` AssetBundle. However, the `SpriteAtlas` AssetBundle can only contain metadata, and the texture can be in one of the other sprite AssetBundles.

## Addressable prefabs with sprite dependencies 

### Sprite prefabs

You have three Addressable sprite prefabs and each prefab has a dependency on its own sprite (about 500KB). Building the three prefabs separately results in three AssetBundles of about 500KB each.

### Sprite prefabs in a non-Addressable SpriteAtlas

The three textures from the previous example are added to a `SpriteAtlas`, and that atlas is not marked as Addressable. In this scenario, the `SpriteAtlas` texture is duplicated. All three AssetBundles are approximately 1500KB. This is expected based on the general rules about duplication of dependencies, but goes against the behavior seen in the previous section.

### Sprite prefabs in a SpriteAtlas AssetBundle

The `SpriteAtlas` from the previous example is now also marked as Addressable. Conforming to the rules of explicit inclusion, the `SpriteAtlas` texture is included only in the AssetBundle containing the `SpriteAtlas`. The AssetBundles with prefabs reference this fourth AssetBundle as a dependency. This will lead to three AssetBundles of about 500KB and one of approximately 1500KB.
