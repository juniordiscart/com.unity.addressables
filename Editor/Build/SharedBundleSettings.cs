namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// Determines the group whose settings used for shared bundles (Built In and MonoScript bundles).
    /// </summary>
    public enum SharedBundleSettings
    {
        /// <summary>
        /// Shared bundles uses the settings of the Default group.
        /// </summary>
        DefaultGroup,

        /// <summary>
        /// Shared bundles uses the settings of a specified group.
        /// </summary>
        CustomGroup
    }
}
