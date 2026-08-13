using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
#endif
using UnityEngine.U2D;

namespace UnityEngine.AddressableAssets.Utility
{
    internal class AssetReferenceUtilities
    {
        static internal string FormatName(string name)
        {
            if (name.EndsWith("(Clone)", StringComparison.Ordinal))
                name = name.Replace("(Clone)", "");
            return name;
        }

#if UNITY_EDITOR

        static Sprite[] FilterSprites(Object[] objs)
        {
            var spriteCount = 0;
            var sprites = new Sprite[objs.Length];
            for (var i = 0; i < objs.Length; i++)
            {
                if (objs[i] is Sprite s)
                {
                    sprites[spriteCount] = s;
                    spriteCount++;
                }
            }

            return sprites[0..spriteCount];
        }

        internal static void GetSpritesFromPackable(Object packable, HashSet<(Sprite, string)> assets)
        {
            if (packable == null)
            {
                return;
            }
            var packablePath = AssetDatabase.GetAssetPath(packable);
            if (Directory.Exists(packablePath))
            {
                // everything gets included
                GetSpritesFromDirectory(packablePath, assets);
                return;
            }

            // top level packables are handled somewhat differently. If a texture contains multiple sprites
            // if the entire texture is included all sprites will be included. But at the top level a single
            // sprite can be picked out of a mutiple sprite texture so only that sprite should be included.
            var sprites = FilterSprites(AssetDatabase.LoadAllAssetsAtPath(packablePath));
            if (sprites.Length == 0)
            {
                Addressables.LogError($"unknown packable {packable.name} in {packablePath}");
                return;
            }


            if (sprites.Length == 1)
            {
                assets.Add((sprites[0], AssetDatabase.AssetPathToGUID(packablePath)));

                return;
            }

            var packableSprite = packable as Sprite;
            if (packableSprite == null)
            {
                Addressables.LogError($"unknown packable type {packable.name} in {packablePath}");
                return;
            }

            // this is using sprite mode multiple
            var guid = AssetDatabase.AssetPathToGUID(packablePath);
            foreach (var sprite in sprites)
            {
                if (sprite is Sprite s)
                {
                    if (s.name == packableSprite.name)
                    {
                        assets.Add((s, guid));
                    }
                }
            }
        }


        // Currently the 3 types of packable assets are Sprite, Texture2D, and DefaultAssets that are Folders
        internal static void GetSpritesFromDirectory(string folderPath, HashSet<(Sprite, string)> assets)
        {
            if (!Directory.Exists(folderPath))
            {
                // we only want to add explicitly added paths if they're sprites
                var sprites = AssetDatabase.LoadAllAssetsAtPath(folderPath);
                if (sprites.Length > 0)
                {
                    var guid = AssetDatabase.AssetPathToGUID(folderPath);
                    foreach (var sprite in sprites)
                    {
                        if (sprite is Sprite s)
                        {
                            assets.Add((s, s.GetSpriteID().ToString()));
                        }
                    }
                }
                return;
            }
            var assetGuids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
            foreach(var guid in assetGuids)
            {
                var sprites = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var sprite in sprites)
                {
                    if (sprite is Sprite s)
                    {
                        assets.Add((s, guid));
                    }
                }
            }
            // texture2Ds can be set to sprite type multiple so we need to check if they have any sprite children
            assetGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            foreach (var guid in assetGuids)
            {

                Object[] data = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (data != null)
                {
                    foreach (Object obj in data)
                    {
                        if (obj is Sprite s)
                        {
                            assets.Add((s, guid));
                        }
                    }
                }
            }
            assetGuids = AssetDatabase.FindAssets("t:DefaultAsset", new[] { folderPath });
            foreach(var guid in assetGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Directory.Exists(path))
                {
                    GetSpritesFromDirectory(path, assets);
                }
            }

        }

        internal static HashSet<(Sprite, string)> GetAtlasSpritesAndPackables(ref SpriteAtlas atlas)
        {
            var guids = new HashSet<(Sprite, string)>();

            if (atlas.isVariant)
                atlas = atlas.GetMasterAtlas();

            Object[] atlasPackables = atlas.GetPackables();
            foreach (var packable in atlasPackables)
            {
                GetSpritesFromPackable(packable, guids);
            }

            if (guids.Count != atlas.spriteCount)
            {
                Addressables.LogError($"Count mismatch between packables ({guids.Count}) and sprites in Atlas ({atlas.spriteCount}).");
                return guids;
            }

            return guids;
        }
#endif
    }
}
