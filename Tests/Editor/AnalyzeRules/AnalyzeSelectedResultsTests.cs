using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.GUI.Adapters;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests.AnalyzeRules
{
    [TestFixture]
    public class AnalyzeSelectedResultsTests : AddressableAssetTestBase
    {
        class TestFixSelectedRule : AnalyzeRule
        {
            public IReadOnlyCollection<string> LastSelectedNames { get; private set; }

            public override string ruleName => nameof(TestFixSelectedRule);

            public override bool SupportsFixSelectedResults => true;

            public override void FixSelectedResults(AddressableAssetSettings settings, IReadOnlyCollection<string> selectedResultNames)
            {
                LastSelectedNames = selectedResultNames;
            }
        }

        class TestRuleA : AnalyzeRule
        {
            public override string ruleName => nameof(TestRuleA);
        }

        class TestRuleB : AnalyzeRule
        {
            public override string ruleName => nameof(TestRuleB);
        }

        class TestableAssetSettingsAnalyzeTreeView : AssetSettingsAnalyzeTreeView
        {
            public TestableAssetSettingsAnalyzeTreeView(TreeViewStateAdapter state)
                : base(state)
            {
            }

            public bool CanMultiSelectForTest(TreeViewItemAdapter item) => CanMultiSelect(item);
        }

        /// <summary>
        /// Minimal tree for exercising <see cref="AssetSettingsAnalyzeTreeView.UpdateSelections"/>.
        /// Assign <see cref="RuleForBuild"/> before constructing.
        /// </summary>
        class MinimalSelectionTestTreeView : AssetSettingsAnalyzeTreeView
        {
            internal static AnalyzeRule RuleForBuild;

            public const int ContainerItemId = 96001;
            public const int ResultItemId = 96002;

            internal MinimalSelectionTestTreeView(TreeViewStateAdapter state)
                : base(state)
            {
            }

            protected override TreeViewItemAdapter BuildRootAdapter()
            {
                var rule = RuleForBuild;
                if (rule == null)
                    throw new InvalidOperationException($"Assign {nameof(MinimalSelectionTestTreeView)}.{nameof(RuleForBuild)} before creating the tree.");

                var root = new TreeViewItemAdapter(-1, -1);
                root.children = TreeViewItemAdapter.EmptyList();
                var container = new AnalyzeRuleTreeViewItem(ContainerItemId, 1, rule);
                var result = new AnalyzeResultsTreeViewItem(ResultItemId, 2, "x", MessageType.Warning);
                result.parent = container;
                container.AddChild(result);
                root.AddChild(container);
                return root;
            }
        }

        [Test]
        public void AnalyzeRule_DefaultSupportsFixSelectedResults_IsFalse()
        {
            var rule = new AnalyzeRule();
            Assert.IsFalse(rule.SupportsFixSelectedResults);
        }

        [Test]
        public void BundleRuleBase_FixSelectedResults_DoesNotThrow()
        {
            var rule = new BundleRuleBase();
            Assert.DoesNotThrow(() => rule.FixSelectedResults(Settings, new[] { "a" }));
        }

        [Test]
        public void AnalyzeSystem_FixSelectedResults_ForwardsNamesToRule()
        {
            int idx = AnalyzeSystem.Rules.Count;
            AnalyzeSystem.RegisterNewRule<TestFixSelectedRule>();
            try
            {
                var rule = AnalyzeSystem.Rules[idx] as TestFixSelectedRule;
                Assert.NotNull(rule);
                var names = new[] { "r1", "r2" };
                AnalyzeSystem.FixSelectedResults(rule, names);
                Assert.AreSame(names, rule.LastSelectedNames);
            }
            finally
            {
                AnalyzeSystem.Rules.RemoveAt(idx);
            }
        }

        [Test]
        public void AnalyzeResultsSelection_TryGetSingleRegisteredRuleContainer_SingleRule_ReturnsTrue()
        {
            int idx = AnalyzeSystem.Rules.Count;
            AnalyzeSystem.RegisterNewRule<TestRuleA>();
            try
            {
                var registered = AnalyzeSystem.Rules[idx];
                var container = new AnalyzeRuleTreeViewItem(90001, 2, registered);
                var resultItem = new AnalyzeResultsTreeViewItem(90002, 3, "child", MessageType.Warning);
                resultItem.parent = container;
                var list = new List<AnalyzeResultsTreeViewItem> { resultItem };
                Assert.IsTrue(AnalyzeResultsSelection.TryGetSingleRegisteredRuleContainer(list, out var rule, out var c));
                Assert.AreSame(registered, rule);
                Assert.AreSame(container, c);
            }
            finally
            {
                AnalyzeSystem.Rules.RemoveAt(idx);
            }
        }

        [Test]
        public void AnalyzeResultsSelection_TryGetSingleRegisteredRuleContainer_TwoItemsSameRule_ReturnsTrue()
        {
            int idx = AnalyzeSystem.Rules.Count;
            AnalyzeSystem.RegisterNewRule<TestRuleA>();
            try
            {
                var registered = AnalyzeSystem.Rules[idx];
                var container = new AnalyzeRuleTreeViewItem(92001, 2, registered);
                var item1 = new AnalyzeResultsTreeViewItem(92002, 3, "child1", MessageType.Warning);
                var item2 = new AnalyzeResultsTreeViewItem(92003, 3, "child2", MessageType.Warning);
                item1.parent = container;
                item2.parent = container;
                var list = new List<AnalyzeResultsTreeViewItem> { item1, item2 };
                Assert.IsTrue(AnalyzeResultsSelection.TryGetSingleRegisteredRuleContainer(list, out var rule, out var c));
                Assert.AreSame(registered, rule);
                Assert.AreSame(container, c);
            }
            finally
            {
                AnalyzeSystem.Rules.RemoveAt(idx);
            }
        }

        [Test]
        public void AssetSettingsAnalyzeTreeView_CanMultiSelect_True_ForAnalyzeResultsTreeViewItem()
        {
            var tree = new TestableAssetSettingsAnalyzeTreeView(new TreeViewStateAdapter());
            var item = new AnalyzeResultsTreeViewItem(93001, 3, "x", MessageType.Warning);
            Assert.IsTrue(tree.CanMultiSelectForTest(item));
        }

        [Test]
        public void AssetSettingsAnalyzeTreeView_CanMultiSelect_True_ForAnalyzeRuleTreeViewItem()
        {
            int idx = AnalyzeSystem.Rules.Count;
            AnalyzeSystem.RegisterNewRule<TestRuleA>();
            try
            {
                var registered = AnalyzeSystem.Rules[idx];
                var container = new AnalyzeRuleTreeViewItem(94001, 2, registered);
                var tree = new TestableAssetSettingsAnalyzeTreeView(new TreeViewStateAdapter());
                Assert.IsTrue(tree.CanMultiSelectForTest(container));
            }
            finally
            {
                AnalyzeSystem.Rules.RemoveAt(idx);
            }
        }

        [Test]
        public void AssetSettingsAnalyzeTreeView_CanMultiSelect_True_ForAnalyzeGroupTreeViewItem()
        {
            var folder = new AnalyzeGroupTreeViewItem(95001, 1, "Auto Fix Rules");
            var tree = new TestableAssetSettingsAnalyzeTreeView(new TreeViewStateAdapter());
            Assert.IsTrue(tree.CanMultiSelectForTest(folder));
        }

        [Test]
        public void UpdateSelections_SelectionContainsOnlyAnalyzeResults_True_WhenOnlyResultRowsSelected()
        {
            int idx = AnalyzeSystem.Rules.Count;
            AnalyzeSystem.RegisterNewRule<TestRuleA>();
            try
            {
                MinimalSelectionTestTreeView.RuleForBuild = AnalyzeSystem.Rules[idx];
                var tree = new MinimalSelectionTestTreeView(new TreeViewStateAdapter());
                tree.UpdateSelections(new List<int> { MinimalSelectionTestTreeView.ResultItemId });
                Assert.IsTrue(tree.SelectionContainsOnlyAnalyzeResults);
            }
            finally
            {
                MinimalSelectionTestTreeView.RuleForBuild = null;
                AnalyzeSystem.Rules.RemoveAt(idx);
            }
        }

        [Test]
        public void UpdateSelections_SelectionContainsOnlyAnalyzeResults_False_WhenRuleContainerSelected()
        {
            int idx = AnalyzeSystem.Rules.Count;
            AnalyzeSystem.RegisterNewRule<TestRuleA>();
            try
            {
                MinimalSelectionTestTreeView.RuleForBuild = AnalyzeSystem.Rules[idx];
                var tree = new MinimalSelectionTestTreeView(new TreeViewStateAdapter());
                tree.UpdateSelections(new List<int> { MinimalSelectionTestTreeView.ContainerItemId });
                Assert.IsFalse(tree.SelectionContainsOnlyAnalyzeResults);
            }
            finally
            {
                MinimalSelectionTestTreeView.RuleForBuild = null;
                AnalyzeSystem.Rules.RemoveAt(idx);
            }
        }

        [Test]
        public void UpdateSelections_SelectionContainsOnlyAnalyzeResults_False_WhenMixedRuleAndResultSelected()
        {
            int idx = AnalyzeSystem.Rules.Count;
            AnalyzeSystem.RegisterNewRule<TestRuleA>();
            try
            {
                MinimalSelectionTestTreeView.RuleForBuild = AnalyzeSystem.Rules[idx];
                var tree = new MinimalSelectionTestTreeView(new TreeViewStateAdapter());
                tree.UpdateSelections(new List<int>
                {
                    MinimalSelectionTestTreeView.ResultItemId,
                    MinimalSelectionTestTreeView.ContainerItemId
                });
                Assert.IsFalse(tree.SelectionContainsOnlyAnalyzeResults);
            }
            finally
            {
                MinimalSelectionTestTreeView.RuleForBuild = null;
                AnalyzeSystem.Rules.RemoveAt(idx);
            }
        }

        [Test]
        public void UpdateSelections_SelectionContainsOnlyAnalyzeResults_False_WhenSelectionEmpty()
        {
            int idx = AnalyzeSystem.Rules.Count;
            AnalyzeSystem.RegisterNewRule<TestRuleA>();
            try
            {
                MinimalSelectionTestTreeView.RuleForBuild = AnalyzeSystem.Rules[idx];
                var tree = new MinimalSelectionTestTreeView(new TreeViewStateAdapter());
                tree.UpdateSelections(new List<int>());
                Assert.IsFalse(tree.SelectionContainsOnlyAnalyzeResults);
            }
            finally
            {
                MinimalSelectionTestTreeView.RuleForBuild = null;
                AnalyzeSystem.Rules.RemoveAt(idx);
            }
        }

        [Test]
        public void AnalyzeResultsSelection_TryGetSingleRegisteredRuleContainer_TwoDifferentRules_ReturnsFalse()
        {
            int idx = AnalyzeSystem.Rules.Count;
            AnalyzeSystem.RegisterNewRule<TestRuleA>();
            AnalyzeSystem.RegisterNewRule<TestRuleB>();
            try
            {
                var ruleA = AnalyzeSystem.Rules[idx];
                var ruleB = AnalyzeSystem.Rules[idx + 1];
                var containerA = new AnalyzeRuleTreeViewItem(91001, 2, ruleA);
                var containerB = new AnalyzeRuleTreeViewItem(91002, 2, ruleB);
                var itemA = new AnalyzeResultsTreeViewItem(91003, 3, "a", MessageType.Warning);
                var itemB = new AnalyzeResultsTreeViewItem(91004, 3, "b", MessageType.Warning);
                itemA.parent = containerA;
                itemB.parent = containerB;
                var list = new List<AnalyzeResultsTreeViewItem> { itemA, itemB };
                Assert.IsFalse(AnalyzeResultsSelection.TryGetSingleRegisteredRuleContainer(list, out _, out _));
            }
            finally
            {
                AnalyzeSystem.Rules.RemoveAt(idx + 1);
                AnalyzeSystem.Rules.RemoveAt(idx);
            }
        }

        [Test]
        public void BuildResults_IdenticalPathsUnderDifferentRuleContainers_HaveDistinctRowIds()
        {
            var tree = new TestableAssetSettingsAnalyzeTreeView(new TreeViewStateAdapter());
            var ruleA = new TestRuleA();
            var ruleB = new TestRuleB();
            var containerA = new AnalyzeRuleTreeViewItem(501001, 1, ruleA);
            var containerB = new AnalyzeRuleTreeViewItem(502002, 1, ruleB);

            string sharedPath = $"Parent{AnalyzeRule.kDelimiter}Child{AnalyzeRule.kDelimiter}Leaf.asset";
            var results = new List<AnalyzeRule.AnalyzeResult>
            {
                new AnalyzeRule.AnalyzeResult { resultName = sharedPath, severity = MessageType.Warning }
            };

            var buildResults = typeof(AssetSettingsAnalyzeTreeView).GetMethod(
                "BuildResults",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(buildResults);

            buildResults.Invoke(tree, new object[] { containerA, results });
            buildResults.Invoke(tree, new object[] { containerB, new List<AnalyzeRule.AnalyzeResult>(results) });

            var idsA = new HashSet<int>();
            var idsB = new HashSet<int>();
            CollectAnalyzeResultsRowIds(containerA, idsA);
            CollectAnalyzeResultsRowIds(containerB, idsB);

            foreach (int id in idsB)
                Assert.IsFalse(idsA.Contains(id),
                    $"Row id {id} collides between two rule branches with identical result paths.");
        }

        static void CollectAnalyzeResultsRowIds(TreeViewItemAdapter node, HashSet<int> ids)
        {
            if (node is AnalyzeResultsTreeViewItem ri)
                ids.Add(ri.id);
            if (!node.hasChildren)
                return;
            foreach (var child in node.children)
                CollectAnalyzeResultsRowIds((TreeViewItemAdapter)child, ids);
        }
    }
}
