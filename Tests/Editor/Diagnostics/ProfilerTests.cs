using System;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Diagnostics;

namespace UnityEditor.AddressableAssets.Tests.Diagnostics
{
    internal class ProfilerTests
    {

//Removing this test from newer editors because something has changed in trunk with the icons.  The PhysicsMaterial Icon in particular is throwing an error.
//We'll need to make a ticket to look into this
#if !UNITY_2023_3_OR_NEWER
        [Test]
        public void GUIUtilitiesGetIcon_ThrowsNoErrors()
        {
            foreach (Enum e in Enum.GetValues(typeof(AssetType)))
            {
                ProfilerGUIUtilities.GetAssetIcon((AssetType)e);
            }
        }
#endif
    }
}
