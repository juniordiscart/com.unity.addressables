using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline;
using UnityEngine;
using UnityEditor.Build.Content;
using BuildCompression = UnityEngine.BuildCompression;
using UnityEditor.Build.Player;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Custom bundle parameter container that provides custom compression settings per bundle.
    /// </summary>
    public class AddressableAssetsBundleBuildParameters : BundleBuildParameters
    {
        internal const string k_AddressablesAddDefines = "ADDRESSABLES_ADD_DEFINES";
        Dictionary<string, string> m_bundleToAssetGroup;
        AddressableAssetSettings m_settings;

        string[] m_ExtraScriptingDefines;

        /// <summary>
        /// Create a AddressableAssetsBundleBuildParameters with data needed to determine the correct compression per bundle.
        /// </summary>
        /// <param name="aaSettings">The AddressableAssetSettings object to use for retrieving groups.</param>
        /// <param name="bundleToAssetGroup">Mapping of bundle identifier to guid of asset groups.</param>
        /// <param name="target">The build target.  This is used by the BundleBuildParameters base class.</param>
        /// <param name="group">The build target group. This is used by the BundleBuildParameters base class.</param>
        /// <param name="outputFolder">The path for the output folder. This is used by the BundleBuildParameters base class.</param>
        public AddressableAssetsBundleBuildParameters(AddressableAssetSettings aaSettings, Dictionary<string, string> bundleToAssetGroup, BuildTarget target, BuildTargetGroup group,
            string outputFolder) : base(target, group, outputFolder)
        {
            UseCache = true;
            ContiguousBundles = aaSettings.ContiguousBundles;
#if NONRECURSIVE_DEPENDENCY_DATA
            NonRecursiveDependencies = aaSettings.NonRecursiveBuilding;
#endif
            DisableVisibleSubAssetRepresentations = aaSettings.DisableVisibleSubAssetRepresentations;

            m_settings = aaSettings;
            m_bundleToAssetGroup = bundleToAssetGroup;

            //If default group has BundledAssetGroupSchema use the compression there otherwise check if the target is webgl or not and try set the compression accordingly
            if (m_settings.DefaultGroup.HasSchema<BundledAssetGroupSchema>())
                BundleCompression = ConverBundleCompressiontToBuildCompression(m_settings.DefaultGroup.GetSchema<BundledAssetGroupSchema>().Compression);
            else
                BundleCompression = target == BuildTarget.WebGL ? BuildCompression.LZ4Runtime : BuildCompression.LZMA;

            if (aaSettings.StripUnityVersionFromBundleBuild)
                ContentBuildFlags |= ContentBuildFlags.StripUnityVersion;
        }

        /// <summary>
        /// Create a AddressableAssetsBundleBuildParameters with data needed to determine the correct compression per bundle.
        /// </summary>
        /// <param name="aaSettings">The AddressableAssetSettings object to use for retrieving groups.</param>
        /// <param name="bundleToAssetGroup">Mapping of bundle identifier to guid of asset groups.</param>
        /// <param name="builderInput">Builder input to pass in the target and group as well as extra scripting defines.</param>
        /// <param name="outputFolder">The path for the output folder. This is used by the BundleBuildParameters base class.</param>
        public AddressableAssetsBundleBuildParameters(AddressableAssetSettings aaSettings, Dictionary<string, string> bundleToAssetGroup, AddressablesDataBuilderInput builderInput,
            string outputFolder) : this(aaSettings, bundleToAssetGroup, builderInput.Target, builderInput.TargetGroup, outputFolder)
        {
            // If ADDRESSABLES_ADD_DEFINES is in the passed-in extra scripting defines, apply development build and extra scripting defines
            bool addDefines = builderInput.ExtraScriptingDefines != null &&
                System.Array.IndexOf(builderInput.ExtraScriptingDefines, k_AddressablesAddDefines) >= 0;
            if (!addDefines)
                return;

            if (builderInput.DevelopmentBuild)
            {
                 ScriptOptions |= ScriptCompilationOptions.DevelopmentBuild;
            }
            if (builderInput.ExtraScriptingDefines != null)
            {
                m_ExtraScriptingDefines = StripEditorScriptingDefines(builderInput.ExtraScriptingDefines);
            }
        }

        private BuildCompression ConverBundleCompressiontToBuildCompression(
            BundledAssetGroupSchema.BundleCompressionMode compressionMode)
        {
            BuildCompression compresion = BuildCompression.LZMA;
            switch (compressionMode)
            {
                case BundledAssetGroupSchema.BundleCompressionMode.LZMA:
                    break;
                case BundledAssetGroupSchema.BundleCompressionMode.LZ4:
                    compresion = BuildCompression.LZ4;
                    break;
                case BundledAssetGroupSchema.BundleCompressionMode.Uncompressed:
                    compresion = BuildCompression.Uncompressed;
                    break;
            }

            return compresion;
        }

        /// <summary>
        /// Get the compressions settings for the specified asset bundle.
        /// </summary>
        /// <param name="identifier">The identifier of the asset bundle.</param>
        /// <returns>The compression setting for the asset group.  If the group is not found, the default compression is used.</returns>
        public override BuildCompression GetCompressionForIdentifier(string identifier)
        {
            string groupGuid;
            if (m_bundleToAssetGroup.TryGetValue(identifier, out groupGuid))
            {
                var group = m_settings.FindGroup(g => g != null && g.Guid == groupGuid);
                if (group != null)
                {
                    var abSchema = group.GetSchema<BundledAssetGroupSchema>();
                    if (abSchema != null)
                        return abSchema.GetBuildCompressionForBundle(identifier);
                    else
                        Debug.LogWarningFormat("Bundle group {0} does not have BundledAssetGroupSchema.", group.name);
                }
                else
                {
                    Debug.LogWarningFormat("Unable to find group with guid {0}", groupGuid);
                }
            }

            return base.GetCompressionForIdentifier(identifier);
        }

        /// <inheritdoc />
        public override ScriptCompilationSettings GetScriptCompilationSettings()
        {
            return new ScriptCompilationSettings
            {
                group = Group,
                target = Target,
                options = ScriptOptions,
                extraScriptingDefines = m_ExtraScriptingDefines
            };
        }

        /// <summary>
        /// Strips UNITY_EDITOR scripting defines from the provided array.
        /// These defines should not be passed to bundle builds since bundles are compiled without Editor code.
        /// </summary>
        /// <param name="defines">The array of scripting defines to filter.</param>
        /// <returns>A new array with UNITY_EDITOR defines removed, or null if the result would be empty.</returns>
        static string[] StripEditorScriptingDefines(string[] defines)
        {
            if (defines == null || defines.Length == 0)
                return defines;

            var filteredDefines = new List<string>();
            foreach (var define in defines)
            {
                if (!define.StartsWith("UNITY_EDITOR"))
                    filteredDefines.Add(define);
            }

            return filteredDefines.Count > 0 ? filteredDefines.ToArray() : null;
        }

    }
}
