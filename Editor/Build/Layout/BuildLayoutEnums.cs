namespace UnityEditor.AddressableAssets.Build.Layout
{
    /// <summary>
    /// UnityEngine Object types found as Assets
    /// </summary>
    public enum AssetType
    {
        /// <summary>
        /// Unknown type that is not handled
        /// </summary>
        Other = 0,

        // Other
        /// <summary>
        /// Font asset
        /// </summary>
        Font,

        /// <summary>
        /// GUISkin asset
        /// </summary>
        GUISkin,

        // Animation
        /// <summary>
        /// AnimationClip, often a subObject of a Model asset
        /// </summary>
        AnimationClip,

        /// <summary>
        /// Avatar asset
        /// </summary>
        Avatar,

        /// <summary>
        /// AnimationController asset
        /// </summary>
        AnimationController,

        // Audio
        /// <summary>
        /// AudioClip asset
        /// </summary>
        AudioClip,

        /// <summary>
        /// AudioMixer asset
        /// </summary>
        AudioMixer,

        // Video
        /// <summary>
        /// Video asset
        /// </summary>
        VideoClip,

        // Shader
        /// <summary>
        /// Shader asset
        /// </summary>
        Shader,

        /// <summary>
        /// ComputeShader asset
        /// </summary>
        ComputeShader,

        // Mesh
        /// <summary>
        /// Mesh, often a subObject of a model asset
        /// </summary>
        Mesh,

        // Texture
        /// <summary>
        /// Generic Texture asset
        /// </summary>
        Texture,

        /// <summary>
        /// 2D image texture asset
        /// </summary>
        Texture2D,

        /// <summary>
        /// Texture3D asset
        /// </summary>
        Texture3D,

        /// <summary>
        /// Sprite Object, often a subObject to a Texture or SpriteAtlas
        /// </summary>
        Sprite,

        // Scriptable Object
        /// <summary>
        /// ScriptableObject asset
        /// </summary>
        ScriptableObject,

        // Prefab
        /// <summary>
        /// Prefab asset
        /// </summary>
        Prefab,

        /// <summary>
        /// Special prefab type for Imported model assets
        /// </summary>
        Model,

        // Material
        /// <summary>
        /// Rendering Material asset
        /// </summary>
        Material,

        /// <summary>
        /// PhysicsMaterial asset
        /// </summary>
        PhysicsMaterial,

        /// <summary>
        /// PhysicalMaterial2D asset
        /// </summary>
        PhysicsMaterial2D,

        // Other Assets
        /// <summary>
        /// TextAsset
        /// </summary>
        TextAsset,

        // Scene
        /// <summary>
        /// Scene asset
        /// </summary>
        Scene,

        // Serialize Content -> combined into Scene, Prefab, Scriptable Object
        /// <summary>
        /// GameObject, can be a Prefab or Scene subObject
        /// </summary>
        GameObject,

        /// <summary>
        /// Generic Scene Object that has an undefined AssetType
        /// </summary>
        SceneObject,

        /// <summary>
        /// MonoBehaviour scripts
        /// </summary>
        MonoBehaviour,

        /// <summary>
        /// Components on a GameObject not of MonoBehaviour type
        /// </summary>
        Component,

        /// <summary>
        /// MonoScript object
        /// </summary>
        MonoScript,

        // Scene Objects that are parsed from string by the scene object type path
        /// <summary>
        /// Cubemap scene Object
        /// </summary>
        Cubemap,

        /// <summary>
        /// Scene Camera component
        /// </summary>
        Camera,

        /// <summary>
        /// Scene AudioListener component
        /// </summary>
        AudioListener,

        /// <summary>
        /// Scene Light component
        /// </summary>
        Light,

        /// <summary>
        /// Scene NavMeshSettings Object
        /// </summary>
        NavMeshSettings,

        /// <summary>
        /// Scene RenderSettings Object
        /// </summary>
        RenderSettings,

        /// <summary>
        /// Scene LightmapSettings Object
        /// </summary>
        LightmapSettings,

        /// <summary>
        /// Scene Transform component
        /// </summary>
        Transform,

        /// <summary>
        /// Scene MeshRenderer component
        /// </summary>
        MeshRenderer,

        /// <summary>
        /// Scene MeshFilter component
        /// </summary>
        MeshFilter,

        /// <summary>
        /// Scene BoxCollider2D component
        /// </summary>
        BoxCollider2D,

        /// <summary>
        /// Scene BoxCollider component
        /// </summary>
        BoxCollider,

        /// <summary>
        /// Scene SphereCollider component
        /// </summary>
        SphereCollider,
    }

    /// <summary>
    /// Type of Addressables build
    /// </summary>
    public enum BuildType
    {
        /// <summary>
        /// Was made with an Addressables build made for new Player builds
        /// </summary>
        NewBuild = 0,

        /// <summary>
        /// Was made with an Addressables update build, for a previous new build
        /// </summary>
        UpdateBuild
    }

    /// <summary>
    /// Bundle status after an update build
    /// </summary>
    public enum BundleBuildStatus
    {
        /// <summary>
        /// Asset bundle is newly created for this build
        /// </summary>
        New = 0,

        /// <summary>
        /// Asset bundle has been modified (Remote bundle expected)
        /// </summary>
        Modified,

        /// <summary>
        /// Prevent updates, updated Asset bundle has been modified and reverted to previous details
        /// </summary>
        ModifiedUpdatePrevented,

        /// <summary>
        /// Asset bundle was not modified and data remains the same
        /// </summary>
        Unmodified
    }
}
