namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// Naming conventions for the monoscript bundle name prefix.
    /// </summary>
    public enum MonoScriptBundleNaming
    {
        /// <summary>
        /// Set the monoscript bundle name prefix to the hash of the project name.
        /// </summary>
        ProjectName,

        /// <summary>
        /// Set the monoscript bundle name prefix to the guid of the default group.
        /// </summary>
        DefaultGroupGuid,

        /// <summary>
        /// Set the monoscript bundle name prefix to the user specified value.
        /// </summary>
        Custom
    }
}
