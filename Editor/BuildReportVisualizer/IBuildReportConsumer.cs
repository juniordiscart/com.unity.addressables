#if UNITY_2022_2_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEngine;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    interface IBuildReportConsumer
    {
        void Consume(BuildLayout buildReport);
    }

}
#endif
