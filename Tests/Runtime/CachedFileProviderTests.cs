using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.TestTools;


public abstract class CachedFileProviderTests : AddressablesTestFixture
{
    string ComputeCachePath(string filename)
    {
        return Hash128.Compute(File.ReadAllBytes(filename)).ToString();
    }

    string GetTempFileName()
    {
        return Path.Combine(Application.temporaryCachePath, Path.GetFileName(Path.GetTempFileName()));
    }

    [UnityTest]
    public IEnumerator CanProvideRemoteFileToCache_WhenCacheIsEmpty()
    {
        m_Addressables.ResourceManager.ResourceProviders.Add(new CachedFileProvider());
        var testText = "test text";
        var tempFile = GetTempFileName();
        File.WriteAllText(tempFile, testText);
        var cachePath = ComputeCachePath(tempFile);
        var fullCachePath = $"{Application.temporaryCachePath}/{cachePath}";
        if (File.Exists(fullCachePath))
            File.Delete(fullCachePath);
        var loc = new ResourceLocationBase("key", $"file://{tempFile}", typeof(CachedFileProvider).FullName, typeof(string));
        loc.Data = new ProviderLoadRequestOptions { LocalCachePath = cachePath };
        var h = m_Addressables.ResourceManager.ProvideResource<string>(loc);
        yield return h;
        Assert.IsTrue(File.Exists(h.Result));
        Assert.AreEqual(testText, File.ReadAllText(h.Result));
        File.Delete(h.Result);
    }

    [UnityTest]
    public IEnumerator CanProvideLocalFileToCache_WhenCacheIsEmpty()
    {
        m_Addressables.ResourceManager.ResourceProviders.Add(new CachedFileProvider());
        var testText = "test text";
        var tempFile = GetTempFileName();
        File.WriteAllText(tempFile, testText);
        var cachePath = ComputeCachePath(tempFile);
        var fullCachePath = $"{Application.temporaryCachePath}/{cachePath}";
        if (File.Exists(fullCachePath))
            File.Delete(fullCachePath);
        var loc = new ResourceLocationBase("key", tempFile, typeof(CachedFileProvider).FullName, typeof(string));
        loc.Data = new ProviderLoadRequestOptions { LocalCachePath = cachePath };
        var h = m_Addressables.ResourceManager.ProvideResource<string>(loc);
        yield return h;
        Assert.IsTrue(File.Exists(h.Result));
        Assert.AreEqual(testText, File.ReadAllText(h.Result));
        File.Delete(h.Result);
    }

    [UnityTest]
    public IEnumerator CanProvideRemoteFileToCache_WhenCacheHasResult()
    {
        m_Addressables.ResourceManager.ResourceProviders.Add(new CachedFileProvider());
        var testText = "test text";
        var tempFile = GetTempFileName();
        File.WriteAllText(tempFile, testText);
        var cachePath = ComputeCachePath(tempFile);
        var fullCachePath = $"{Application.temporaryCachePath}/{cachePath}";
        File.Copy(tempFile, fullCachePath, true);
        var loc = new ResourceLocationBase("key", $"file://{tempFile}", typeof(CachedFileProvider).FullName, typeof(string));
        loc.Data = new ProviderLoadRequestOptions { LocalCachePath = cachePath };
        var h = m_Addressables.ResourceManager.ProvideResource<string>(loc);
        yield return h;
        Assert.IsTrue(File.Exists(h.Result));
        Assert.AreEqual(testText, File.ReadAllText(h.Result));
        File.Delete(h.Result);
    }

    [UnityTest]
    public IEnumerator CanProvideRemoteFileToCache_WhenCachePathIsInvalid()
    {
        m_Addressables.ResourceManager.ResourceProviders.Add(new CachedFileProvider());
        var testText = "test text";
        var tempFile = GetTempFileName();
        File.WriteAllText(tempFile, testText);
        var loc = new ResourceLocationBase("key", $"file://{tempFile}", typeof(CachedFileProvider).FullName, typeof(string));
        var h = m_Addressables.ResourceManager.ProvideResource<string>(loc);
        yield return h;
        Assert.IsTrue(File.Exists(h.Result));
        Assert.AreEqual(testText, File.ReadAllText(h.Result));
    }

}


#if UNITY_EDITOR
class CachedFileProviderTests_PackedPlaymodeMode : CachedFileProviderTests
{
    protected override TestBuildScriptMode BuildScriptMode
    {
        get { return TestBuildScriptMode.PackedPlaymode; }
    }
}
#endif

[UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor, RuntimePlatform.PS5, RuntimePlatform.Android, RuntimePlatform.Switch
#if UNITY_6000_5_OR_NEWER
    , RuntimePlatform.Switch2
#endif
})]
class CachedFileProviderTests_PackedMode : CachedFileProviderTests
{
    protected override TestBuildScriptMode BuildScriptMode
    {
        get { return TestBuildScriptMode.Packed; }
    }
}
