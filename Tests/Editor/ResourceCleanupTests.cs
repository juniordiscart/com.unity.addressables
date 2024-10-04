using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets.Tests
{
    using Object = UnityEngine.Object;

    public class ResourceCleanupTests : AddressableAssetTestBase
    {
        int CountResourcesByName(string name)
        {
            int count = 0;
            Object[] objects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
            foreach (Object o in objects)
            {
                if (o.name.Equals(name)) ++count;
            }

            return count;
        }

        [Test]
        public void CleanupDelayedActionManager()
        {
            var currentDAMCount = CountResourcesByName("DelayedActionManager");

            EditorApplication.isPlaying = true;
            DelayedActionManager.AddAction(new Action(() => { }));
            Assert.True(DelayedActionManager.Exists);
            Assert.NotNull(DelayedActionManager.Instance);
            EditorApplication.isPlaying = false;
            Assert.False(DelayedActionManager.Exists);

            Assert.AreEqual(currentDAMCount, CountResourcesByName("DelayedActionManager"));
        }
    }
}
