#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Generic;
using Unity.Loading;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Implemented by the global root asset embedded in a Content Directory. Exposes the
    /// scene id lookup needed by <see cref="SceneProvider"/> without depending on the
    /// concrete root asset type (which lives in the higher-level Addressables assembly).
    /// </summary>
    public interface IAddressableRootAsset
    {
        /// <summary>
        /// Key is not used for the global root asset but kept for compatibility.
        /// </summary>
        string Key { get; set; }

        /// <summary>
        /// Resolves a <see cref="LoadableSceneId"/> from the integer index stored at build time.
        /// </summary>
        /// <param name="id">The integer index produced during the build.</param>
        /// <returns>The matching <see cref="LoadableSceneId"/>, or default if out of range.</returns>
        LoadableSceneId GetLoadableSceneId(int id);
    }

    /// <summary>
    /// Mounts Content Directories on demand and shares a single mount across every asset and
    /// scene that loads from the same load path. Replaces the previous design where a dedicated
    /// ContentDirectoryProvider loaded the directory as a shared dependency.
    ///
    /// <para>
    /// A directory is mounted at most once (the first <see cref="EnsureMounted"/> for a resolved
    /// path calls <c>ContentLoadManager.RegisterContentDirectory</c>) and then stays mounted for
    /// the lifetime of the Addressables system. All mounts are released together by
    /// <see cref="UnmountAll"/>, which is called from <c>AddressablesImpl.Dispose</c>. All access
    /// is expected on the main thread.
    /// </para>
    /// </summary>
    public static class ContentDirectoryMountManager
    {
        // Mounted directories, keyed by resolved (placeholder-expanded) path so entries sharing
        // the same symbolic load path collapse onto one mount.
        static readonly Dictionary<string, ContentDirectoryHandle> s_Mounts = new Dictionary<string, ContentDirectoryHandle>();

        /// <summary>
        /// Resolves runtime placeholders (e.g. <c>{UnityEngine.AddressableAssets.Addressables.RuntimePath}</c>)
        /// in a stored load path. Defaults to identity; Addressables initialization wires this to
        /// <c>AddressablesRuntimeProperties.EvaluateString</c> so this lower-level assembly can
        /// resolve paths without referencing the Addressables assembly.
        /// </summary>
        public static Func<string, string> PathResolver = s => s;

        /// <summary>
        /// Runs the configured <see cref="PathResolver"/> over a raw, symbolic load path.
        /// </summary>
        /// <param name="rawLoadPath">The load path stored in the catalog entry data.</param>
        /// <returns>The resolved, on-disk load path.</returns>
        public static string ResolvePath(string rawLoadPath)
        {
            return PathResolver == null ? rawLoadPath : PathResolver(rawLoadPath);
        }

        /// <summary>
        /// Mounts the Content Directory at the given load path if it is not already mounted, and
        /// returns its handle. The directory remains mounted until <see cref="UnmountAll"/> is called.
        /// </summary>
        /// <param name="rawLoadPath">The symbolic load path stored in the catalog entry data.</param>
        /// <returns>A valid handle to the mounted Content Directory.</returns>
        /// <exception cref="ArgumentException">Thrown when the load path is null or empty.</exception>
        /// <exception cref="Exception">Thrown when the Content Directory fails to mount.</exception>
        public static ContentDirectoryHandle EnsureMounted(string rawLoadPath)
        {
            if (string.IsNullOrEmpty(rawLoadPath))
                throw new ArgumentException("Content Directory load path is null or empty.", nameof(rawLoadPath));

            string path = ResolvePath(rawLoadPath);
            if (s_Mounts.TryGetValue(path, out var handle))
                return handle;

            handle = ContentLoadManager.RegisterContentDirectory(path);
            if (!handle.IsValid)
                throw new Exception($"Failed to mount Content Directory at {path}.");

            s_Mounts.Add(path, handle);
            return handle;
        }

        /// <summary>
        /// Unmounts every Content Directory mounted through <see cref="EnsureMounted"/> and clears
        /// the table. Called from <c>AddressablesImpl.Dispose</c> when the Addressables system shuts down.
        /// </summary>
        public static void UnmountAll()
        {
            foreach (var kvp in s_Mounts)
                ContentLoadManager.UnregisterContentDirectory(kvp.Value);
            s_Mounts.Clear();
        }

        /// <summary>
        /// Finds the <see cref="IAddressableRootAsset"/> embedded in a mounted Content Directory.
        /// </summary>
        /// <param name="handle">A valid Content Directory handle returned by <see cref="EnsureMounted"/>.</param>
        /// <returns>The root asset, or null if the handle is invalid or no root asset is present.</returns>
        public static IAddressableRootAsset GetRootAsset(ContentDirectoryHandle handle)
        {
            if (!handle.IsValid)
                return null;

            foreach (var ra in ContentLoadManager.GetRootAssets(handle))
            {
                if (ra is IAddressableRootAsset ara)
                    return ara;
            }

            return null;
        }
    }
}
#endif
