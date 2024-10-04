using System;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Diagnostics;

namespace UnityEditor.AddressableAssets.Tests.Diagnostics
{
    internal class ProfilerTests
    {
        [Test]
        public void GUIUtilitiesGetIcon_ThrowsNoErrors()
        {
            foreach (Enum e in Enum.GetValues(typeof(AssetType)))
            {
                ProfilerGUIUtilities.GetAssetIcon((AssetType)e);
            }
        }
    }
}
