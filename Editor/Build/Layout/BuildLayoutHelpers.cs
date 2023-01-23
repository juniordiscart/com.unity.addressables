using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Video;

namespace UnityEditor.AddressableAssets.Build.Layout
{
    /// <summary>
    /// Helper methods for gathering data about a build layout.
    /// </summary>
    public class BuildLayoutHelpers
    {
        /// <summary>
        /// Gather a list of Explicit Assets defined in a BuildLayout
        /// </summary>
        /// <param name="layout">The BuildLayout generated during a build</param>
        /// <returns>A list of ExplicitAsset data.</returns>
        public static IEnumerable<BuildLayout.ExplicitAsset> EnumerateAssets(BuildLayout layout)
        {
            return EnumerateBundles(layout).SelectMany(b => b.Files).SelectMany(f => f.Assets);
        }

        /// <summary>
        /// Gather a list of Explicit Assets defined in a Bundle
        /// </summary>
        /// <param name="bundle">The Bundle data generated during a build</param>
        /// <returns>A list of ExplicitAssets defined in the Bundle</returns>
        public static IEnumerable<BuildLayout.ExplicitAsset> EnumerateAssets(BuildLayout.Bundle bundle)
        {
            return bundle.Files.SelectMany(f => f.Assets);
        }

        /// <summary>
        /// Gather a list of Bundle data defined in a BuildLayout
        /// </summary>
        /// <param name="layout">The BuildLayout generated during a build</param>
        /// <returns>A list of the Bundle data defined in a BuildLayout</returns>
        public static IEnumerable<BuildLayout.Bundle> EnumerateBundles(BuildLayout layout)
        {
            foreach (BuildLayout.Bundle b in layout.BuiltInBundles)
                yield return b;

            foreach (BuildLayout.Bundle b in layout.Groups.SelectMany(g => g.Bundles))
                yield return b;
        }

        /// <summary>
        /// Gather a list of File data defined in a BuildLayout
        /// </summary>
        /// <param name="layout">The BuildLayout generated during a build</param>
        /// <returns>A list of File data</returns>
        public static IEnumerable<BuildLayout.File> EnumerateFiles(BuildLayout layout)
        {
            return EnumerateBundles(layout).SelectMany(b => b.Files);
        }

        static Type AnimationClipType = typeof(AnimationClip);
        static Type AnimationControllerType = typeof(RuntimeAnimatorController);
        static Type AvatarType = typeof(Avatar);
        static Type AudioClipType = typeof(AudioClip);
        static Type AudioMixerType = typeof(AudioMixer);
        static Type ComputeShaderType = typeof(ComputeShader);
        static Type FontType = typeof(Font);
        static Type GUISkinType = typeof(GUISkin);
        static Type MaterialType = typeof(Material);
        static Type MeshType = typeof(Mesh);
        static Type PhysicsMaterialType = typeof(PhysicMaterial);
        static Type PhysicsMaterial2DType = typeof(PhysicsMaterial2D);
        static Type ShaderType = typeof(Shader);
        static Type SpriteType = typeof(Sprite);
        static Type TextureType = typeof(Texture);
        static Type Texture2DType = typeof(Texture2D);
        static Type Texture3DType = typeof(Texture3D);
        static Type VideoClipType = typeof(VideoClip);
        static Type TextAssetType = typeof(TextAsset);
        static Type GameObjectType = typeof(GameObject);
        static Type ScriptableObjectType = typeof(ScriptableObject);
        static Type SceneType = typeof(SceneAsset);
        static Type MonoBehaviourType = typeof(MonoBehaviour);
        static Type ComponentType = typeof(Component);

        public static AssetType GetAssetType(Type ofType)
        {
            if (ofType == null)
                return AssetType.Other;

            if (ofType == AnimationClipType) return AssetType.AnimationClip;
            if (ofType == AvatarType) return AssetType.Avatar;
            if (ofType == AudioClipType) return AssetType.AudioClip;
            if (ofType == AudioMixerType) return AssetType.AudioMixer;
            if (ofType == ComputeShaderType) return AssetType.ComputeShader;
            if (ofType == FontType) return AssetType.Font;
            if (ofType == GUISkinType) return AssetType.GUISkin;
            if (ofType == MaterialType) return AssetType.Material;
            if (ofType == MeshType) return AssetType.Mesh;
            if (ofType == GameObjectType) return AssetType.GameObject;
            if (ofType == PhysicsMaterialType) return AssetType.PhysicsMaterial;
            if (ScriptableObjectType.IsAssignableFrom(ofType)) return AssetType.ScriptableObject;
            if (ofType == ShaderType) return AssetType.Shader;
            if (ofType == SpriteType) return AssetType.Sprite;
            if (ofType == TextureType) return AssetType.Texture;
            if (ofType == Texture2DType) return AssetType.Texture2D;
            if (ofType == Texture3DType) return AssetType.Texture3D;
            if (ofType == VideoClipType) return AssetType.VideoClip;
            if (ofType == TextAssetType) return AssetType.TextAsset;
            if (ofType == PhysicsMaterial2DType) return AssetType.PhysicsMaterial2D;
            if (ofType == SceneType) return AssetType.Scene;
            if (MonoBehaviourType.IsAssignableFrom(ofType)) return AssetType.MonoBehaviour;
            if (ComponentType.IsAssignableFrom(ofType)) return AssetType.Component;

            ofType = AddressableAssetUtility.MapEditorTypeToRuntimeType(ofType, false);
            if (ofType == AnimationControllerType) return AssetType.AnimationController;

            return AssetType.Other;
        }
    }
}
