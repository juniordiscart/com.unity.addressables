using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// The project configuration settings for addressables.
    /// </summary>
    public class ProjectConfigData
    {
        [Serializable]
        class ConfigSaveData
        {
            [FormerlySerializedAs("m_postProfilerEvents")]
            [SerializeField]
            internal bool postProfilerEventsInternal;

            [FormerlySerializedAs("m_localLoadSpeed")]
            [SerializeField]
            internal long localLoadSpeedInternal = 1024 * 1024 * 10;

            [FormerlySerializedAs("m_remoteLoadSpeed")]
            [SerializeField]
            internal long remoteLoadSpeedInternal = 1024 * 1024 * 1;

            [FormerlySerializedAs("m_hierarchicalSearch")]
            [SerializeField]
            internal bool hierarchicalSearchInternal = true;

            [SerializeField]
            internal int activePlayModeIndex = 0;

            [SerializeField]
            internal bool hideSubObjectsInGroupView = false;

            [SerializeField]
            internal bool showGroupsAsHierarchy = false;

            [SerializeField]
            internal bool generateBuildLayout = false;

            [SerializeField]
            internal ReportFileFormat buildLayoutReportFileFormat = ReportFileFormat.JSON;

            [SerializeField]
            internal List<string> buildReports = new List<string>();
#if UNITY_2022_2_OR_NEWER
            [SerializeField]
            internal bool autoOpenAddressablesReport = true;
            [SerializeField]
            internal bool userHasBeenInformedAboutBuildReportSettingPreBuild = false;
#endif
            [SerializeField]
            internal bool userHasBeenInformedAboutPathPairMigration = false;
        }

        static ConfigSaveData s_Data;

        /// <summary>
        /// Whether to display sub objects in the Addressables Groups window.
        /// </summary>
        public static bool ShowSubObjectsInGroupView
        {
            get
            {
                ValidateData();
                return !s_Data.hideSubObjectsInGroupView;
            }
            set
            {
                ValidateData();
                s_Data.hideSubObjectsInGroupView = !value;
                SaveData();
            }
        }

        /// <summary>
        /// Whether to generate the bundle build layout report.
        /// </summary>
        public static bool GenerateBuildLayout
        {
            get
            {
                ValidateData();
                return s_Data.generateBuildLayout;
            }
            set
            {
                ValidateData();
                if (s_Data.generateBuildLayout != value)
                {
                    s_Data.generateBuildLayout = value;
                    SaveData();
                }
            }
        }

#if UNITY_2022_2_OR_NEWER
        internal static bool AutoOpenAddressablesReport
        {
            get
            {
                ValidateData();
                return s_Data.autoOpenAddressablesReport;
            }
            set
            {
                ValidateData();
                if (s_Data.autoOpenAddressablesReport != value)
                {
                    s_Data.autoOpenAddressablesReport = value;
                    SaveData();
                }
            }
        }

        internal static bool UserHasBeenInformedAboutBuildReportSettingPreBuild
        {
            get
            {
                ValidateData();
                return s_Data.userHasBeenInformedAboutBuildReportSettingPreBuild;
            }
            set
            {
                ValidateData();
                if (s_Data.userHasBeenInformedAboutBuildReportSettingPreBuild != value)
                {
                    s_Data.userHasBeenInformedAboutBuildReportSettingPreBuild = value;
                    SaveData();
                }
            }
        }


#endif

        internal static bool UserHasBeenInformedAboutPathPairMigration
        {
            get
            {
                ValidateData();
                return s_Data.userHasBeenInformedAboutPathPairMigration;
            }
            set
            {
                ValidateData();
                if (s_Data.userHasBeenInformedAboutPathPairMigration != value)
                {
                    s_Data.userHasBeenInformedAboutPathPairMigration = value;
                    SaveData();
                }
            }
        }

        /// <summary>
        /// File formats supported for the bundle build layout report.
        /// </summary>
        public enum ReportFileFormat
        {
            /// <summary>
            /// When selected, a human readable .txt build layout will be generated alongside the .json file format
            /// </summary>
            TXT,

            /// <summary>
            /// The .json file format.
            /// </summary>
            JSON
        };

        /// <summary>
        /// File format of the bundle build layout report.
        /// </summary>
        public static ReportFileFormat BuildLayoutReportFileFormat
        {
            get
            {
                ValidateData();
                return s_Data.buildLayoutReportFileFormat;
            }
            set
            {
                ValidateData();
                if (s_Data.buildLayoutReportFileFormat != value)
                {
                    s_Data.buildLayoutReportFileFormat = value;
                    SaveData();
                }
            }
        }

        /// <summary>
        /// Returns the file paths of build reports used by the Build Reports window.
        /// </summary>
        public static List<string> BuildReportFilePaths
        {
            get
            {
                ValidateData();
                return s_Data.buildReports;
            }
        }

        /// <summary>
        /// Adds the filepath of a build report to be used by the Build Reports window
        /// </summary>
        /// <param name="reportFilePath">The file path to add</param>
        public static void AddBuildReportFilePath(string reportFilePath)
        {
            ValidateData();
            s_Data.buildReports.Add(reportFilePath);
            SaveData();
        }

        /// <summary>
        /// Removes the build report at index from the list of build reports shown in the Build Reports window
        /// </summary>
        /// <param name="index">The index of the build report to be removed</param>
        public static void RemoveBuildReportFilePathAtIndex(int index)
        {
            ValidateData();
            s_Data.buildReports.RemoveAt(index);
            SaveData();
        }

        /// <summary>
        /// Removes the build report located at reportFilePath from the list of build reports shown in the Build Reports window
        /// </summary>
        /// <param name="reportFilePath"></param>
        public static void RemoveBuildReportFilePath(string reportFilePath)
        {
            ValidateData();
            s_Data.buildReports.Remove(reportFilePath);
            SaveData();
        }

        /// <summary>
        /// Removes all build reports from the Build Reports window
        /// </summary>
        public static void ClearBuildReportFilePaths()
        {
            ValidateData();
            s_Data.buildReports.Clear();
            SaveData();
        }


        /// <summary>
        /// The active play mode data builder index.
        /// </summary>
        public static int ActivePlayModeIndex
        {
            get
            {
                ValidateData();
                return s_Data.activePlayModeIndex;
            }
            set
            {
                ValidateData();
                s_Data.activePlayModeIndex = value;
                SaveData();
            }
        }

        /// <summary>
        /// Whether to post profiler events in the ResourceManager profiler window.
        /// </summary>
        public static bool PostProfilerEvents
        {
            get
            {
                ValidateData();
                return s_Data.postProfilerEventsInternal;
            }
            set
            {
                ValidateData();
                s_Data.postProfilerEventsInternal = value;
                SaveData();
            }
        }

        /// <summary>
        /// The local bundle loading speed used in the Simulate Groups (advanced) playmode.
        /// </summary>
        public static long LocalLoadSpeed
        {
            get
            {
                ValidateData();
                return s_Data.localLoadSpeedInternal;
            }
            set
            {
                ValidateData();
                s_Data.localLoadSpeedInternal = value;
                SaveData();
            }
        }

        /// <summary>
        /// The remote bundle loading speed used in the Simulate Groups (advanced) playmode.
        /// </summary>
        public static long RemoteLoadSpeed
        {
            get
            {
                ValidateData();
                return s_Data.remoteLoadSpeedInternal;
            }
            set
            {
                ValidateData();
                s_Data.remoteLoadSpeedInternal = value;
                SaveData();
            }
        }

        /// <summary>
        /// Whether to allow searching for assets parsed hierarchally in the Addressables Groups window.
        /// </summary>
        public static bool HierarchicalSearch
        {
            get
            {
                ValidateData();
                return s_Data.hierarchicalSearchInternal;
            }
            set
            {
                ValidateData();
                s_Data.hierarchicalSearchInternal = value;
                SaveData();
            }
        }

        /// <summary>
        /// Whether to display groups names parsed hierarchally in the Addressables Groups window.
        /// </summary>
        public static bool ShowGroupsAsHierarchy
        {
            get
            {
                ValidateData();
                return s_Data.showGroupsAsHierarchy;
            }
            set
            {
                ValidateData();
                s_Data.showGroupsAsHierarchy = value;
                SaveData();
            }
        }

        static void ValidateData()
        {
            if (s_Data == null)
            {
                var dataPath = Path.GetFullPath(".");
                dataPath = dataPath.Replace("\\", "/");
                dataPath += "/Library/AddressablesConfig.dat";

                if (File.Exists(dataPath))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    try
                    {
                        using (FileStream file = new FileStream(dataPath, FileMode.Open, FileAccess.Read))
                        {
                            var data = bf.Deserialize(file) as ConfigSaveData;
                            if (data != null)
                            {
                                s_Data = data;
                            }
                        }
                    }
                    catch
                    {
                        //if the current class doesn't match what's in the file, Deserialize will throw. since this data is non-critical, we just wipe it
                        Addressables.LogWarning("Error reading Addressable Asset project config (play mode, etc.). Resetting to default.");
                        File.Delete(dataPath);
                    }
                }

                //check if some step failed.
                if (s_Data == null)
                {
                    s_Data = new ConfigSaveData();
                }

                if(s_Data.buildReports == null)
                    s_Data.buildReports = new List<string>();
            }
        }

        static void SaveData()
        {
            if (s_Data == null)
                return;

            var dataPath = Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += "/Library/AddressablesConfig.dat";

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(dataPath);

            bf.Serialize(file, s_Data);
            file.Close();
        }
    }
}
