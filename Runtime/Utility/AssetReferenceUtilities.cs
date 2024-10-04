using System;
using System.Collections.Generic;
#if UNITY_EDITOR
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
        internal static List<(Object, Object)> GetAtlasSpritesAndPackables(ref SpriteAtlas atlas)
        {
            List<(Object, Object)> packables = new List<(Object, Object)>();

            Object[] atlasPackables = atlas.GetPackables();
            var sprites = new Sprite[atlas.spriteCount];
            atlas.GetSprites(sprites);
            for (int i = 0; i < atlas.spriteCount; i++)
            {
                packables.Add((sprites[i], atlasPackables[i]));
            }
            if (packables.Count != atlas.spriteCount)
            {
                Addressables.LogError($"Count mismatch between packables ({packables.Count}) and sprites in Atlas ({atlas.spriteCount}).");
            }

            return packables;
        }
#endif
    }
}
