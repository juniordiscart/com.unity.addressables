using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets.Tests
{
    public class InitFixture : ScriptableObject, IObjectInitializationDataProvider
    {
        public string Name
        {
            get => "InitFixture1";
        }

        public ObjectInitializationData CreateObjectInitializationData()
        {
            return new ObjectInitializationData();
        }
    }
}
