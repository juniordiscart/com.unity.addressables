using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// Script added to bootstrap scene loading for the scene built by the <see cref="CustomBuildScript"/> Sample.
/// </summary>
public class LoadSceneForCustomBuild : MonoBehaviour
{
    /// <summary>
    /// Assigned the address of an AddressableAsset to bootstrap dynamically loading a scene at runtime.
    /// </summary>
    public string SceneKey;

    /// <summary>
    /// Start is called before the first frame update.
    /// </summary>
    void Start()
    {
        Addressables.LoadSceneAsync(SceneKey);
    }
}
