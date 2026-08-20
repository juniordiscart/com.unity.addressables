#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.AddressableAssets.ResourceProviders
{
    /// <summary>
    /// Specialized provider for bare-key <c>IList&lt;T&gt;</c> requests on multi-sub-asset
    /// entries (e.g. a Texture2D with multiple Sprites). Inherits all list-loading logic
    /// from <see cref="NativeContentAssetEntryProvider"/> but restricts
    /// <see cref="CanProvide"/> to generic <c>IList&lt;&gt;</c> requests.
    ///
    /// <para>
    /// This distinct provider type is necessary to avoid a conflict with
    /// <see cref="DynamicResourceLocator"/>: when a bare-key Sprite-typed location exists,
    /// <c>DynamicResourceLocator</c> intercepts sub-key single-sprite loads and synthesises
    /// a child location carrying the parent's <c>ContentDirectoryAssetData</c> (including
    /// <c>SubAssetIds</c>). By giving the list location a different <c>ProviderId</c>,
    /// <c>CanProvide(Sprite, …)</c> returns <c>false</c> for the child location, the
    /// <c>ResourceManager</c> provider lookup returns <c>null</c>, and the outer locator
    /// loop falls through to the catalog's direct sub-key entry — ensuring the correct
    /// per-sprite <c>AssetId</c> is used.
    /// </para>
    ///
    /// <para>
    /// A <c>CanProvide</c> override on <see cref="NativeContentAssetEntryProvider"/> itself
    /// would be unsafe: <c>ResourceManager.GetResourceProvider</c> caches results by
    /// <c>(ProviderId, requestedType)</c> only, so a location-sensitive override would be
    /// memoized incorrectly across calls sharing the same provider id.
    /// </para>
    /// </summary>
    public class NativeContentAssetListProvider : NativeContentAssetEntryProvider
    {
        /// <inheritdoc/>
        /// <remarks>
        /// Only accepts generic <c>IList&lt;T&gt;</c> requests. Single-asset requests
        /// (<c>typeof(Sprite)</c>, <c>typeof(Texture2D)</c>, etc.) return <c>false</c>
        /// so that dynamically synthesised single-item locations are skipped.
        /// </remarks>
        public override bool CanProvide(Type t, IResourceLocation location)
            => t.IsGenericType && typeof(IList<>) == t.GetGenericTypeDefinition();
    }
}
#endif
