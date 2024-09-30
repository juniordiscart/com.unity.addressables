#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Diagnostics;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests.Diagnostics
{
    internal class ProfilerTests
    {
        //Removing this test from newer editors because something has changed in trunk with the icons.  The PhysicsMaterial Icon in particular is throwing an error.
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

#endif
