using System.IO;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Centralized constants shared across the editor tooling.
    /// </summary>
    internal static class Constants
    {
        public const string PackageShortName = "Auto Group Generator";
        public const string PackageShortDescription = "Automated Addressable Grouping Tool";

        /// <summary>
        /// Menu definitions for the editor UI.
        /// </summary>
        public static class Menus
        {
            public const int AutoGroupGeneratorMenuPriority = 2070;
            public const string Root = "Window/Asset Management/Addressables/";
            public const string AutoGroupGeneratorMenuPath = Root + PackageShortName;
        }

        /// <summary>
        /// Context menu definitions for asset creation.
        /// </summary>
        public static class ContextMenus
        {
            public const string Root = "Addressables/" + PackageShortName + "/";
            public const string RulesMenu = Root + "Rules/";
            public const string InputRulesMenu = RulesMenu + "Input Rules/";
            public const string OutputRulesMenu = RulesMenu + "Output Rules/";
        }

        /// <summary>
        /// Paths used for report persistence.
        /// </summary>
        public static class FilePaths
        {
            public static string PersistentDataFolder => Path.Combine(Application.persistentDataPath, PackageShortName) + "/";

            public static string DependencyGraphFilePath => Path.Combine(PersistentDataFolder, "DependencyGraph.txt");

            public static string SubGraphReportPath => Path.Combine(PersistentDataFolder, "SubGraphReport.txt");
            public static string GroupLayoutReportPath => Path.Combine(PersistentDataFolder, "GroupLayoutReport.txt");
            public static string CleanupReportPath => Path.Combine(PersistentDataFolder, "CleanupReport.txt");


            public static string SummaryReportPath => Path.Combine(Application.persistentDataPath, "SummaryReport.txt");
        }

    }
}
