using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.TestTools;

namespace UnityEditor.AddressableAssets.Tests
{
    public class DomainReloadTests
    {
#if UNITY_2022_1_OR_NEWER
        bool savedState;
        EnterPlayModeOptions savedOptions;

        [UnitySetUp]
        public IEnumerator RuntimeSetup()
        {
            savedState = EditorSettings.enterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            savedOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            Addressables.reinitializeAddressables = true;
            AssetBundleProvider.m_UnloadingBundles.Add("test", new AssetBundleUnloadOperation());
            Assert.False(Application.isPlaying);
            yield return new EnterPlayMode(false);

        }

        [UnityTearDown]
        public IEnumerator RuntimeTearDown()
        {
            yield return new ExitPlayMode();
            EditorSettings.enterPlayModeOptionsEnabled = savedState;
            EditorSettings.enterPlayModeOptions = savedOptions;

            if (AssetBundleProvider.m_UnloadingBundles.Count != 0)
            {
                AssetBundleProvider.m_UnloadingBundles = new Dictionary<string, AssetBundleUnloadOperation>();
            }
#if !UNITY_EDITOR
            Assert.IsTrue(Addressables.reinitializeAddressables);
#endif
            Assert.False(Application.isPlaying);
        }

        [Test]
        public void DomainReloadTests_EnteringPlaymode_ClearsUnloadingBundles()
        {
#if UNITY_2022_1_OR_NEWER
            Assert.AreEqual(AssetBundleProvider.m_UnloadingBundles.Count, 0, "m_UnloadingBundles not cleared correctly on enter playmode");
        #else
            Assert.Ignore("UNLOAD_BUNDLE_ASYNC scripting define is not set, test will be ignored.");
#endif
        }

        [Test]
        public void DomainReloadTests_ReInitAddressablesFlagIsSetCorrectly_WhenExitingPlaymode()
        {
            Assert.True(Application.isPlaying);
            Addressables.ResolveInternalId("DummyString"); //just need this so m_Addressables property gets called
            Assert.IsFalse(Addressables.reinitializeAddressables);
        }
#endif
    }
}

