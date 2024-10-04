#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;


/// <summary>
/// Build scripts used for player builds and running with bundles in the editor.
/// </summary>
[CreateAssetMenu(fileName = "CustomBuild.asset", menuName = "Addressables/Content Builders/Custom Build Script")]
public class CustomBuildScript : BuildScriptPackedMode
{
    Dictionary<AddressableAssetGroup, bool> m_SavedIncludeInBuildState = new Dictionary<AddressableAssetGroup, bool>();
    AddressableAssetGroup m_CurrentSceneGroup;

    /// <inheritdoc />
    public override string Name
    {
        get { return "Custom Build Script"; }
    }

    /// <inheritdoc />
    protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
    {
        TResult result;
        var timer = new Stopwatch();
        timer.Start();

        try
        {
            CreateCurrentSceneOnlyBuildSetup(builderInput.AddressableSettings);
            result = base.BuildDataImplementation<TResult>(builderInput);
        }
        finally
        {
            RevertCurrentSceneSetup(builderInput.AddressableSettings);
        }

        if (result != null)
            result.Duration = timer.Elapsed.TotalSeconds;

        return result;
    }

    void CreateCurrentSceneOnlyBuildSetup(AddressableAssetSettings settings)
    {
        Debug.Log("HERE");
        Debug.LogError(SceneManager.GetActiveScene().name);
        if (SceneManager.GetActiveScene().name == "customscene")
        {
            throw new Exception(
                "please make sure that a scene other than the customscene is currently open and make sure it doesn't have any Addressable dependencies");

        }
        foreach (var group in settings.groups)
        {
            if (!group)
                continue;
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (!schema)
                continue;
            m_SavedIncludeInBuildState.Add(group, schema.IncludeInBuild);
            schema.IncludeInBuild = false;
        }

        m_CurrentSceneGroup = settings.CreateGroup("TempCurrentSceneGroup", false, false, true,
            new List<AddressableAssetGroupSchema>(), typeof(BundledAssetGroupSchema));
        var sceneEntry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(SceneManager.GetActiveScene().path), m_CurrentSceneGroup);

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
        AssetDatabase.SaveAssets();

        //Setup bootstrap scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        GameObject go = new GameObject("Bootstrapper");
        go.AddComponent<LoadSceneForCustomBuild>().SceneKey = sceneEntry.address;
        string dir = "Assets/TempCustomBuildScene";
        Directory.CreateDirectory(dir);
        EditorSceneManager.SaveScene(scene, $"{dir}/customscene.unity");
    }

    void RevertCurrentSceneSetup(AddressableAssetSettings settings)
    {
        foreach (var group in settings.groups)
        {
            if (!group)
                continue;
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (!schema || !m_SavedIncludeInBuildState.ContainsKey(group))
                continue;
            schema.IncludeInBuild = m_SavedIncludeInBuildState[group];
        }

        m_SavedIncludeInBuildState.Clear();
        settings.RemoveGroup(m_CurrentSceneGroup);
        m_CurrentSceneGroup = null;
    }
}
#endif
