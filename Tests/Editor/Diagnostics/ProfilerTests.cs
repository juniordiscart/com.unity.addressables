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

#endif
