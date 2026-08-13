using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.BuildReportVisualizer;
using UnityEngine.TestTools;

namespace Tests.Editor.BuildReportVisualizer
{
    public class BuildReportWindowTests
    {
        [Test]
        public void CanOpenAndCloseWindow()
        {
            var window = EditorWindow.GetWindow<BuildReportWindow>();
            LogAssert.NoUnexpectedReceived();
            window.Close();
        }
    }
}
