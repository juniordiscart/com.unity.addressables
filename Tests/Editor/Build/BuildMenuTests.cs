using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.GUI;
using UnityEngine;
using UnityEngine.TestTools;
using static UnityEditor.AddressableAssets.GUI.AddressableAssetsSettingsGroupEditor;

namespace UnityEditor.AddressableAssets.Tests
{
    public class BuildMenuTests : AddressableAssetTestBase
    {
        [HideBuildMenuInUI]
        public class BaseTestBuildMenu : AddressableAssetsSettingsGroupEditor.IAddressablesBuildMenu
        {
            public virtual string BuildMenuPath
            {
                get => "";
            }

            public virtual bool SelectableBuildScript
            {
                get => true;
            }

            public virtual int Order
            {
                get => 0;
            }

            public virtual bool OnPrebuild(AddressablesDataBuilderInput input)
            {
                return true;
            }

            public virtual bool OnPostbuild(AddressablesDataBuilderInput input, AddressablesPlayerBuildResult result)
            {
                return true;
            }
        }

        public class TestBuildMenu : BaseTestBuildMenu
        {
            public override bool OnPrebuild(AddressablesDataBuilderInput input)
            {
                Debug.Log("Pre Invoked");
                return true;
            }

            public override bool OnPostbuild(AddressablesDataBuilderInput input, AddressablesPlayerBuildResult result)
            {
                Debug.Log("Post Invoked");
                return true;
            }
        }

        public class TestBuildMenuOrderZero : BaseTestBuildMenu
        {
            public override string BuildMenuPath => "Zero";
            public override int Order => 0;
        }

        public class TestBuildMenuOrderMinusOne : BaseTestBuildMenu
        {
            public override string BuildMenuPath => "MinusOne";
            public override int Order => -1;
        }

        public class TestBuildMenuOrderOne : BaseTestBuildMenu
        {
            public override string BuildMenuPath => "One";
            public override int Order => 1;
        }

        public class TestBuildMenu1_BuildPathTest : BaseTestBuildMenu
        {
            public override string BuildMenuPath => "Test";
        }

        public class TestBuildMenu2_BuildPathTest : BaseTestBuildMenu
        {
            public override string BuildMenuPath => "Test";
        }

        [Test]
        public void BuildMenu_BuildCorrectlyCallPreAndPost()
        {
            AddressableAssetsSettingsGroupEditor.BuildMenuContext context =
                new AddressableAssetsSettingsGroupEditor.BuildMenuContext();
            context.BuildMenu = new TestBuildMenu();
            context.buildScriptIndex = -1;
            context.Settings = Settings;

            // Test
            LogAssert.Expect(LogType.Log, "Pre Invoked");
            LogAssert.Expect(LogType.Log, "Post Invoked");
            AddressableAssetsSettingsGroupEditor.OnBuildAddressables(context);
        }

        [Test]
        public void BuildMenu_CreateBuildMenus_CorrectOrder()
        {
            List<Type> menuTypes = new List<Type>();
            // 1, -1, 0
            menuTypes.Add(typeof(TestBuildMenuOrderOne));
            menuTypes.Add(typeof(TestBuildMenuOrderMinusOne));
            menuTypes.Add(typeof(TestBuildMenuOrderZero));
            var menus = AddressableAssetsSettingsGroupEditor.CreateBuildMenus(menuTypes, true);

            Assert.AreEqual(3, menus.Count, "Failed to get the correct number of build menus");
            string orderStr = menus[0].Order.ToString();
            orderStr += "," + menus[1].Order.ToString();
            orderStr += "," + menus[2].Order.ToString();
            Assert.AreEqual("-1,0,1", orderStr, "Menus not in the correct order");
        }

        [Test]
        public void BuildMenu_RemovesConflictingBuildPaths()
        {
            List<Type> menuTypes = new List<Type>();
            menuTypes.Add(typeof(TestBuildMenu1_BuildPathTest));
            menuTypes.Add(typeof(TestBuildMenu2_BuildPathTest));
            LogAssert.Expect(LogType.Warning,
                "Trying to new build menu [UnityEditor.AddressableAssets.Tests.BuildMenuTests+TestBuildMenu2_BuildPathTest] with path \"Test\". But an existing type already exists with that path, [UnityEditor.AddressableAssets.Tests.BuildMenuTests+TestBuildMenu1_BuildPathTest].");
            var menus = AddressableAssetsSettingsGroupEditor.CreateBuildMenus(menuTypes, true);

            Assert.AreEqual(1, menus.Count, "Failed to get the correct number of build menus");
        }
    }
}
