using UnityEditor.AddressableAssets.Build.Layout;

namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    interface IBuildReportConsumer
    {
        void Consume(BuildLayout buildReport);
    }

}
