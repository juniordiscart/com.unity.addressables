using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.GUI.Adapters;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityEditor.AddressableAssets.GUI
{
    /// <summary>
    /// Analyze Rules tree: grouping folders, per-rule rows from <see cref="AnalyzeSystem.Rules"/>, and nested result rows.
    /// Toolbar actions scheduled via <see cref="EditorApplication.delayCall"/> should pass a selection snapshot taken at click time
    /// so the operation matches the user intent if selection changes before the callback runs.
    /// </summary>
    class AssetSettingsAnalyzeTreeView : TreeViewAdapter
    {
        int m_CurrentDepth;

        internal AssetSettingsAnalyzeTreeView(TreeViewStateAdapter state)
            : base(state)
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;

            Reload();
        }

        /// <summary>
        /// Collects every <see cref="AnalyzeRuleTreeViewItem"/> under <paramref name="node"/> (does not recurse through result rows).
        /// </summary>
        static void GatherDescendantRuleRows(TreeViewItemAdapter node, List<AnalyzeRuleTreeViewItem> result)
        {
            if (node == null || !node.hasChildren)
                return;

            foreach (var child in node.children)
            {
                if (child is AnalyzeRuleTreeViewItem ruleRow)
                    result.Add(ruleRow);
                else if (child is AnalyzeResultsTreeViewItem)
                    continue;
                else if (child is TreeViewItemAdapter adapter)
                    GatherDescendantRuleRows(adapter, result);
            }
        }

        /// <summary>
        /// Appends rule rows implied by the selection: selected rule/group folders, and the registered rule row that owns each selected issue row.
        /// </summary>
        void AppendRuleRowsExpandedFromSelectionIds(IEnumerable<int> ids, List<AnalyzeRuleTreeViewItem> destination)
        {
            foreach (int id in ids)
            {
                var item = FindItem(id, rootItem) as TreeViewItemAdapter;
                if (item is AnalyzeRuleTreeViewItem rr)
                {
                    destination.Add(rr);
                    GatherDescendantRuleRows(rr, destination);
                }
                else if (item is AnalyzeGroupTreeViewItem gr)
                    GatherDescendantRuleRows(gr, destination);
                else if (item is AnalyzeResultsTreeViewItem resultItem)
                {
                    var ruleRow = AnalyzeResultsSelection.FindRegisteredRuleContainerParent(resultItem);
                    if (ruleRow != null && AnalyzeSystem.Rules.Contains(ruleRow.analyzeRule))
                    {
                        destination.Add(ruleRow);
                        GatherDescendantRuleRows(ruleRow, destination);
                    }
                }
            }
        }

        static void AddRangeUnique<T>(List<T> destination, IEnumerable<T> source, HashSet<T> seen) where T : class
        {
            foreach (var item in source)
            {
                if (seen.Add(item))
                    destination.Add(item);
            }
        }

        IEnumerable<int> ActiveSelectionIds(IReadOnlyList<int> selectionSnapshot)
        {
            if (selectionSnapshot != null)
                return selectionSnapshot;
            return GetSelection();
        }

        void CollectResultItemsFromIds(IEnumerable<int> ids, List<AnalyzeResultsTreeViewItem> outItems)
        {
            foreach (int id in ids)
            {
                var item = FindItem(id, rootItem) as AnalyzeResultsTreeViewItem;
                if (item != null)
                    outItems.Add(item);
            }
        }

        bool TryCollectOnlyResultItems(IList<int> selectedIds, out List<AnalyzeResultsTreeViewItem> resultItems)
        {
            resultItems = new List<AnalyzeResultsTreeViewItem>(selectedIds.Count);
            foreach (int id in selectedIds)
            {
                var ri = FindItem(id, rootItem) as AnalyzeResultsTreeViewItem;
                if (ri == null)
                    return false;
                resultItems.Add(ri);
            }

            return true;
        }

        static bool DirectChildHasErrorResult(AnalyzeRuleTreeViewItem ruleRow)
        {
            if (ruleRow.children == null)
                return false;
            foreach (var child in ruleRow.children)
            {
                if (child is AnalyzeResultsTreeViewItem resultItem && resultItem.IsError)
                    return true;
            }

            return false;
        }

        static bool AnyItemsReportError(IReadOnlyList<AnalyzeResultsTreeViewItem> items)
        {
            if (items == null)
                return false;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].IsError)
                    return true;
            }

            return false;
        }

        bool SelectionIncludesGroupOrRuleRow(IEnumerable<int> selectedIds)
        {
            foreach (int id in selectedIds)
            {
                var item = FindItem(id, rootItem);
                if (item is AnalyzeRuleTreeViewItem || item is AnalyzeGroupTreeViewItem)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Expands the current selection into registered rule rows (selected rules plus every rule under a selected group folder).
        /// </summary>
        void PerformActionForEntireRuleSelection(Action<AnalyzeRuleTreeViewItem> action, IReadOnlyList<int> selectionSnapshot = null)
        {
            var activeRuleRows = new List<AnalyzeRuleTreeViewItem>();
            AppendRuleRowsExpandedFromSelectionIds(ActiveSelectionIds(selectionSnapshot), activeRuleRows);
            PerformActionForRuleRows(action, activeRuleRows);
        }

        /// <summary>
        /// Runs <paramref name="action"/> on <paramref name="activeRuleRows"/> and each descendant rule row under those nodes, deduplicated.
        /// </summary>
        static void PerformActionForRuleRows(Action<AnalyzeRuleTreeViewItem> action, List<AnalyzeRuleTreeViewItem> activeRuleRows)
        {
            var inheritSelection = new List<AnalyzeRuleTreeViewItem>();
            foreach (var selected in activeRuleRows)
                GatherDescendantRuleRows(selected, inheritSelection);

            var seen = new HashSet<AnalyzeRuleTreeViewItem>();
            var entireSelection = new List<AnalyzeRuleTreeViewItem>();
            AddRangeUnique(entireSelection, activeRuleRows, seen);
            AddRangeUnique(entireSelection, inheritSelection, seen);

            foreach (var ruleRow in entireSelection)
                action(ruleRow);
        }

        /// <summary>
        /// Runs analyze for each rule in the active selection (or <paramref name="selectionSnapshot"/> when provided).
        /// </summary>
        public void RunAllSelectedRules(IReadOnlyList<int> selectionSnapshot = null)
        {
            ProcessSelectedRules(selectionSnapshot, false);
        }

        /// <summary>
        /// Runs full rule fix for each selected rule (or snapshot).
        /// </summary>
        public void FixAllSelectedRules(IReadOnlyList<int> selectionSnapshot = null)
        {
            ProcessSelectedRules(selectionSnapshot, true);
        }

        private void ProcessSelectedRules(IReadOnlyList<int> selectionSnapshot, bool fixRules)
        {
            PerformActionForEntireRuleSelection(ruleRow =>
            {
                if (fixRules)
                {
                    AnalyzeSystem.FixIssues(ruleRow.analyzeRule);
                }
                var results = AnalyzeSystem.RefreshAnalysis(ruleRow.analyzeRule);
                BuildResults(ruleRow, results);
            }, selectionSnapshot);
            Reload();
            UpdateSelections(GetSelection());

        }

        public void ClearAll()
        {
            var root = rootItem.children[0] as AnalyzeGroupTreeViewItem;
            if (root == null)
            {
                Debug.LogError("Error: Structure of AnalyzeRule tree view is different to expected.");
                return;
            }

            var ruleRows = new List<AnalyzeRuleTreeViewItem>();
            GatherDescendantRuleRows(root, ruleRows);
            PerformActionForRuleRows(ruleRow =>
            {
                AnalyzeSystem.ClearAnalysis(ruleRow.analyzeRule);
                BuildResults(ruleRow, new List<AnalyzeRule.AnalyzeResult>());
            }, ruleRows);

            Reload();
            UpdateSelections(GetSelection());
        }

        /// <summary>
        /// Clears cached results for each rule in the current selection (or <paramref name="selectionSnapshot"/>).
        /// </summary>
        public void ClearAllSelectedRules(IReadOnlyList<int> selectionSnapshot = null)
        {
            PerformActionForEntireRuleSelection(ruleRow =>
            {
                AnalyzeSystem.ClearAnalysis(ruleRow.analyzeRule);
                BuildResults(ruleRow, new List<AnalyzeRule.AnalyzeResult>());
            }, selectionSnapshot);

            Reload();
            UpdateSelections(GetSelection());
        }

        public bool SelectionContainsFixableRule { get; private set; }

        /// <summary>
        /// True when the selection includes at least one <see cref="AnalyzeGroupTreeViewItem"/> or <see cref="AnalyzeRuleTreeViewItem"/> row.
        /// </summary>
        public bool SelectionContainsRuleContainer { get; private set; }

        public bool SelectionContainsErrors { get; private set; }

        /// <summary>
        /// True when every selected row is an <see cref="AnalyzeResultsTreeViewItem"/> (no rule or folder row is selected).
        /// Used so the results context menu takes precedence over the rule menu when selection contains only result rows.
        /// </summary>
        public bool SelectionContainsOnlyAnalyzeResults { get; private set; }

        /// <summary>
        /// True when the selection is only analyze result rows, includes at least one error-level issue,
        /// and every distinct owning rule supports partial fix (<see cref="AnalyzeRule.SupportsFixSelectedResults"/> / <see cref="AnalyzeRule.CanFix"/>).
        /// </summary>
        public bool SelectionContainsFixableSelectedResults { get; private set; }

        /// <summary>
        /// Toolbar label for Fix: always <c>Fix Selected Rule(s) (n)</c> with <c>n</c> = number of selected tree rows (matches <see cref="AnalyzeRuleGUI"/> enable logic).
        /// </summary>
        internal void GetFixToolbarLabel(out string text, out string tooltip)
        {
            var selectedIds = GetSelection();
            int n = selectedIds != null ? selectedIds.Count : 0;

            const string tooltipText =
                "Apply fixes for the selected analyze rows (Addressables groups with errors).";

            var canFixToolbar = SelectionContainsFixableSelectedResults
                || (SelectionContainsRuleContainer && SelectionContainsFixableRule && SelectionContainsErrors);

            if (canFixToolbar)
                text = FormatFixSelectedRulesLabel(n);
            else
                text = "Fix Selected Rules (0)";

            tooltip = tooltipText;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            var normalized = NormalizeSelection(selectedIds);
            if (!SelectionListsEqual(selectedIds, normalized))
            {
                SetSelection(normalized, TreeViewSelectionOptions.FireSelectionChanged);
                return;
            }

            UpdateSelections(normalized);
        }

        /// <summary>
        /// First drops any id that has another selected node on its parent chain (so e.g. you cannot keep a folder and Ctrl+add its nested rule or issue row).
        /// Then drops <see cref="AnalyzeResultsTreeViewItem"/> rows that sit under a selected <see cref="AnalyzeRuleTreeViewItem"/> when both appear in the selection (same rule subtree).
        /// </summary>
        List<int> NormalizeSelection(IList<int> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count <= 1)
                return selectedIds == null ? new List<int>() : new List<int>(selectedIds);

            var selectedSet = new HashSet<int>(selectedIds);
            var afterAncestorPrune = new List<int>();
            foreach (int id in selectedIds)
            {
                if (HasSelectedAncestor(id, selectedSet))
                    continue;
                afterAncestorPrune.Add(id);
            }

            if (afterAncestorPrune.Count <= 1)
                return afterAncestorPrune;

            var ruleIds = new HashSet<int>();
            foreach (int id in afterAncestorPrune)
            {
                var item = FindItem(id, rootItem);
                if (item is AnalyzeRuleTreeViewItem)
                    ruleIds.Add(id);
            }

            if (ruleIds.Count == 0)
                return afterAncestorPrune;

            // Rule row + selected issues under that rule would mix toolbar semantics; keep the rule row only.
            var final = new List<int>();
            foreach (int id in afterAncestorPrune)
            {
                var item = FindItem(id, rootItem);
                if (item is AnalyzeResultsTreeViewItem resultItem)
                {
                    var ruleParent = AnalyzeResultsSelection.FindRegisteredRuleContainerParent(resultItem);
                    if (ruleParent != null && ruleIds.Contains(ruleParent.id))
                        continue;
                }

                final.Add(id);
            }

            return final;
        }

        bool HasSelectedAncestor(int itemId, HashSet<int> selectedSet)
        {
            var item = FindItem(itemId, rootItem);
            if (item == null)
                return false;

#if UNITY_6000_2_OR_NEWER
            for (TreeViewItem<int> p = item.parent; p != null; p = p.parent)
#else
            for (TreeViewItem p = item.parent; p != null; p = p.parent)
#endif
            {
                if (selectedSet.Contains(p.id))
                    return true;
            }

            return false;
        }

        static bool SelectionListsEqual(IList<int> a, IList<int> b)
        {
            if (a == null && b == null)
                return true;
            if (a == null || b == null)
                return false;
            if (a.Count != b.Count)
                return false;
            var set = new HashSet<int>(a);
            foreach (int x in b)
            {
                if (!set.Contains(x))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Allows Ctrl/Cmd multi-selection on grouping folders, registered rule rows, and result rows (siblings such as two section folders are allowed).
        /// Descendant rows added while an ancestor stays selected are removed in <see cref="NormalizeSelection"/>.
        /// </summary>
        protected override bool CanMultiSelect(TreeViewItemAdapter item)
        {
            return item is AnalyzeResultsTreeViewItem || item is AnalyzeGroupTreeViewItem || item is AnalyzeRuleTreeViewItem;
        }

        /// <summary>
        /// Refreshes toolbar and context-menu affordances from the current selection ids (after <see cref="NormalizeSelection"/>).
        /// </summary>
        internal void UpdateSelections(IList<int> selectedIds)
        {
            SelectionContainsOnlyAnalyzeResults = false;

            var rawRuleRows = new List<AnalyzeRuleTreeViewItem>();
            AppendRuleRowsExpandedFromSelectionIds(selectedIds, rawRuleRows);

            var seenRuleRows = new HashSet<AnalyzeRuleTreeViewItem>();
            var allRuleRows = new List<AnalyzeRuleTreeViewItem>();
            AddRangeUnique(allRuleRows, rawRuleRows, seenRuleRows);

            SelectionContainsErrors = false;
            SelectionContainsFixableRule = false;
            foreach (var ruleRow in allRuleRows)
            {
                // short circuit if we have errors and fixable rules
                if (SelectionContainsErrors && SelectionContainsFixableRule)
                    break;

                if (!SelectionContainsErrors && DirectChildHasErrorResult(ruleRow))
                {
                    SelectionContainsErrors = true;
                }
                if (!SelectionContainsFixableRule && ruleRow.analyzeRule.CanFix)
                {
                    SelectionContainsFixableRule = true;
                }
            }

            SelectionContainsRuleContainer = SelectionIncludesGroupOrRuleRow(selectedIds);

            SelectionContainsFixableSelectedResults = false;
            if (selectedIds.Count > 0)
            {
                if (TryCollectOnlyResultItems(selectedIds, out var resultItems))
                {
                    SelectionContainsOnlyAnalyzeResults = resultItems.Count > 0;

                    if (resultItems.Count > 0)
                        SelectionContainsFixableSelectedResults =
                            GetFixSelectedResultsMenuState(resultItems) == FixMenuState.Enabled;
                }
            }
        }

        protected override void ContextClicked()
        {
            IList<int> selectedIds = GetSelection();

            if (SelectionContainsOnlyAnalyzeResults && selectedIds != null && selectedIds.Count > 0)
            {
                var resultOnlyItems = new List<AnalyzeResultsTreeViewItem>();
                CollectResultItemsFromIds(selectedIds, resultOnlyItems);

                if (resultOnlyItems.Count > 0)
                    ShowAnalyzeResultsContextMenu(resultOnlyItems);
                return;
            }

            if (SelectionContainsRuleContainer)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Run Analyze Rule"), false, () => RunAllSelectedRules());
                menu.AddItem(new GUIContent("Clear Analyze Results"), false, () => ClearAllSelectedRules());

                var ruleFixMenuLabel = FormatFixSelectedRulesLabel(selectedIds?.Count ?? 0);
                if (SelectionContainsFixableRule && SelectionContainsErrors)
                    menu.AddItem(new GUIContent(ruleFixMenuLabel), false, () => FixAllSelectedRules());
                else
                    menu.AddDisabledItem(new GUIContent(ruleFixMenuLabel));

                if (selectedIds != null && selectedIds.Count == 1)
                {
                    var analyzeRuleRow = FindItem(selectedIds[0], rootItem) as AnalyzeRuleTreeViewItem;
                    if (analyzeRuleRow != null)
                    {
                        foreach (var customMenuItem in analyzeRuleRow.analyzeRule.GetCustomContextMenuItems())
                        {
                            if (customMenuItem.MenuEnabled)
                                menu.AddItem(new GUIContent(customMenuItem.MenuName), customMenuItem.ToggledOn, () => customMenuItem.MenuAction());
                            else
                                menu.AddDisabledItem(new GUIContent(customMenuItem.MenuName));
                        }
                    }
                }

                menu.ShowAsContext();
                Repaint();
                return;
            }

            if (selectedIds != null)
            {
                var items = new List<AnalyzeResultsTreeViewItem>();
                CollectResultItemsFromIds(selectedIds, items);

                if (items.Count > 0)
                    ShowAnalyzeResultsContextMenu(items);
            }
        }

        void ShowAnalyzeResultsContextMenu(List<AnalyzeResultsTreeViewItem> items)
        {
            var objects = new HashSet<Object>();
            foreach (AnalyzeResultsTreeViewItem viewItem in items)
            {
                foreach (var itemResult in viewItem.results)
                {
                    Object o = AnalyzeResultsTreeViewItem.GetResultObject(itemResult.resultName);
                    if (o != null)
                        objects.Add(o);
                }
            }

            var fixLabel = FormatFixSelectedRulesLabel(items.Count);
            var fixState = GetFixSelectedResultsMenuState(items);
            var hasAssets = objects.Count > 0;
            if (!hasAssets && fixState == FixMenuState.Hidden)
                return;

            var menu = new GenericMenu();
            if (hasAssets)
            {
                menu.AddItem(new GUIContent(objects.Count > 1 ? "Select Assets" : "Select Asset"), false, () =>
                {
                    var objectArray = new Object[objects.Count];
                    objects.CopyTo(objectArray);
                    Selection.objects = objectArray;
                    foreach (Object o in objects)
                    {
                        EditorGUIUtility.PingObject(o);
                        return;
                    }
                });
            }

            if (fixState == FixMenuState.Enabled)
            {
                if (hasAssets)
                    menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent(fixLabel), false, () => FixSelectedResultsForItems(items));
            }
            else if (fixState == FixMenuState.Disabled)
            {
                if (hasAssets)
                    menu.AddSeparator(string.Empty);
                menu.AddDisabledItem(new GUIContent(fixLabel));
            }

            menu.ShowAsContext();
        }

        enum FixMenuState
        {
            Hidden,
            Disabled,
            Enabled
        }

        FixMenuState GetFixSelectedResultsMenuState(IReadOnlyList<AnalyzeResultsTreeViewItem> items)
        {
            if (items == null || items.Count == 0)
                return FixMenuState.Hidden;
            if (!AnyItemsReportError(items))
                return FixMenuState.Disabled;

            var containers = new HashSet<AnalyzeRuleTreeViewItem>();
            foreach (var item in items)
            {
                var c = AnalyzeResultsSelection.FindRegisteredRuleContainerParent(item);
                if (c == null || !AnalyzeSystem.Rules.Contains(c.analyzeRule))
                    return FixMenuState.Disabled;
                containers.Add(c);
            }

            foreach (var c in containers)
            {
                var rule = c.analyzeRule;
                if (!rule.SupportsFixSelectedResults || !rule.CanFix)
                    return FixMenuState.Hidden;
            }

            return FixMenuState.Enabled;
        }

        void FixSelectedResultsForItems(IReadOnlyList<AnalyzeResultsTreeViewItem> items)
        {
            if (items == null || items.Count == 0)
                return;

            var byRuleContainer = new Dictionary<AnalyzeRuleTreeViewItem, List<AnalyzeResultsTreeViewItem>>();
            foreach (var item in items)
            {
                var c = AnalyzeResultsSelection.FindRegisteredRuleContainerParent(item);
                if (c == null)
                    continue;
                if (!byRuleContainer.TryGetValue(c, out var list))
                {
                    list = new List<AnalyzeResultsTreeViewItem>();
                    byRuleContainer[c] = list;
                }

                list.Add(item);
            }

            foreach (var kvp in byRuleContainer)
            {
                var ruleRow = kvp.Key;
                var rule = ruleRow.analyzeRule;
                if (!rule.SupportsFixSelectedResults || !rule.CanFix)
                    continue;

                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var item in kvp.Value)
                {
                    foreach (var r in item.results)
                        names.Add(r.resultName);
                }

                if (names.Count == 0)
                    continue;

                AnalyzeSystem.RefreshAnalysis(rule);
                AnalyzeSystem.FixSelectedResults(rule, names);
                AnalyzeSystem.RefreshAnalysis(rule);
            }

            Reload();
            UpdateSelections(GetSelection());
        }

        static string FormatFixSelectedRulesLabel(int count)
        {
            return count == 1
                ? $"Fix Selected Rule ({count})"
                : $"Fix Selected Rules ({count})";
        }

        /// <summary>
        /// Toolbar entry: fix selected issue rows when the selection contains only <see cref="AnalyzeResultsTreeViewItem"/> entries.
        /// </summary>
        internal void FixSelectedResultsFromCurrentSelection(IReadOnlyList<int> selectionSnapshot = null)
        {
            var items = new List<AnalyzeResultsTreeViewItem>();
            CollectResultItemsFromIds(ActiveSelectionIds(selectionSnapshot), items);

            if (items.Count > 0)
                FixSelectedResultsForItems(items);
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as AnalyzeResultsTreeViewItem;
            if (item != null)
                item.DoubleClicked();
        }

        protected override TreeViewItemAdapter BuildRootAdapter()
        {
            m_CurrentDepth = 0;
            var root = new TreeViewItemAdapter(-1, -1);
            root.children = TreeViewItemAdapter.EmptyList();

            string baseName = "Analyze Rules";
            string fixableRules = "Fixable Rules";
            string unfixableRules = "Unfixable Rules";

            AnalyzeSystem.TreeView = this;

            var baseViewItem = new AnalyzeGroupTreeViewItem(baseName.GetHashCode(), m_CurrentDepth, baseName);
            baseViewItem.children = TreeViewItemAdapter.EmptyList();

            root.AddChild(baseViewItem);

            m_CurrentDepth++;

            var fixable = new AnalyzeGroupTreeViewItem(fixableRules.GetHashCode(), m_CurrentDepth, fixableRules);
            var unfixable = new AnalyzeGroupTreeViewItem(unfixableRules.GetHashCode(), m_CurrentDepth, unfixableRules);

            baseViewItem.AddChild(fixable);
            baseViewItem.AddChild(unfixable);

            m_CurrentDepth++;

            var fixableRuleList = new List<AnalyzeRule>();
            var unfixableRuleList = new List<AnalyzeRule>();
            foreach (var rule in AnalyzeSystem.Rules)
            {
                if (rule.CanFix)
                    fixableRuleList.Add(rule);
                else
                    unfixableRuleList.Add(rule);
            }

            fixableRuleList.Sort((a, b) =>
                string.Compare(a.ruleName, b.ruleName, StringComparison.OrdinalIgnoreCase));
            unfixableRuleList.Sort((a, b) =>
                string.Compare(a.ruleName, b.ruleName, StringComparison.OrdinalIgnoreCase));

            foreach (var rule in fixableRuleList)
                fixable.AddChild(new AnalyzeRuleTreeViewItem(rule.ruleName.GetHashCode(), m_CurrentDepth, rule));

            foreach (var rule in unfixableRuleList)
                unfixable.AddChild(new AnalyzeRuleTreeViewItem(rule.ruleName.GetHashCode(), m_CurrentDepth, rule));

            m_CurrentDepth++;

            int index = 0;
            var ruleRows = new List<AnalyzeRuleTreeViewItem>();
            GatherDescendantRuleRows(baseViewItem, ruleRows);
            foreach (var ruleRow in ruleRows)
            {
                if (ruleRow == null)
                    continue;

                EditorUtility.DisplayProgressBar("Calculating Analyze Results...", ruleRow.displayName, index / (float)ruleRows.Count);
                if (AnalyzeSystem.AnalyzeData.Data.ContainsKey(ruleRow.analyzeRule.ruleName))
                    BuildResults(ruleRow, AnalyzeSystem.AnalyzeData.Data[ruleRow.analyzeRule.ruleName]);

                index++;
            }

            EditorUtility.ClearProgressBar();
            return root;
        }

        readonly Dictionary<string, AnalyzeResultsTreeViewItem> pathToAnalyzeResults =
            new Dictionary<string, AnalyzeResultsTreeViewItem>();

        /// <summary>
        /// Builds nested <see cref="AnalyzeResultsTreeViewItem"/> rows under <paramref name="root"/> from flat analyze results (shared path prefixes merge).
        /// </summary>
        void BuildResults(TreeViewItemAdapter root, List<AnalyzeRule.AnalyzeResult> ruleResults)
        {
            pathToAnalyzeResults.Clear();
            // Sequential ids avoid HashCode(name) collisions mapping different paths to the same row id, which breaks
            // FindItem and partial-fix selection when multi-selecting issue rows across sibling branches.
            int nextNewResultRowId = 0;
            int resultCount = ruleResults.Count;
            int updateFrequency = Mathf.Max(resultCount / 10, 1);
            int denom = Mathf.Max(resultCount, 1);

            for (int index = 0; index < resultCount; ++index)
            {
                var result = ruleResults[index];
                if (index == 0 || index % updateFrequency == 0)
                    EditorUtility.DisplayProgressBar("Building Results Tree...", result.resultName, (float)index / denom);

                var resPath = result.resultName.Split(AnalyzeRule.kDelimiter);
                string name = string.Empty;
                TreeViewItemAdapter parent = root;

                for (int i = 0; i < resPath.Length; i++)
                {
                    name += resPath[i];

                    if (!pathToAnalyzeResults.ContainsKey(name))
                    {
                        int rowId = HashCode.Combine(root.id, nextNewResultRowId++);
                        AnalyzeResultsTreeViewItem item =
                            new AnalyzeResultsTreeViewItem(rowId, i + m_CurrentDepth, resPath[i], result.severity, result);
                        pathToAnalyzeResults.Add(name, item);
                        parent.AddChild(item);
                        parent = item;
                    }
                    else
                    {
                        var targetItem = pathToAnalyzeResults[name];
                        targetItem.results.Add(result);
                        parent = targetItem;
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            (root as AnalyzeTreeViewItemBase)?.AddIssueCountToName();
            foreach (var node in root.children)
                (node as AnalyzeTreeViewItemBase)?.AddIssueCountToName();

            AnalyzeSystem.SerializeData();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as AnalyzeResultsTreeViewItem;
            if (item != null && item.severity != MessageType.None)
            {
                var icon = GetIconForSeverity(item.severity);
                UnityEngine.GUI.Label(
                    new Rect(args.rowRect.x + baseIndent, args.rowRect.y, args.rowRect.width - baseIndent,
                        args.rowRect.height), new GUIContent(icon, string.Empty));
            }

            base.RowGUI(args);
        }

        Texture2D m_ErrorIcon;
        Texture2D m_WarningIcon;
        Texture2D m_InfoIcon;

        Texture2D GetIconForSeverity(MessageType severity)
        {
            FindMessageIcons();
            switch (severity)
            {
                case MessageType.Info:
                    return m_InfoIcon;
                case MessageType.Warning:
                    return m_WarningIcon;
                case MessageType.Error:
                    return m_ErrorIcon;
                default:
                    return null;
            }
        }

        void FindMessageIcons()
        {
            if (m_ErrorIcon != null)
                return;
            m_ErrorIcon = EditorGUIUtility.FindTexture("console.errorIcon");
            m_WarningIcon = EditorGUIUtility.FindTexture("console.warnicon");
            m_InfoIcon = EditorGUIUtility.FindTexture("console.infoIcon");
        }
    }
}
