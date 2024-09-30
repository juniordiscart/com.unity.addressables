#if  UNITY_2022_2_OR_NEWER

using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class ProfilerGUIUtilities
    {
        private static Dictionary<AssetType, string> m_AssetTypeToTextureName = null;
        private static Dictionary<AssetType, string> AssetTypeToIconName
        {
            get
            {
                if (m_AssetTypeToTextureName != null)
                    return m_AssetTypeToTextureName;
                m_AssetTypeToTextureName = new Dictionary<AssetType, string>
                {
                    { AssetType.Other, "DefaultAsset Icon" },
                    { AssetType.Font, "d_Font Icon" },
                    { AssetType.GUISkin, "d_GUISkin Icon" },
                    { AssetType.AnimationClip, "d_AnimationClip Icon" },
                    { AssetType.Avatar, "d_Avatar Icon" },
                    { AssetType.AnimationController, "d_AnimatorController Icon" },
                    { AssetType.AudioClip, "d_AudioClip Icon" },
                    { AssetType.AudioMixer, "d_AudioMixerController Icon" },
                    { AssetType.VideoClip, "VideoClip Icon" },
                    { AssetType.Shader, "d_Shader Icon" },
                    { AssetType.ComputeShader, "d_ComputeShader Icon" },
                    { AssetType.Mesh, "d_Mesh Icon" },
                    { AssetType.Texture, "d_Texture Icon" },
                    { AssetType.Texture2D, "d_Texture2D Icon" },
                    { AssetType.Texture3D, "d_Texture Icon" },

                    { AssetType.Sprite, "d_Sprite Icon" },
                    { AssetType.ScriptableObject, "d_ScriptableObject Icon" },
                    { AssetType.Prefab, "d_Prefab Icon" },
                    { AssetType.Model, "d_PrefabModel Icon" },
                    { AssetType.Material, "d_Material Icon" },
                    { AssetType.PhysicsMaterial, "d_PhysicMaterial Icon" },
                    { AssetType.PhysicsMaterial2D, "d_PhysicsMaterial2D Icon" },
                    { AssetType.TextAsset, "d_TextAsset Icon" },
                    { AssetType.Scene, "d_SceneAsset Icon" },

                    { AssetType.GameObject, "d_GameObject Icon" },
                    { AssetType.SceneObject, "d_SceneAsset Icon"},
                    { AssetType.MonoBehaviour, "d_cs Script Icon"},
                    { AssetType.Component, "d_cs Script Icon"},
                    { AssetType.MonoScript, "DefaultAsset Icon"},

                    { AssetType.Cubemap, "d_Cubemap Icon"},

                    { AssetType.NavMeshSettings, "d_NavMeshData Icon"},
                    { AssetType.RenderSettings, "DefaultAsset Icon"},
                    { AssetType.LightmapSettings, "d_LightmapParameters Icon"},
                };
                return m_AssetTypeToTextureName;
            }
        }

        private static Dictionary<string, string> m_ComponentToTextureName = null;
        private static Dictionary<string, string> ComponentNameToIconName
        {
            get
            {
                if (m_ComponentToTextureName != null)
                    return m_ComponentToTextureName;
                m_ComponentToTextureName = new()
                {
                    // standard
                    { "Transform", "d_Transform Icon" },
                    { "GameObject", "d_GameObject Icon" },

                    // rendering
                    { "Camera", "d_Camera Icon"},
                    { "MeshRenderer", "d_MeshRenderer Icon" },
                    { "SkinnedMeshRenderer", "d_SkinnedMeshRenderer Icon"},
                    { "MeshFilter", "d_MeshFilter Icon" },
                    { "CanvasRenderer", "d_CanvasRenderer Icon"},
                    { "FlareLayer", "d_FlareLayer Icon"},
                    { "LODGroup", "d_LODGroup Icon"},
                    { "Light", "d_Light Icon"},
                    { "LightProbeGroup", "d_LightProbeGroup Icon"},
                    { "LightProbeProxyVolume", "d_LightProbeProxyVolume Icon"},
                    { "OcclusionArea", "d_OcclusionArea Icon"},
                    { "OcclusionPortal", "d_OcclusionPortal Icon"},
                    { "ReflectionProbe", "d_ReflectionProbe Icon"},
                    { "Skybox", "d_Skybox Icon"},
                    { "SortingGroup", "d_SortingGroup Icon"},
                    { "SpriteRenderer", "d_SpriteRenderer Icon"},
                    { "StreamingController", "d_StreamingController Icon"},

                    // Renderers
                    { "BillboardRenderer", "d_BillboardRenderer Icon"},
                    { "LineRenderer", "d_LineRenderer Icon"},
                    { "SpriteShapeRenderer", "d_SpriteShapeRenderer Icon"},
                    { "TrailRenderer", "d_TrailRenderer Icon"},

                    // Audio
                    { "AudioChorusFilter", "d_AudioChorusFilter Icon"},
                    { "AudioDistortionFilter", "d_AudioDistortionFilter Icon"},
                    { "AudioEchoFilter", "d_AudioEchoFilter Icon"},
                    { "AudioHighPassFilter", "d_AudioHighPassFilter Icon"},
                    { "AudioLowPassFilter", "d_AudioLowPassFilter Icon"},
                    { "AudioReverbFilter", "d_AudioReverbFilter Icon"},
                    { "AudioReverbZone", "d_AudioReverbZone Icon"},
                    { "AudioSource", "d_AudioSource Icon"},
                    { "AudioListener", "d_AudioListener Icon"},

                    // Effects
                    { "Halo", "d_Halo Icon"},
                    { "LensFlare", "LensFlare Icon"},
                    { "ParticleSystem", "d_ParticleSystem Icon"},
                    { "Projector", "d_Projector Icon"},
                    { "VisualEffect", "d_VisualEffect Icon"},

                    // Event
                    { "EventSystem", "d_EventSystem Icon"},
                    { "EventTrigger", "d_EventTrigger Icon"},
                    { "GraphicRaycaster", "d_GraphicRaycaster Icon"},
                    { "Physics2DRaycaster", "d_Physics2DRaycaster Icon"},
                    { "PhysicsRaycaster", "d_PhysicsRaycaster Icon"},
                    { "StandaloneInputModule", "d_StandaloneInputModule Icon"},
                    { "TouchInputModule", "d_TouchInputModule Icon"},

                    // Layout
                    { "AspectRatioFitter", "d_AspectRatioFitter Icon"},
                    { "Canvas", "d_Canvas Icon"},
                    { "CanvasGroup", "d_CanvasGroup Icon"},
                    { "CanvasScaler", "d_CanvasScaler Icon"},
                    { "ContentSizeFitter", "d_ContentSizeFitter Icon"},
                    { "GridLayoutGroup", "d_GridLayoutGroup Icon"},
                    { "HorizontalLayoutGroup", "d_HorizontalLayoutGroup Icon"},
                    { "LayoutElement", "d_LayoutElement Icon"},
                    { "RectTransform", "d_RectTransform Icon"},
                    { "VerticalLayoutGroup", "d_VerticalLayoutGroup Icon"},

                    // misc
                    { "AimConstraint", "d_AimConstraint Icon"},
                    { "Animation", "d_Animation Icon"},
                    { "Animator", "d_Animator Icon"},
                    { "Grid", "d_Grid Icon"},
                    { "LookAtConstraint", "d_LookAtConstraint Icon"},
                    { "ParticleSystemForceField", "d_ParticleSystemForceField Icon"},
                    { "PositionConstraint", "d_PositionConstraint Icon"},
                    { "RotationConstraint", "d_RotationConstraint Icon"},
                    { "ScaleConstraint", "d_ScaleConstraint Icon"},
                    { "SpriteMask", "d_SpriteMask Icon"},
                    { "Terrain", "d_Terrain Icon"},
                    { "WindZone", "d_WindZone Icon"},
                    { "PlayableDirector", "d_PlayableDirector Icon"},
                    { "VideoPlayer", "d_VideoPlayer Icon"},
                    { "TrackedPoseDriver", "TrackedPoseDriver Icon"},

                    // navigation
                    { "NavMeshAgent", "d_NavMeshAgent Icon"},
                    { "NavMeshObstacle", "d_NavMeshObstacle Icon"},
                    { "OffMeshLink", "d_OffMeshLink Icon"},

                    // physics 2D
                    { "AreaEffector2D", "d_AreaEffector2D Icon"},
                    { "BoxCollider2D", "d_BoxCollider2D Icon"},
                    { "BuoyancyEffector2D", "d_BuoyancyEffector2D Icon"},
                    { "CapsuleCollider2D", "d_CapsuleCollider2D Icon"},
                    { "CircleCollider2D", "d_CircleCollider2D Icon"},
                    { "CompositeCollider2D", "d_CompositeCollider2D Icon"},
                    { "ConstantForce2D", "d_ConstantForce2D Icon"},
                    { "CustomCollider2D", "d_CustomCollider2D Icon"},
                    { "DistanceJoint2D", "d_DistanceJoint2D Icon"},
                    { "EdgeCollider2D", "d_EdgeCollider2D Icon"},
                    { "FixedJoint2D", "d_FixedJoint2D Icon"},
                    { "FrictionJoint2D", "d_FrictionJoint2D Icon"},
                    { "HingeJoint2D", "d_HingeJoint2D Icon"},
                    { "PlatformEffector2D", "d_PlatformEffector2D Icon"},
                    { "PointEffector2D", "d_PointEffector2D Icon"},
                    { "PolygonCollider2D", "d_PolygonCollider2D Icon"},
                    { "RelativeJoint2D", "d_RelativeJoint2D Icon"},
                    { "Rigidbody2D", "d_Rigidbody2D Icon"},
                    { "SliderJoint2D", "d_SliderJoint2D Icon"},
                    { "SpringJoint2D", "d_SpringJoint2D Icon"},
                    { "SurfaceEffector2D", "d_SurfaceEffector2D Icon"},
                    { "TargetJoint2D", "d_TargetJoint2D Icon"},
                    { "WheelJoint2D", "d_WheelJoint2D Icon"},

                    // Physics
                    { "ArticulationBody", "d_ArticulationBody Icon"},
                    { "BoxCollider", "d_BoxCollider Icon"},
                    { "CapsuleCollider", "d_CapsuleCollider Icon"},
                    { "CharacterController", "d_CharacterController Icon"},
                    { "CharacterJoint", "d_CharacterJoint Icon"},
                    { "Cloth", "d_Cloth Icon"},
                    { "ConfigurableJoint", "d_ConfigurableJoint Icon"},
                    { "ConstantForce", "d_ConstantForce Icon"},
                    { "FixedJoint", "d_FixedJoint Icon"},
                    { "HingeJoint", "d_HingeJoint Icon"},
                    { "MeshCollider", "d_MeshCollider Icon"},
                    { "Rigidbody", "d_Rigidbody Icon"},
                    { "SphereCollider", "d_SphereCollider Icon"},
                    { "SpringJoint", "d_SpringJoint Icon"},
                    { "TerrainCollider", "d_TerrainCollider Icon"},
                    { "WheelCollider", "d_WheelCollider Icon"},

                    // Tilemap,
                    { "Tilemap", "d_Tilemap Icon"},
                    { "TilemapCollider2D", "d_TilemapCollider2D Icon"},
                    { "TilemapRenderer", "d_TilemapRenderer Icon"},

                    // ui
                    { "Button", "d_Button Icon"},
                    { "Image", "d_Image Icon"},
                    { "Mask", "d_Mask Icon"},
                    { "RawImage", "d_RawImage Icon"},
                    { "RectMask2D", "d_RectMask2D Icon"},
                    { "ScrollRect", "d_ScrollRect Icon"},
                    { "Scrollbar", "d_Scrollbar Icon"},
                    { "Selectable", "d_Selectable Icon"},
                    { "Slider", "d_Slider Icon"},
                    { "Toggle", "d_Toggle Icon"},
                    { "ToggleGroup", "d_ToggleGroup Icon"},
                    // effects
                    { "Outline", "d_Outline Icon"},
                    { "PositionAsUV1", "d_PositionAsUV1 Icon"},
                    { "Shadow", "d_Shadow Icon"},
                    { "TextMesh", ""}
                };
                return m_ComponentToTextureName;
            }
        }

        private static Dictionary<string, Texture2D> m_GizmoTextures = null;
        private static Dictionary<string, Texture2D> GizmoTextures
        {
            get
            {
                if (m_GizmoTextures == null)
                {
                    m_GizmoTextures = new Dictionary<string, Texture2D>();
                    var projectInfos = GizmoUtility.GetGizmoInfo();
                    foreach (GizmoInfo gizmoInfo in projectInfos)
                    {
                        if (!gizmoInfo.hasIcon)
                            continue;

                        Texture2D thumb = gizmoInfo.thumb;
                        m_GizmoTextures[gizmoInfo.name] = thumb;
                        m_GizmoTextures[thumb.name] = thumb;
                    }
                }

                return m_GizmoTextures;
            }
        }

        public static Texture2D GetAssetIcon(AssetType MainAssetType)
        {
            if (AssetTypeToIconName.TryGetValue(MainAssetType, out string textureName))
            {
                if (!EditorGUIUtility.isProSkin && textureName.StartsWith("d_"))
                    textureName = textureName.Substring(2);
                return EditorGUIUtility.IconContent(textureName).image as Texture2D;
            }
            else if (GizmoTextures.TryGetValue(MainAssetType + " Icon", out Texture2D t))
                return t;

            return EditorGUIUtility.IconContent("DefaultAsset Icon").image as Texture2D;
        }

        public static Texture2D GetComponentIcon(string componentName)
        {
            if (ComponentNameToIconName.TryGetValue(componentName, out string textureName))
            {
                if (!EditorGUIUtility.isProSkin && textureName.StartsWith("d_", StringComparison.Ordinal))
                    textureName = textureName.Substring(2);
            }
            else if (GizmoTextures.TryGetValue(componentName + " Icon", out Texture2D t))
                return t;

            if (!EditorGUIUtility.isProSkin && textureName.StartsWith("d_"))
                textureName = EditorGUIUtility.isProSkin ? "d_cs Script Icon" : "cs Script Icon";

            return EditorGUIUtility.IconContent(textureName).image as Texture2D;
        }

        public static void Hide(VisualElement element)
        {
            element.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        public static void Show(VisualElement element)
        {
            element.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        }
    }
}

#endif
