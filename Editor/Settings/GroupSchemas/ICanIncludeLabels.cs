namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    internal interface ICanIncludeLabels
    {
        /// <summary>
        /// Gets or sets whether labels are included as keys in the content catalog for entries
        /// using this schema. This is required if labels are used at runtime to load assets.
        /// </summary>
        public bool IncludeLabelsInCatalog { get; set; }
    }
}
