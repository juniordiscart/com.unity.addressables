namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    internal interface ICanIncludeFolderKeys
    {
        /// <summary>
        /// Gets or sets whether each addressable folder's own address is included as an extra
        /// shared key on every asset within that folder. This allows loading every asset in an
        /// addressable folder with a single call, for example
        /// Addressables.LoadAssetsAsync(folderAddress, ...), similar to Resources.LoadAll.
        /// </summary>
        public bool IncludeFolderKeysInCatalog { get; set; }

        /// <summary>
        /// Gets or sets whether assets inside an addressable folder keep their own individual
        /// address in the catalog, in addition to the folder's shared key (see
        /// IncludeFolderKeysInCatalog). GUIDs are unaffected. Only takes effect when
        /// IncludeFolderKeysInCatalog is enabled. Disable to reduce catalog size when assets
        /// are always loaded via the folder key.
        /// </summary>
        public bool IncludeAddressesForFolderChildren { get; set; }
    }
}
