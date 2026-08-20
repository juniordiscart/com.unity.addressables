using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using File = System.IO.File;
using Path = System.IO.Path;

/// <summary>
/// Verifies that ISceneProvider can be implemented from scratch in an external namespace.
/// If the test fails to compile, the type that broke it will appear as a compiler error in
/// the Unity console, identifying the specific API that needs to be made public.
/// </summary>
public class VerifyPublicSceneProvider
{
    private string m_FolderPath = $"Assets/SceneProviderVerify";

    /// <summary>
    /// Creates the folder the generated test script is written into.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        if (AssetDatabase.IsValidFolder(m_FolderPath))
            AssetDatabase.DeleteAsset(m_FolderPath);
        AssetDatabase.CreateFolder("Assets", "SceneProviderVerify");
    }

    /// <summary>
    /// Deletes the folder containing the generated test script.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (AssetDatabase.IsValidFolder(m_FolderPath))
            AssetDatabase.DeleteAsset(m_FolderPath);
    }

    /// <summary>
    /// Writes a minimal ISceneProvider implementation in a third-party namespace and checks it compiles.
    /// Any missing public API on the interface will surface as a compile error.
    /// </summary>
    /// <returns>IEnumerator for async test</returns>
    [UnityTest]
    public IEnumerator Verify_ISceneProvider_CanBeImplementedExternally()
    {
        var content =
            "using UnityEngine.ResourceManagement;\n" +
            "using UnityEngine.ResourceManagement.AsyncOperations;\n" +
            "using UnityEngine.ResourceManagement.ResourceLocations;\n" +
            "using UnityEngine.ResourceManagement.ResourceProviders;\n" +
            "using UnityEngine.SceneManagement;\n" +
            "\n" +
            "namespace ThirdParty.CustomProviders\n" +
            "{\n" +
            "    class MinimalSceneProvider : ISceneProvider\n" +
            "    {\n" +
            "        public AsyncOperationHandle<SceneInstance> ProvideScene(\n" +
            "            ResourceManager resourceManager, IResourceLocation location,\n" +
            "            LoadSceneMode loadMode, bool activateOnLoad, int priority) => default;\n" +
            "\n" +
            "        public AsyncOperationHandle<SceneInstance> ProvideScene(\n" +
            "            ResourceManager resourceManager, IResourceLocation location,\n" +
            "            LoadSceneParameters loadSceneParameters, bool activateOnLoad, int priority) => default;\n" +
            "\n" +
            "        public AsyncOperationHandle<SceneInstance> ProvideScene(\n" +
            "            ResourceManager resourceManager, IResourceLocation location,\n" +
            "            LoadSceneParameters loadSceneParameters, SceneReleaseMode releaseMode,\n" +
            "            bool activateOnLoad, int priority) => default;\n" +
            "\n" +
            "        public AsyncOperationHandle<SceneInstance> ReleaseScene(\n" +
            "            ResourceManager resourceManager,\n" +
            "            AsyncOperationHandle<SceneInstance> sceneLoadHandle) => default;\n" +
            "\n" +
            "        public AsyncOperationHandle<SceneInstance> ReleaseScene(\n" +
            "            ResourceManager resourceManager,\n" +
            "            AsyncOperationHandle<SceneInstance> sceneLoadHandle,\n" +
            "            UnloadSceneOptions unloadOptions) => default;\n" +
            "    }\n" +
            "}\n";

        var testFilePath = Path.Combine(m_FolderPath, "MinimalSceneProvider.cs");
        File.WriteAllText(testFilePath, content);
        AssetDatabase.Refresh();
        yield return new WaitForDomainReload();

        LogAssert.NoUnexpectedReceived();
    }
}
