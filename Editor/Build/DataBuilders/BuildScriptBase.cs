using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_2022_2_OR_NEWER
using UnityEditor.AddressableAssets.BuildReportVisualizer;
#endif
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;
using System.Reflection;


namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Base class for build script assets
    /// </summary>
    public class BuildScriptBase : ScriptableObject, IDataBuilder
    {
        /// <summary>
        /// Static part of the builtin bundle filename.
        /// </summary>
        public const string BuiltInBundleBaseName = "_unitybuiltinassets";

        /// <summary>
        /// The type of instance provider to create for the Addressables system.
        /// </summary>
        [FormerlySerializedAs("m_InstanceProviderType")]
        [SerializedTypeRestrictionAttribute(type = typeof(IInstanceProvider))]
        public SerializedType instanceProviderType = new SerializedType() { Value = typeof(InstanceProvider) };

        /// <summary>
        /// The type of scene provider to create for the addressables system.
        /// </summary>
        [FormerlySerializedAs("m_SceneProviderType")]
        [SerializedTypeRestrictionAttribute(type = typeof(ISceneProvider))]
        public SerializedType sceneProviderType = new SerializedType() { Value = typeof(SceneProvider) };

        /// <summary>
        /// Stores the logged information of all the build tasks.
        /// </summary>
        public IBuildLogger Log
        {
            get { return m_Log; }
        }

        [NonSerialized]
        internal IBuildLogger m_Log;

        /// <summary>
        /// The descriptive name used in the UI.
        /// </summary>
        public virtual string Name
        {
            get { return "Undefined"; }
        }

        internal static void WriteBuildLog(BuildLog log, string directory)
        {
            Directory.CreateDirectory(directory);
            PackageManager.PackageInfo info = PackageManager.PackageInfo.FindForAssembly(typeof(BuildScriptBase).Assembly);
            log.AddMetaData(info.name, info.version);
            File.WriteAllText(Path.Combine(directory, "AddressablesBuildTEP.json"), log.FormatForTraceEventProfiler());
        }

        /// <summary>
        /// Build the specified data with the provided builderInput.  This is the public entry point.
        ///  Child class overrides should use <see cref="BuildDataImplementation{TResult}"/>
        /// </summary>
        /// <typeparam name="TResult">The type of data to build.</typeparam>
        /// <param name="builderInput">The builderInput object used in the build.</param>
        /// <returns>The build data result.</returns>
        public TResult BuildData<TResult>(AddressablesDataBuilderInput builderInput) where TResult : IDataBuilderResult
        {
            if (!CanBuildData<TResult>())
            {
                var message = "Data builder " + Name + " cannot build requested type: " + typeof(TResult);
                Debug.LogError(message);
                return AddressableAssetBuildResult.CreateResult<TResult>(null, 0, message);
            }

            AddressableAnalytics.BuildType buildType = AddressableAnalytics.DetermineBuildType();
            m_Log = (builderInput.Logger != null) ? builderInput.Logger : new BuildLog();

            AddressablesRuntimeProperties.ClearCachedPropertyValues();

            TResult result = default;
            // Append the file registry to the results
            using (m_Log.ScopedStep(LogLevel.Info, $"Building {this.Name}"))
            {
                try
                {
                    result = BuildDataImplementation<TResult>(builderInput);
                }
                catch (Exception e)
                {
                    string errMessage;
                    if (e.Message == "path")
                        errMessage = "Invalid path detected during build. Check for unmatched brackets in your active profile's variables.";
                    else
                        errMessage = e.Message;

                    Debug.LogError(errMessage);
                    return AddressableAssetBuildResult.CreateResult<TResult>(null, 0, errMessage);
                }

                if (result != null)
                    result.FileRegistry = builderInput.Registry;
            }

            if (builderInput.Logger == null && m_Log != null)
                WriteBuildLog((BuildLog)m_Log, Path.GetDirectoryName(Application.dataPath) + "/" + Addressables.LibraryPath);

            if (result is AddressableAssetBuildResult)
            {
                AddressableAnalytics.ReportBuildEvent(builderInput, result as AddressableAssetBuildResult, buildType);
            }

            return result;
        }

        /// <summary>
        /// The implementation of <see cref="BuildData{TResult}"/>.  That is the public entry point,
        ///  this is the home for child class overrides.
        /// </summary>
        /// <param name="builderInput">The builderInput object used in the build</param>
        /// <typeparam name="TResult">The type of data to build</typeparam>
        /// <returns>The build data result</returns>
        protected virtual TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput) where TResult : IDataBuilderResult
        {
            return default(TResult);
        }

        /// <summary>
        /// Loops over each group, after doing some data checking.
        /// </summary>
        /// <param name="aaContext">The Addressables builderInput object to base the group processing on</param>
        /// <returns>An error string if there were any problems processing the groups</returns>
        protected virtual string ProcessAllGroups(AddressableAssetsBuildContext aaContext)
        {
            try
            {
                if (aaContext == null ||
                    aaContext.Settings == null ||
                    aaContext.Settings.groups == null)
                {
                    return "No groups found to process in build script " + Name;
                }

                //intentionally for not foreach so groups can be added mid-loop.
                for (int index = 0; index < aaContext.Settings.groups.Count; index++)
                {
                    AddressableAssetGroup assetGroup = aaContext.Settings.groups[index];
                    if (assetGroup == null)
                        continue;

                    var error = ErrorCheckBundleSettings(assetGroup, aaContext);
                    if (error != string.Empty)
                    {
                        return error;
                    }

                    EditorUtility.DisplayProgressBar($"Processing Addressable Group", assetGroup.Name, (float)index / aaContext.Settings.groups.Count);
                    var errorString = ProcessGroup(assetGroup, aaContext);
                    if (!string.IsNullOrEmpty(errorString))
                    {
                        return errorString;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return string.Empty;
        }


        internal static string ErrorCheckBundleSettings(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            if (!assetGroup.HasSchema<BundledAssetGroupSchema>())
                return string.Empty;

            var message = string.Empty;
            var settings = aaContext.Settings;
            var schema = assetGroup.GetSchema<BundledAssetGroupSchema>();

            if(settings.UseUnityWebRequestForLocalBundles && schema.StripDownloadOptions)
            {
                message = "Strip Download Options is enabled, but Use UnityWebRequest for Local Bundles is also enabled. " +
                          "These options are mutually exclusive and cannot be used together.";
            }

            string buildPath = settings.profileSettings.GetValueById(settings.activeProfileId, schema.BuildPath.Id);
            string loadPath = settings.profileSettings.GetValueById(settings.activeProfileId, schema.LoadPath.Id);

            bool buildLocal = AddressableAssetUtility.StringContains(buildPath, "[UnityEngine.AddressableAssets.Addressables.BuildPath]", StringComparison.Ordinal);
            bool loadLocal = AddressableAssetUtility.StringContains(loadPath, "{UnityEngine.AddressableAssets.Addressables.RuntimePath}", StringComparison.Ordinal);

            if (buildLocal && !loadLocal)
            {
                message = "BuildPath for group '" + assetGroup.Name + "' is set to the dynamic-lookup version of StreamingAssets, but LoadPath is not. \n";
            }
            else if (!buildLocal && loadLocal)
            {
                message = "LoadPath for group " + assetGroup.Name +
                          " is set to the dynamic-lookup version of StreamingAssets, but BuildPath is not. These paths must both use the dynamic-lookup, or both not use it. \n";
            }

            if (!string.IsNullOrEmpty(message))
            {
                message += "BuildPath: '" + buildPath + "'\n";
                message += "LoadPath: '" + loadPath + "'";
            }

            if (schema.Compression == BundledAssetGroupSchema.BundleCompressionMode.LZMA && (buildLocal || loadLocal))
            {
                Debug.LogWarningFormat("Bundle compression is set to LZMA, but group {0} uses local content.", assetGroup.Name);
            }

            return message;
        }


        /// <summary>
        /// Build processing of an individual group.
        /// </summary>
        /// <param name="assetGroup">The group to process</param>
        /// <param name="aaContext">The Addressables builderInput object to base the group processing on</param>
        /// <returns>An error string if there were any problems processing the groups</returns>
        protected virtual string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            return string.Empty;
        }

        /// <summary>
        /// Used to determine if this builder is capable of building a specific type of data.
        /// </summary>
        /// <typeparam name="T">The type of data needed to be built.</typeparam>
        /// <returns>True if this builder can build this data.</returns>
        public virtual bool CanBuildData<T>() where T : IDataBuilderResult
        {
            return false;
        }

        /// <summary>
        /// Utility method for deleting files.
        /// </summary>
        /// <param name="path">The file path to delete.</param>
        protected static void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Utility method to write a file.  The directory will be created if it does not exist.
        /// </summary>
        /// <param name="path">The path of the file to write.</param>
        /// <param name="content">The content of the file.</param>
        /// <param name="registry">The file registry used to track all produced artifacts.</param>
        /// <returns>True if the file was written.</returns>
        protected internal static bool WriteFile(string path, byte[] content, FileRegistry registry)
        {
            try
            {
                registry.AddFile(path);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, content);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                registry.RemoveFile(path);
                return false;
            }
        }

        /// <summary>
        /// Utility method to write a file.  The directory will be created if it does not exist.
        /// </summary>
        /// <param name="path">The path of the file to write.</param>
        /// <param name="content">The content of the file.</param>
        /// <param name="registry">The file registry used to track all produced artifacts.</param>
        /// <returns>True if the file was written.</returns>
        protected static bool WriteFile(string path, string content, FileRegistry registry)
        {
            try
            {
                registry.AddFile(path);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, content);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                registry.RemoveFile(path);
                return false;
            }
        }

        /// <summary>
        /// Used to clean up any cached data created by this builder.
        /// </summary>
        public virtual void ClearCachedData()
        {
        }

        /// <summary>
        /// Checks to see if the data is built for the given builder.
        /// </summary>
        /// <returns>Returns true if the data is built. Returns false otherwise.</returns>
        public virtual bool IsDataBuilt()
        {
            return false;
        }


        /// <summary>
        /// Copies the content state binary file from the temp directory to its final location and registers it in the
        /// file registry and build results.
        /// </summary>
        /// <param name="tempPath">Temporary location of the content state file.</param>
        /// <param name="contentStatePath">Destination location of the content state file.</param>
        /// <param name="builderInput">The builderInput object used in the build.</param>
        /// <param name="addrResult">The build data result.</param>
        public virtual void CopyAndRegisterContentState(string tempPath, string contentStatePath, AddressablesDataBuilderInput builderInput, AddressablesPlayerBuildResult addrResult)
        {
            try
            {
                string directory = Path.GetDirectoryName(contentStatePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                if (File.Exists(contentStatePath))
                    File.Delete(contentStatePath);

                File.Copy(tempPath, contentStatePath, true);
                if (addrResult != null)
                    addrResult.ContentStateFilePath = contentStatePath;
                builderInput.Registry.AddFile(contentStatePath);
            }
            catch (UnauthorizedAccessException uae)
            {
                if (!AddressableAssetUtility.IsVCAssetOpenForEdit(contentStatePath))
                    Debug.LogErrorFormat("Cannot access the file {0}. It may be locked by version control.",
                        contentStatePath);
                else
                    Debug.LogException(uae);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Notifies the user about the existence of the Addressables Report
        /// </summary>
        protected virtual void NotifyUserAboutBuildReport()
        {
            bool buildReportSettingCheck = ProjectConfigData.UserHasBeenInformedAboutBuildReportSettingPreBuild;
            if (!buildReportSettingCheck && !Application.isBatchMode && !ProjectConfigData.GenerateBuildLayout)
            {
                bool turnOnBuildLayout = EditorUtility.DisplayDialog("Addressables Build Report",
                    "There's a new Addressables Build Report you can check out after your content build.  " +
                    "However, this requires that 'Debug Build Layout' is turned on.  The setting can be found in Edit > Preferences > Addressables.  Would you like to turn it on?",
                    "Yes", "No");
                if (turnOnBuildLayout)
                    ProjectConfigData.GenerateBuildLayout = true;
                ProjectConfigData.UserHasBeenInformedAboutBuildReportSettingPreBuild = true;
            }
        }

        /// <summary>
        /// Displays the Addressables Report window
        /// </summary>
        protected virtual void DisplayBuildReport()
        {
            if (!Application.isBatchMode && ProjectConfigData.AutoOpenAddressablesReport && ProjectConfigData.GenerateBuildLayout)
            {
                BuildReportWindow.ShowWindowAfterBuild();
            }
        }

        /// <summary>
        /// Clears content update notifications from teh groups window
        /// </summary>
        /// <param name="groups">A list of groups that were built</param>
        protected virtual void ClearContentUpdateNotifications(List<AddressableAssetGroup> groups)
        {
            foreach (var group in groups)
                ContentUpdateScript.ClearContentUpdateNotifications(group);
        }
    }
}
