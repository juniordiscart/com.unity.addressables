namespace UnityEngine.ResourceManagement.Util
{
    /// <summary>
    /// Options for where content can be loaded from.
    /// </summary>
    public enum LoadType
    {
        /// <summary>
        /// Cannot determine where the content is located.
        /// </summary>
        None,

        /// <summary>
        /// Load the content from a local file location.
        /// </summary>
        Local,

        /// <summary>
        /// Download the content from a web server.
        /// </summary>
        Web
    }
}
