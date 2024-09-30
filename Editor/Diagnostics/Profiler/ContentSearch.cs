#if ENABLE_ADDRESSABLE_PROFILER && UNITY_2022_2_OR_NEWER

using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.GUIElements;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.UIElements;
using UnityEngine.ResourceManagement.Profiling;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.UIElements;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    internal class ContentSearch
    {
        [System.Flags]
        private enum BuildInclusion
        {
            None = 0,
            Explicit = 1,
            Implicit = 2
        }

        private FilterString m_SearchQuery = new FilterString();

        private HashSet<AssetType> m_TypeSearches = new HashSet<AssetType>();
        private BuildInclusion m_BuildInclusionSearchFlags = BuildInclusion.None;
        private ContentStatus m_StatusSearchFlags = ContentStatus.None;
        private BundleSource m_BundleSourceSearchFlags = BundleSource.None;
        private NumericQuery m_HandlesSearch;
        private NumericQuery m_RefsBySearch;
        private NumericQuery m_RefsToSearch;

        private ToolbarSearchField m_SearchField;


        private const string k_MenuTypeImplicit = "Implicit";
        private const string k_MenuTypeExplicit = "Explicit";
        private const string k_MenuHandles = "Handles";
        private const string k_MenuStatus = "Status/";
        private const string k_MenuType = "Type/";
        private const string k_MenuSource = "Source/";
        private const string k_MenuReferencesTo = "References To";
        private const string k_MenuReferencedBy = "Referenced By";


        public void Clear()
        {
            m_SearchQuery.Clear();
            m_TypeSearches.Clear();
            m_BuildInclusionSearchFlags = BuildInclusion.None;
            m_StatusSearchFlags = ContentStatus.None;
            m_BundleSourceSearchFlags = BundleSource.None;
            m_HandlesSearch.Clear();
            m_RefsBySearch.Clear();
            m_RefsToSearch.Clear();
        }

        public ContentSearch()
        {
            InitialiseLongForms();
        }

        public void InitialiseFilterMenu(DropdownMenu menu, ToolbarSearchField searchField)
        {
            m_SearchField = searchField;
            menu.AppendAction(k_MenuTypeExplicit, SearchMenuActionSelectedCallback);
            menu.AppendAction(k_MenuTypeImplicit, SearchMenuActionSelectedCallback);
            menu.AppendAction(k_MenuHandles, SearchMenuActionSelectedCallback);

            foreach (Enum e in Enum.GetValues(typeof(ContentStatus)))
            {
                string enumString = e.ToString();
                if (enumString == "None")
                    continue;
                menu.AppendAction(k_MenuStatus + enumString, SearchMenuActionSelectedCallback);
            }

            foreach (Enum e in Enum.GetValues(typeof(AssetType)))
            {
                string enumString = e.ToString();
                if (enumString == "Other")
                    continue;
                menu.AppendAction(k_MenuType + enumString, SearchMenuActionSelectedCallback);
            }

            foreach (Enum e in Enum.GetValues(typeof(BundleSource)))
            {
                string enumString = e.ToString();
                if (enumString == "None")
                    continue;
                menu.AppendAction(k_MenuSource + enumString, SearchMenuActionSelectedCallback);
            }

            menu.AppendAction(k_MenuReferencesTo, SearchMenuActionSelectedCallback);
            menu.AppendAction(k_MenuReferencedBy, SearchMenuActionSelectedCallback);
        }

        private void SearchMenuActionSelectedCallback(DropdownMenuAction action)
        {
            switch (action.name)
            {
                case k_MenuTypeExplicit: PushSearchFilter("Type:Explicit"); return;
                case k_MenuTypeImplicit: PushSearchFilter("Type:Implicit"); return;
                case k_MenuHandles: PushSearchFilter("Handles:>0"); return;
                case k_MenuReferencesTo: PushSearchFilter("RefsTo:>0"); return;
                case k_MenuReferencedBy: PushSearchFilter("RefsBy:>0"); return;
            }
            if (action.name.StartsWith(k_MenuStatus, StringComparison.Ordinal))
            {
                PushSearchFilter("Status:" + action.name.Substring(k_MenuStatus.Length));
                return;
            }
            if (action.name.StartsWith(k_MenuType, StringComparison.Ordinal))
            {
                PushSearchFilter("Type:" + action.name.Substring(k_MenuType.Length));
                return;
            }
            if (action.name.StartsWith(k_MenuSource, StringComparison.Ordinal))
            {
                PushSearchFilter("Source:" + action.name.Substring(k_MenuSource.Length));
                return;
            }
        }

        private void PushSearchFilter(string filterString)
        {
            string value = m_SearchField.value;
            if (value.Length == 0)
                m_SearchField.value = filterString;
            else if (value[value.Length - 1] == ' ')
                m_SearchField.value = value + filterString;
            else
                m_SearchField.value = $"{value} {filterString}";
        }

        /// <summary>
        /// adds any search filter strings to be used to identify a query
        /// </summary>
        private void InitialiseLongForms()
        {
            m_SearchQuery.AddFilterLongHand("handles", 'h');
            m_SearchQuery.AddFilterLongHand("type", 't');
            m_SearchQuery.AddFilterLongHand("assettype", 't');
            m_SearchQuery.AddFilterLongHand("status", 's');
            m_SearchQuery.AddFilterLongHand("refsto", 'r');
            m_SearchQuery.AddFilterLongHand("rt", 'r');
            m_SearchQuery.AddFilterLongHand("refsby", 'p');
            m_SearchQuery.AddFilterLongHand("rb", 'p');
            m_SearchQuery.AddFilterLongHand("bundlesource", 'b');
            m_SearchQuery.AddFilterLongHand("source", 'b');
            m_SearchQuery.AddFilterLongHand("bs", 'b');
        }

        /// <summary>
        /// Take the given string and convert to content search queries
        /// </summary>
        /// <param name="value">string to parse</param>
        public void ProcessSearchValue(string value)
        {
            Clear();
            m_SearchQuery.ProcessSearchValue(value);
            foreach (Filter filter in m_SearchQuery.Filters)
                ProcessFilter(filter.FilterIdentifier, filter.FilterValue);
        }

        private void ProcessFilter(char filterChar, string filterValue)
        {
            if (filterChar == 't' || filterChar == 'T')
            {
                if (string.Equals(filterValue, "Implicit", StringComparison.OrdinalIgnoreCase))
                    m_BuildInclusionSearchFlags |= BuildInclusion.Implicit;
                else if (string.Equals(filterValue, "Explicit", StringComparison.OrdinalIgnoreCase))
                    m_BuildInclusionSearchFlags |= BuildInclusion.Explicit;
                else
                {
                    foreach (Enum e in Enum.GetValues(typeof(AssetType)))
                    {
                        if (string.Equals(filterValue, e.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            m_TypeSearches.Add((AssetType)e);
                            break;
                        }
                    }
                }
            }
            else if (filterChar == 's' || filterChar == 'S')
            {
                foreach (Enum e in Enum.GetValues(typeof(ContentStatus)))
                {
                    if (string.Equals(filterValue, e.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        m_StatusSearchFlags |= (ContentStatus)e;
                        break;
                    }
                }
            }
            else if (filterChar == 'b' || filterChar == 'B')
            {
                foreach (Enum e in Enum.GetValues(typeof(BundleSource)))
                {
                    if (string.Equals(filterValue, e.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        m_BundleSourceSearchFlags |= (BundleSource)e;
                        break;
                    }
                }
            }
            else if (filterChar == 'h' || filterChar == 'H')
                m_HandlesSearch.Parse(filterValue);
            else if (filterChar == 'r' || filterChar == 'R')
                m_RefsToSearch.Parse(filterValue);
            else if (filterChar == 'p' || filterChar == 'P')
                m_RefsBySearch.Parse(filterValue);
        }

        public bool IsValidSearch(GroupData value)
        {
            if (!m_SearchQuery.IsValid)
                return true;
            if (!IsNameValidSearch(value)
                || m_StatusSearchFlags != ContentStatus.None
                || m_BundleSourceSearchFlags != BundleSource.None
                || m_BuildInclusionSearchFlags != BuildInclusion.None
                || m_HandlesSearch.IsValid || m_RefsBySearch.IsValid || m_RefsToSearch.IsValid)
                return false;

            if (m_HandlesSearch.IsValid && !m_HandlesSearch.Evaluate(value.AddressableHandles))
                return false;
            if (m_RefsBySearch.IsValid && !m_RefsBySearch.Evaluate(value.ReferencesToThis.Count))
                return false;
            if (m_RefsToSearch.IsValid && !m_RefsToSearch.Evaluate(value.ThisReferencesOther.Count))
                return false;
            return true;
        }

        public bool IsValidSearch(BundleData value)
        {
            if (!m_SearchQuery.IsValid)
                return true;

            if (!IsNameValidSearch(value)
                || m_BuildInclusionSearchFlags != BuildInclusion.None)
                return false;

            if (m_StatusSearchFlags != ContentStatus.None)
            {
                if (!m_StatusSearchFlags.HasFlag(value.Status))
                    return false;
            }

            if (m_BundleSourceSearchFlags != BundleSource.None)
            {
                if (!m_BundleSourceSearchFlags.HasFlag(value.Source))
                    return false;
            }

            if (m_HandlesSearch.IsValid && !m_HandlesSearch.Evaluate(value.AddressableHandles))
                return false;
            if (m_RefsBySearch.IsValid && !m_RefsBySearch.Evaluate(value.ReferencesToThis.Count))
                return false;
            if (m_RefsToSearch.IsValid && !m_RefsToSearch.Evaluate(value.ThisReferencesOther.Count))
                return false;

            return true;
        }

        public bool IsValidSearch(AssetData value)
        {
            if (!m_SearchQuery.IsValid)
                return true;

            if (!IsNameValidSearch(value)
                || m_BundleSourceSearchFlags != BundleSource.None)
                return false;

            if (m_BuildInclusionSearchFlags != BuildInclusion.None)
            {
                if (!(m_BuildInclusionSearchFlags.HasFlag(BuildInclusion.Explicit) && !value.IsImplicit)
                    && !(m_BuildInclusionSearchFlags.HasFlag(BuildInclusion.Implicit) && value.IsImplicit))
                    return false;
            }
            if (m_StatusSearchFlags != ContentStatus.None)
            {
                if (!m_StatusSearchFlags.HasFlag(value.Status))
                    return false;
            }

            if (m_TypeSearches.Count > 0)
            {
                if (!m_TypeSearches.Contains(value.MainAssetType))
                    return false;
            }

            if (m_HandlesSearch.IsValid && !m_HandlesSearch.Evaluate(value.AddressableHandles))
                return false;
            if (m_RefsBySearch.IsValid && !m_RefsBySearch.Evaluate(value.ReferencesToThis.Count))
                return false;
            if (m_RefsToSearch.IsValid && !m_RefsToSearch.Evaluate(value.ThisReferencesOther.Count))
                return false;

            return true;
        }

        public bool IsValidSearch(ObjectData value)
        {
            if (!m_SearchQuery.IsValid)
                return true;
            if (!IsNameValidSearch(value)
                || m_BundleSourceSearchFlags != BundleSource.None
                || m_BuildInclusionSearchFlags != BuildInclusion.None
                || m_HandlesSearch.IsValid || m_RefsBySearch.IsValid || m_RefsToSearch.IsValid)
                return false;

            if (m_TypeSearches.Count > 0)
            {
                if (!m_TypeSearches.Contains(value.AssetType))
                    return false;
            }
            if (m_StatusSearchFlags != ContentStatus.None)
            {
                if (!m_StatusSearchFlags.HasFlag(value.Status))
                    return false;
            }

            return true;
        }

        private bool IsNameValidSearch(ContentData content)
        {
            foreach (string value in m_SearchQuery.StringFilters)
            {
                if (!AddressableAssetUtility.StringContains(content.Name, value, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }
    }
}

#endif
