#if UNITY_EDITOR
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEngine;

/// <summary>
/// Uses data built by CustomBuildScript class.  This script just sets up the correct variables and runs.
/// </summary>
[CreateAssetMenu(fileName = "CustomPlayMode.asset", menuName = "Addressables/Content Builders/Use CustomPlayMode Script")]
public class CustomPlayModeScript : BuildScriptPackedPlayMode
{
    /// <inheritdoc />
    public override string Name
    {
        get { return "Use Custom Build (requires built groups)"; }
    }
}
#endif
