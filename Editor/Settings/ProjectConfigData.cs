using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_6000_0_OR_NEWER
using System.Runtime.Serialization;
using System.Xml;
#else
using System.Runtime.Serialization.Formatters.Binary;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using static UnityEngine.AddressableAssets.ResourceLocators.BinaryContentCatalogData.ResourceLocator.ResourceLocation.Serializer;

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// The project configuration settings for addressables.
    /// </summary>
    public class ProjectConfigData
    {
        [Serializable]
#if UNITY_6000_0_OR_NEWER
        [DataContract]
#endif
        class ConfigSaveData
        {
            [FormerlySerializedAs("m_localLoadSpeed")]
            [SerializeField]
#if UNITY_6000_0_OR_NEWER
            [DataMember]
#endif
            internal long localLoadSpeedInternal = 1024 * 1024 * 10;

            [FormerlySerializedAs("m_remoteLoadSpeed")]
            [SerializeField]
#if UNITY_6000_0_OR_NEWER
            [DataMember]
#endif
            internal long remoteLoadSpeedInternal = 1024 * 1024 * 1;

            [FormerlySerializedAs("m_hierarchicalSearch")]
            [SerializeField]
#if UNITY_6000_0_OR_NEWER
            [DataMember]
#endif
            internal bool hierarchicalSearchInternal = true;

            [SerializeField]
#if UNITY_6000_0_OR_NEWER
            [DataMember]
#endif
            internal int activePlayModeIndex = 0;

            [SerializeField]
#if UNITY_6000_0_OR_NEWER
            [DataMember]
#endif
            internal bool hideSubObjectsInGroupView = false;

            [SerializeField]
#if UNITY_6000_0_OR_NEWER
            [DataMember]
#endif
            internal bool showGroupsAsHierarchy = false;

            [SerializeField]
#if UNITY_6000_0_OR_NEWER
            [DataMember]
#endif
            internal bool generateBuildLayout = false;

            [SerializeField]
#if UNITY_6000_0_OR_NEWER
            [DataMember]
#endif
            internal ReportFileFormat buildLayoutReportFileFormat = ReportFileFormat.JSON;

            [SerializeField]
#if UNITY_6000_0_OR_NEWER
            [DataMember]
#endif
            internal List<string> buildReports = new List<string>();

            [SerializeField]
#if UNITY_6000_0_OR_NEWER
            [DataMember]
#endif
            internal bool autoOpenAddressablesReport = true;
            [SerializeField]
#if UNITY_6000_0_OR_NEWER
            [DataMember]
#endif
            internal bool userHasBeenInformedAboutBuildReportSettingPreBuild = false;
            [SerializeField]
#if UNITY_6000_0_OR_NEWER
            [DataMember]
#endif
            internal bool userHasBeenInformedAboutPathPairMigration = false;
            [SerializeField]
#if UNITY_6000_0_OR_NEWER
            [DataMember]
#endif
            internal bool userHasBeenInformedAboutNestedFolderStructure = false;
            [SerializeField]
#if UNITY_6000_0_OR_NEWER
            [DataMember]
#endif
            internal bool userHasSeenContentDirectoryAnnouncement = false;
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

        internal static bool UserHasBeenInformedAbouNestedFolderStructure
        {
            get
            {
                ValidateData();
                return s_Data.userHasBeenInformedAboutNestedFolderStructure;
            }
            set
            {
                ValidateData();
                if (s_Data.userHasBeenInformedAboutNestedFolderStructure != value)
                {
                    s_Data.userHasBeenInformedAboutNestedFolderStructure = value;
                    SaveData();
                }
            }
        }

        internal static bool UserHasSeenContentDirectoryAnnouncement
        {
            get
            {
                ValidateData();
                return s_Data.userHasSeenContentDirectoryAnnouncement;
            }
            set
            {
                ValidateData();
                if (s_Data.userHasSeenContentDirectoryAnnouncement != value)
                {
                    s_Data.userHasSeenContentDirectoryAnnouncement = value;
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
        /// <param name="reportFilePath">The file path of the report</param>
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

#if UNITY_6000_0_OR_NEWER
        private static DataContractSerializer s_Serializer;

        private static DataContractSerializer Serializer
        {
            get
            {
                if (s_Serializer == null)
                {
                    s_Serializer = new DataContractSerializer(typeof(ConfigSaveData));
                }
                return s_Serializer;
            }
        }
#endif

        static void ValidateData()
        {
            if (s_Data == null)
            {
                var dataPath = Path.GetFullPath(".");
                dataPath = dataPath.Replace("\\", "/");
                dataPath += "/Library/AddressablesConfig.dat";

                if (File.Exists(dataPath))
                {
#if UNITY_6000_0_OR_NEWER
                    // Check for legacy format and convert if necessary
                    if (Build.LegacyFormatConverter.IsLegacyFormat(dataPath, Build.LegacyFormatConverter.ConfigDataVersionMarker))
                    {
                        var convertedPath = Build.LegacyFormatConverter.ConvertLegacyFile(dataPath, logAsWarning: true);
                        if (convertedPath == null)
                        {
                            // Conversion failed, delete the old file and start fresh
                            File.Delete(dataPath);
                        }
                    }

                    // Try to load with new format
                    if (File.Exists(dataPath))
                    {
                        try
                        {
                            using (var file = new FileStream(dataPath, FileMode.Open, FileAccess.Read))
                            {
                                // Skip version marker
                                file.Position = Build.LegacyFormatConverter.ConfigDataVersionMarker.Length;

                                using (var reader = XmlDictionaryReader.CreateBinaryReader(file, XmlDictionaryReaderQuotas.Max))
                                {
                                    var data = Serializer.ReadObject(reader) as ConfigSaveData;
                                    if (data != null)
                                    {
                                        s_Data = data;
                                    }
                                }
                            }
                        }
                        catch
                        {
                            Addressables.LogWarning("Error reading Addressable Asset project config (play mode, etc.). Resetting to default.");
                            File.Delete(dataPath);
                        }
                    }
#else
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
#endif
                }
                if (s_Data == null)
                {
                    s_Data = new ConfigSaveData();
                }

                if (s_Data.buildReports == null)
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

#if UNITY_6000_0_OR_NEWER
            using (var file = File.Create(dataPath))
            {
                // Write version marker
                var marker = Build.LegacyFormatConverter.ConfigDataVersionMarker;
                file.Write(marker, 0, marker.Length);

                // Write using Binary XML format
                using (var writer = XmlDictionaryWriter.CreateBinaryWriter(file, null, null, false))
                {
                    Serializer.WriteObject(writer, s_Data);
                    writer.Flush();
                }
            }
#else
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(dataPath);
            bf.Serialize(file, s_Data);
            file.Close();
#endif
        }
    }
}
