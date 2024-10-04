using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// Maintains Addresssables build data when processing a player build.
/// </summary>
public class AddressablesPlayerBuildProcessor : BuildPlayerProcessor
{
    /// <summary>
    /// Functor to override Addressables build when building Player.
    /// </summary>
    /// <remarks>
    /// Functor is invoked where Addressables settings state to build Addressables content when performing a Player build.
    ///
    /// Available in Unity 2021.2 or later.
    /// </remarks>
    public static Func<AddressableAssetSettings, AddressablesPlayerBuildResult> BuildAddressablesOverride { get; set; }

    /// <summary>
    /// Returns the player build processor callback order.
    /// </summary>
    public override int callbackOrder
    {
        get { return 1; }
    }

    [InitializeOnLoadMethod]
    private static void CleanTemporaryPlayerBuildData()
    {
        RemovePlayerBuildLinkXML(AddressableAssetSettingsDefaultObject.Settings);
    }

    internal static void RemovePlayerBuildLinkXML(AddressableAssetSettings settings)
    {
        string linkProjectPath = GetLinkPath(settings, false);
        string guid = AssetDatabase.AssetPathToGUID(linkProjectPath);
        if (!string.IsNullOrEmpty(guid))
            AssetDatabase.DeleteAsset(linkProjectPath);
        else if (File.Exists(linkProjectPath))
            File.Delete(linkProjectPath);

        DirectoryUtility.DeleteDirectory(Path.GetDirectoryName(linkProjectPath));
    }

    private static string GetLinkPath(AddressableAssetSettings settings, bool createFolder)
    {
        string folderPath;
        if (settings == null)
            folderPath = "Assets/Addressables_Temp";
        else
            folderPath = settings.ConfigFolder;

        if (createFolder && !Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.ImportAsset(folderPath);
        }

        return Path.Combine(folderPath, "link.xml");
        ;
    }

    /// <summary>
    /// Invoked before performing a Player build. Maintains building Addressables step and processing Addressables build data.
    /// </summary>
    /// <param name="buildPlayerContext"></param>
    public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        PrepareForPlayerbuild(settings, buildPlayerContext, ShouldBuildAddressablesForPlayerBuild(settings));
    }

    internal static void PrepareForPlayerbuild(AddressableAssetSettings settings, BuildPlayerContext buildPlayerContext, bool buildAddressables)
    {
        if (settings != null && buildAddressables)
        {
            AddressablesPlayerBuildResult result;
            if (BuildAddressablesOverride != null)
            {
                try
                {
                    result = BuildAddressablesOverride.Invoke(settings);
                }
                catch (Exception e)
                {
                    result = new AddressablesPlayerBuildResult();
                    result.Error = "Exception in BuildAddressablesOverride: " + e;
                }
            }
            else
                result = DefaultBuild(settings);

            if (result != null && !string.IsNullOrEmpty(result.Error))
                Debug.LogError($"Failed to build Addressables content, content not included in Player Build. \"{result.Error}\"");
        }

        if (buildPlayerContext != null)
        {
            var streamingAssetValues = GetStreamingAssetPaths();
            foreach (KeyValuePair<string, string> streamingAssetValue in streamingAssetValues)
            {
                buildPlayerContext.AddAdditionalPathToStreamingAssets(streamingAssetValue.Key,
                    streamingAssetValue.Value);
            }
        }

        string buildPath = Addressables.BuildPath + "/AddressablesLink/link.xml";
        if (File.Exists(buildPath))
        {
            string projectPath = GetLinkPath(settings, true);
            File.Copy(buildPath, projectPath, true);
            AssetDatabase.ImportAsset(projectPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.DontDownloadFromCacheServer);
        }
    }

    static AddressablesPlayerBuildResult DefaultBuild(AddressableAssetSettings settings)
    {
        var types = AddressableAssetUtility.GetTypes<AddressableAssetsSettingsGroupEditor.IAddressablesBuildMenu>();
        var displayMenus = AddressableAssetsSettingsGroupEditor.CreateBuildMenus(types);
        AddressableAssetsSettingsGroupEditor.IAddressablesBuildMenu defaultNewBuildMenu = null;
        foreach (var buildMenu in displayMenus)
        {
            if (buildMenu.BuildMenuPath == "New Build")
            {
                defaultNewBuildMenu = buildMenu;
                break;
            }
        }

        if (defaultNewBuildMenu != null)
        {
            AddressableAssetsSettingsGroupEditor.BuildMenuContext context = new AddressableAssetsSettingsGroupEditor.BuildMenuContext()
                { buildScriptIndex = -1, BuildMenu = defaultNewBuildMenu, Settings = settings };
            return AddressableAssetsSettingsGroupEditor.BuildAddressablesWithResult(context);
        }
        else
        {
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            return result;
        }
    }

    internal static bool ShouldBuildAddressablesForPlayerBuild(AddressableAssetSettings settings)
    {
        if (settings == null)
            return false;

        switch (settings.BuildAddressablesWithPlayerBuild)
        {
            case AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer:
                return false;
            case AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer:
                break;
            case AddressableAssetSettings.PlayerBuildOption.PreferencesValue:
                if (!EditorPrefs.GetBool(AddressablesPreferences.kBuildAddressablesWithPlayerBuildKey, true))
                    return false;
                break;
        }

        return true;
    }

    /// <summary>
    /// Allows to filter paths which should be added to StreamingAssets during build. Should return true if path should be added to Streaming Assets.
    /// </summary>
    internal static Func<string, bool> AddPathToStreamingAssets { get; set; }

    /// <summary>
    /// Gets a list of StreamingAsset files managed through Addressables, and relative path in StreamingAssets.
    /// </summary>
    internal static List<KeyValuePair<string, string>> GetStreamingAssetPaths()
    {
        List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>(1);
        if (Directory.Exists(Addressables.BuildPath) && (AddPathToStreamingAssets != null ? AddPathToStreamingAssets.Invoke(Addressables.BuildPath) : true))
            pairs.Add(new KeyValuePair<string, string>(Addressables.BuildPath, "aa"));
        return pairs;
    }
}
