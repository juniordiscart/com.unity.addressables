using System.IO;

namespace UnityEngine.ResourceManagement.Util
{
    /// <summary>
    /// Utility methods for working with catalog and catalog hash file paths.
    /// </summary>
    public class CatalogUtilities
    {
        /// <summary>
        /// Gets the file extension of a catalog path, ignoring any URL query string.
        /// </summary>
        /// <param name="catalogPath">The catalog path or URL.</param>
        /// <returns>The file extension of the catalog path (including the leading dot), or an empty string if it has none.</returns>
        public static string GetCatalogExtension(string catalogPath)
        {
            // remove a query string if this is a URL
            var pathForExt = ResourceManagerConfig.StripQueryParameters(catalogPath);
            return Path.GetExtension(pathForExt);
        }

        /// <summary>
        /// Given a path to a <c>.hash</c> file, returns the corresponding catalog path with
        /// <paramref name="catalogExtension"/> (e.g. <c>.bin</c> or <c>.json</c>).
        /// Only the file extension is replaced, so a <c>.hash</c> that appears elsewhere
        /// in the path (e.g. a folder name) is left untouched.  Any URL query string is
        /// preserved correctly.
        /// </summary>
        /// <param name="hashPath">The path or URL of the <c>.hash</c> file.</param>
        /// <param name="catalogExtension">The catalog file extension to apply (e.g. <c>.bin</c> or <c>.json</c>).</param>
        /// <returns>The catalog path corresponding to the given hash path.</returns>
        public static string GetCatalogFilePath(string hashPath, string catalogExtension)
        {
            return ChangeExtensionPreservingQuery(hashPath, catalogExtension);
        }

        /// <summary>
        /// Given a path to a catalog file, returns the corresponding <c>.hash</c> file path.
        /// Only the file extension is replaced; any URL query string is preserved.
        /// </summary>
        /// <param name="catalogPath">The path or URL of the catalog file.</param>
        /// <returns>The hash file path corresponding to the given catalog path.</returns>
        public static string GetHashFilePath(string catalogPath)
        {
            return ChangeExtensionPreservingQuery(catalogPath, ".hash");
        }

        /// <summary>
        /// Changes the file extension on the path portion of <paramref name="path"/> to
        /// <paramref name="newExtension"/>, then re-appends any URL query string.
        /// <para>
        /// Using <see cref="Path.ChangeExtension"/> directly on a full URL fails when the
        /// query string contains <c>:</c> (e.g. <c>value2:date=number</c>), because
        /// <see cref="Path.ChangeExtension"/> treats <c>:</c> as a volume separator and
        /// appends the new extension to the whole URL instead of replacing the file extension.
        /// </para>
        /// </summary>
        static string ChangeExtensionPreservingQuery(string path, string newExtension)
        {
            var pathForExt = ResourceManagerConfig.StripQueryParameters(path);
            var query = path.Substring(pathForExt.Length);
            return Path.ChangeExtension(pathForExt, newExtension) + query;
        }

    }
}
