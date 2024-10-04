using NUnit.Framework;
#if ENABLE_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

#if !ENABLE_CCD
namespace UnityEditor.AddressableAssets.Tests.OptionalPackages.Ccd
{
    public class CcdNotFoundTest
    {

        [SetUp]
        public void SetUp()
        {
            // initialize settings if they don't exist
            AddressableAssetSettingsDefaultObject.GetSettings(true);
        }

        [Test]
        public void VerifyCCDDisabled()
        {
            // it is difficult to test this, but AddressableAssetSettings.CheckCCDStatus is called using InitializeOnLoad
            // if the package is not installed CCDEnabled will be set to false regardless of initial status
            Assert.That(AddressableAssetSettingsDefaultObject.Settings.CCDEnabled, Is.False);
        }
    }
}
#endif
