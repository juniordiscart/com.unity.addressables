using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.AddressableAssets.Tests;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests.AnalyzeRules
{
    [TestFixture]
    public class AnalyzeRuleBaseTests : AddressableAssetTestBase
    {
        [Test]
        public void ConvertBundleNamesToGroupNames()
        {
            var bundleName = "2398471298347129034_bundlename_1";
            var fakeFileName = "archive://3912983hf9sdf902340jidf";
            var convertedBundleName = "group1_bundlename_1";

            var group = Settings.CreateGroup("group1", false, false, false, null, typeof(BundledAssetGroupSchema));

            AddressableAssetsBuildContext context = new AddressableAssetsBuildContext();
            context.Settings = Settings;
            context.assetGroupToBundles = new Dictionary<AddressableAssetGroup, List<string>>()
            {
                {group, new List<string>() {bundleName}}
            };

            BundleRuleBase baseRule = new BundleRuleBase();
            baseRule.m_ExtractData = new ExtractDataTask();

            var field = typeof(ExtractDataTask).GetField("m_WriteData", BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(baseRule.m_ExtractData, new BundleWriteData());

            baseRule.m_AllBundleInputDefs.Add(new AssetBundleBuild()
            {
                assetBundleName = bundleName
            });

            baseRule.m_ExtractData.WriteData.FileToBundle.Add(fakeFileName, bundleName);
            baseRule.ConvertBundleNamesToGroupNames(context);

            Assert.AreEqual(convertedBundleName, baseRule.m_ExtractData.WriteData.FileToBundle[fakeFileName]);

            Settings.RemoveGroup(group);
        }

        [Test]
        public void BaseAnalyzeRule_DoesNotThrowOnFix()
        {
            BundleRuleBase baseRule = new BundleRuleBase();
            Assert.DoesNotThrow(() => baseRule.FixIssues(Settings));
        }

        class TestBaseRule : BundleRuleBase
        {
            public override string ruleName => "TestBaseRule";
        }

        class TestInheritedRule : TestBaseRule
        {
            public override string ruleName => "TestInheritedRule";
        }

        [Test]
        public void AnalyzeSystem_CanRegisterInheritedRule()
        {
            int currentCount = AnalyzeSystem.Rules.Count;

            AnalyzeSystem.RegisterNewRule<TestBaseRule>();
            AnalyzeSystem.RegisterNewRule<TestInheritedRule>();

            Assert.AreEqual(currentCount + 2, AnalyzeSystem.Rules.Count);
            Assert.AreEqual(typeof(TestBaseRule), AnalyzeSystem.Rules[currentCount].GetType());
            Assert.AreEqual(typeof(TestInheritedRule), AnalyzeSystem.Rules[currentCount + 1].GetType());

            AnalyzeSystem.Rules.RemoveAt(currentCount + 1);
            AnalyzeSystem.Rules.RemoveAt(currentCount);
        }

        [Test]
        public void AnalyzeSystem_CannotRegisterDuplicateRule()
        {
            int currentCount = AnalyzeSystem.Rules.Count;

            AnalyzeSystem.RegisterNewRule<TestBaseRule>();
            AnalyzeSystem.RegisterNewRule<TestBaseRule>();
            Assert.AreEqual(currentCount + 1, AnalyzeSystem.Rules.Count);

            AnalyzeSystem.Rules.RemoveAt(currentCount);
        }

        [Test]
        public void AnalyzeSystem_CanSaveAndLoad()
        {
            string path = GetAssetPath("analysis.json");
            if (File.Exists(path))
                File.Delete(path);

            int currentCount = AnalyzeSystem.Rules.Count;
            AnalyzeSystem.RegisterNewRule<TestBaseRule>();
            AnalyzeSystem.ClearAnalysis<TestBaseRule>();
            try
            {
                AnalyzeSystem.SerializeData(path);
                Assert.IsTrue(File.Exists(path));
                string json = File.ReadAllText(path);
                Assert.IsTrue(json.Contains("\"RuleName\":\"TestBaseRule\",\"Results\":[]"));
                json = json.Replace("\"TestBaseRule\",\"Results\":[", "\"TestBaseRule\",\"Results\":[{\"m_ResultName\":\"someFakeResult\",\"m_Severity\":0}");
                File.WriteAllText(path, json);

                AnalyzeSystem.AnalyzeData.Data.TryGetValue(
                    "TestBaseRule", out var results);
                Assert.IsFalse(results.Any(r => r.resultName == "someFakeResult"));

                AnalyzeSystem.DeserializeData(path);
                Assert.IsTrue(AnalyzeSystem.AnalyzeData.Data.TryGetValue(
                    "TestBaseRule", out results));
                Assert.IsTrue(results.Count > 0);
                Assert.AreEqual("someFakeResult", results[0].resultName);
            }
            finally
            {
                //cleanup
                AnalyzeSystem.Rules.RemoveAt(currentCount);
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
