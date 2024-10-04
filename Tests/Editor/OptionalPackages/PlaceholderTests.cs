using NUnit.Framework;


namespace UnityEditor.AddressableAssets.Tests.Editor.OptionalPackages
{
    public class PlaceholderTests
    {
        [Test]
        public void Test_Placeholder()
        {
            // this can be removed if optional package tests are added, the utr used by
            // upm-ci doesn't currently seem to have a way to allow zero tests
            Assert.IsTrue(true);
        }
    }
}
