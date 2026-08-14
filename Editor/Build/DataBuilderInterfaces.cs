using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// The result of IDataBuilder.Build.
    /// </summary>
    public interface IDataBuilderResult
    {
        /// <summary>
        /// Duration of the build in seconds.
        /// </summary>
        double Duration { get; set; }

        /// <summary>
        /// The number of addressable assets contained in the build.
        /// </summary>
        int LocationCount { get; set; }

        /// <summary>
        /// Error string, if any.  If Succeeded is true, this may be null.
        /// </summary>
        string Error { get; set; }

        /// <summary>
        /// Path of runtime settings file
        /// </summary>
        string OutputPath { get; set; }

        /// <summary>
        /// Registry of files created during the build
        /// </summary>
        FileRegistry FileRegistry { get; set; }
    }

    /// <summary>
    /// Builds objects of type IDataBuilderResult.
    /// </summary>
    public interface IDataBuilder
    {
        /// <summary>
        /// The name of the builder, used for GUI.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Can this builder build the type of data requested.
        /// </summary>
        /// <typeparam name="T">The data type.</typeparam>
        /// <returns>True if the build can build it.</returns>
        bool CanBuildData<T>() where T : IDataBuilderResult;

        /// <summary>
        /// Build the data of a specific type.
        /// </summary>
        /// <typeparam name="TResult">The data type.</typeparam>
        /// <param name="builderInput">The builderInput used to build the data.</param>
        /// <returns>The built data.</returns>
        TResult BuildData<TResult>(AddressablesDataBuilderInput builderInput) where TResult : IDataBuilderResult;

        /// <summary>
        /// Clears all cached data.
        /// </summary>
        void ClearCachedData();
    }

    /// <summary>
    /// Interface for schema builders that process specific AddressableAssetGroupSchema types during a build.
    /// Implementations handle schema-specific build logic such as AssetBundle creation or Content Directory generation.
    /// </summary>
    public interface ISchemaBuilder
    {

        /// <summary>
        /// The name of the schema builder, used for GUI.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Determines whether this schema builder can process the given schema.
        /// </summary>
        /// <param name="schema">The schema to check.</param>
        /// <returns>True if this builder can process the schema; otherwise, false.</returns>
        bool CanBuildSchema(AddressableAssetGroupSchema schema);

        /// <summary>
        /// Is data built for this schema. Used for incremental builds and entering play mode.
        /// </summary>
        /// <returns>True if data is built.</returns>
        bool IsDataBuilt();

        /// <summary>
        /// Initialize the schema builder.
        /// </summary>
        /// <param name="aaContext">The Addressable Asset context used to build the schema data.</param>
        /// <param name="dataBuilder">A reference to the parent data builder script.</param>
        void Init(AddressableAssetsBuildContext aaContext, IDataBuilder dataBuilder);

        /// <summary>
        /// Validates the schema instance for a group and collects information on how to build data.
        /// </summary>
        /// <param name="schema">The schema to verify.</param>
        /// <param name="assetGroup">The asset group that owns the schema.</param>
        /// <param name="aaContext">The Addressable Asset context used to build the schema data.</param>
        /// <returns>A message with an error string for the user, empty if successful.</returns>
        string ProcessGroupSchema(AddressableAssetGroupSchema schema, AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext);

        /// <summary>
        /// Build data for the schema.
        /// </summary>
        /// <param name="buildContext">The build context containing build pipeline state and data.</param>
        /// <param name="builderInput">The builderInput used to build the schema data.</param>
        /// <param name="aaContext">The Addressable Asset context used to build the schema data.</param>
        /// <param name="extractData">The ExtractData task used to store data between tasks.</param>
        /// <param name="cachedState">The cached asset state from a content update state file.</param>
        /// <param name="addrResult">The Addressables result.</param>
        void Build(BuildContext buildContext,
            AddressablesDataBuilderInput builderInput,
            AddressableAssetsBuildContext aaContext,
            ExtractDataTask extractData,
            List<CachedAssetState> cachedState,
            AddressablesPlayerBuildResult addrResult);

        /// <summary>
        /// Generate type stripping information for the player build.
        /// </summary>
        /// <param name="builderInput">The builderInput used to build the schema data.</param>
        /// <param name="aaContext">The Addressable Asset context used to build the schema data.</param>
        /// <param name="contentCatalog">The content catalog generated by GenerateCatalog.</param>
        void GenerateTypeStrippingInfo(AddressablesDataBuilderInput builderInput,
            AddressableAssetsBuildContext aaContext,
            ContentCatalogData contentCatalog);

        /// <summary>
        /// Generate a content catalog for the schema.
        /// </summary>
        /// <param name="builderInput">The builderInput used to build the schema data.</param>
        /// <param name="aaContext">The Addressable Asset context used to build the schema data.</param>
        /// <param name="addrResult">The Addressables result.</param>
        /// <returns>A list of content catalogs generated by the schema.</returns>
        List<ContentCatalogData> GenerateCatalogs(AddressablesDataBuilderInput builderInput,
            AddressableAssetsBuildContext aaContext,
            AddressablesPlayerBuildResult addrResult);

        /// <summary>
        /// Handle content update logic for the schema.
        /// </summary>
        /// <param name="builderInput">The builderInput used to build the schema data.</param>
        /// <param name="aaContext">The Addressable Asset context used to build the schema data.</param>
        /// <param name="extractData">The ExtractData task used to store data between tasks.</param>
        /// <param name="cachedState">The cached asset state from a content update state file.</param>
        /// <param name="addrResult">The Addressables result.</param>
        void GenerateContentUpdate(AddressablesDataBuilderInput builderInput,
            AddressableAssetsBuildContext aaContext,
            ExtractDataTask extractData,
            List<CachedAssetState> cachedState,
            AddressablesPlayerBuildResult addrResult);
    }

}
