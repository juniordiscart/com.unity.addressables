using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityEditor.AddressableAssets.Build.Layout
{
    /// <summary>
    /// A storage class used to gather data about an Addressable build.
    /// </summary>
    [Serializable]
    public class BuildLayout
    {
        /// <summary>
        /// Helper class to wrap header values for BuildLayout
        /// </summary>
        public class LayoutHeader
        {
            /// <summary>
            /// Build layout for this header
            /// </summary>
            internal BuildLayout m_BuildLayout;

            /// <summary>
            /// Build Platform Addressables build is targeting
            /// </summary>
            public BuildTarget BuildTarget
            {
                get
                {
                    if (m_BuildLayout == null)
                        return BuildTarget.NoTarget;
                    return m_BuildLayout.BuildTarget;
                }
            }

            /// <summary>
            /// Hash of the build results
            /// </summary>
            public string BuildResultHash
            {
                get
                {
                    if (m_BuildLayout == null)
                        return null;
                    return m_BuildLayout.BuildResultHash;
                }
            }

            /// <summary>
            /// If the build was a new build or an update for a previous build
            /// </summary>
            public BuildType BuildType
            {
                get
                {
                    if (m_BuildLayout == null)
                        return BuildType.NewBuild;
                    return m_BuildLayout.BuildType;
                }
            }

            /// <summary>
            /// DateTime at the start of building Addressables
            /// </summary>
            public DateTime BuildStart
            {
                get
                {
                    if (m_BuildLayout == null)
                        return DateTime.MinValue;
                    return m_BuildLayout.BuildStart;
                }
            }

            /// <summary>
            /// Time in seconds taken to build Addressables Content
            /// </summary>
            public double Duration
            {
                get
                {
                    if (m_BuildLayout == null)
                        return 0;
                    return m_BuildLayout.Duration;
                }
            }

            /// <summary>
            /// Null or Empty if the build completed successfully, else contains error causing the failure
            /// </summary>
            public string BuildError
            {
                get
                {
                    if (m_BuildLayout == null)
                        return "";
                    return m_BuildLayout.BuildError;
                }
            }
        }

        /// <summary>
        /// Helper object to get header values for this build layout
        /// </summary>
        public LayoutHeader Header
        {
            get
            {
                if (m_Header == null)
                    m_Header = new LayoutHeader() {m_BuildLayout = this};
                return m_Header;
            }
        }

        private LayoutHeader m_Header;

        #region HeaderValues // Any values in here should also be in BuildLayoutHeader class

        /// <summary>
        /// Build Platform Addressables build is targeting
        /// </summary>
        public BuildTarget BuildTarget;

        /// <summary>
        /// Hash of the build results
        /// </summary>
        public string BuildResultHash;

        /// <summary>
        /// If the build was a new build or an update for a previous build
        /// </summary>
        public BuildType BuildType;

        /// <summary>
        /// DateTime at the start of building Addressables
        /// </summary>
        public DateTime BuildStart
        {
            get
            {
                if (m_BuildStartDateTime.Year > 2000)
                    return m_BuildStartDateTime;
                if (DateTime.TryParse(BuildStartTime, out DateTime result))
                {
                    m_BuildStartDateTime = result;
                    return m_BuildStartDateTime;
                }
                return DateTime.MinValue;
            }
            set
            {
                BuildStartTime = value.ToString();
            }
        }
        private DateTime m_BuildStartDateTime;

        [SerializeField]
        internal string BuildStartTime;

        /// <summary>
        /// Time in seconds taken to build Addressables Content
        /// </summary>
        public double Duration;

        /// <summary>
        /// Null or Empty if the build completed successfully, else contains error causing the failure
        /// </summary>
        public string BuildError;

        #endregion // End of header values

        /// <summary>
        /// Version of the Unity edtior used to perform the build.
        /// </summary>
        public string UnityVersion;

        /// <summary>
        /// Version of the Addressables package used to perform the build.
        /// </summary>
        public string PackageVersion;

        /// <summary>
        /// Player build version for the build, this is a timestamp if PlayerVersionOverride is not set in the settings
        /// </summary>
        public string PlayerBuildVersion;

        /// <summary>
        /// Settings used by the Addressables settings at the time of building
        /// </summary>
        public AddressablesEditorData AddressablesEditorSettings;

        /// <summary>
        /// Values used by the Addressables runtime
        /// </summary>
        public AddressablesRuntimeData AddressablesRuntimeSettings;

        /// <summary>
        /// Name of the build script to build
        /// </summary>
        public string BuildScript;

        /// <summary>
        /// Default group at the time of building
        /// </summary>
        [SerializeReference]
        public Group DefaultGroup;

        /// <summary>
        /// The Addressable Groups that reference this data
        /// </summary>
        [SerializeReference]
        public List<Group> Groups = new List<Group>();

        /// <summary>
        /// The List of AssetBundles that were built without a group associated to them, such as the BuiltIn Shaders Bundle and the MonoScript Bundle
        /// </summary>
        [SerializeReference]
        public List<Bundle> BuiltInBundles = new List<Bundle>();

        /// <summary>
        /// List of assets with implicitly included Objects
        /// </summary>
        public List<AssetDuplicationData> DuplicatedAssets = new List<AssetDuplicationData>();

        /// <summary>
        /// The build path on disk of the default local content catalog
        /// </summary>
        [SerializeField]
        internal string LocalCatalogBuildPath;

        /// <summary>
        /// The build path of the remote content catalog, if one was built
        /// </summary>
        [SerializeField]
        internal string RemoteCatalogBuildPath;

        internal string m_FilePath;

        private bool m_HeaderRead = false;
        private bool m_BodyRead = false;

        private FileStream m_FileStream = null;
        private StreamReader m_StreamReader = null;

        /// <summary>
        /// Used for serialising the header info for the BuildLayout.
        /// Names must match values in BuildLayout class
        /// </summary>
        [Serializable]
        private class BuildLayoutHeader
        {
            public BuildTarget BuildTarget;
            public string BuildResultHash;
            public BuildType BuildType;
            public string BuildStartTime;
            public double Duration;
            public string BuildError;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path">Path to the BuildLayout json file on disk</param>
        /// <param name="readHeader">If the basic header information should be read</param>
        /// <param name="readFullFile">If the full build layout should be read</param>
        /// <returns></returns>
        public static BuildLayout Open(string path, bool readHeader = true, bool readFullFile = false)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                Debug.LogError($"Invalid path provided : {path}");
                return null;
            }

            BuildLayout readLayout = new BuildLayout
            {
                m_FilePath = path
            };

            if (readFullFile)
                readLayout.ReadFull();
            else if (readHeader)
                readLayout.ReadHeader();

            return readLayout;
        }

        /// <summary>
        /// Writes json file for the build layout to the destination path
        /// </summary>
        /// <param name="destinationPath">File path to write build layout</param>
        /// <param name="prettyPrint">If json should be written using pretty print</param>
        public void WriteToFile(string destinationPath, bool prettyPrint)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

            string versionElementString = "\"UnityVersion\":";
            string headerJson = null;
            string bodyJson = JsonUtility.ToJson(this, prettyPrint);

            if (prettyPrint)
            {
                BuildLayoutHeader header = new BuildLayoutHeader()
                {
                    BuildTarget = this.BuildTarget,
                    BuildResultHash = this.BuildResultHash,
                    BuildType = this.BuildType,
                    BuildStartTime = this.BuildStartTime,
                    Duration = this.Duration,
                    BuildError = this.BuildError
                };
                headerJson = JsonUtility.ToJson(header, false);
                headerJson = headerJson.Remove(headerJson.Length - 1, 1) + ',';
            }

            int index = bodyJson.IndexOf(versionElementString);
            if (prettyPrint)
                bodyJson = bodyJson.Remove(0, index);
            else
                bodyJson = bodyJson.Insert(index, "\n");

            using (FileStream s = System.IO.File.Open(destinationPath, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(s))
                {
                    if (prettyPrint)
                        sw.WriteLine(headerJson);
                    sw.Write(bodyJson);
                }
            }
        }

        /// <summary>
        /// Closes streams for loading the build layout
        /// </summary>
        public void Close()
        {
            if (m_StreamReader != null)
            {
                m_StreamReader.Close();
                m_StreamReader = null;
            }

            if (m_FileStream != null)
            {
                m_FileStream.Close();
                m_FileStream = null;
            }
        }

        /// <summary>
        /// Reads basic information about the build layout
        /// </summary>
        /// <param name="keepFileStreamsActive">If false, the file will be closed after reading the header line.</param>
        /// <returns>true is successful, else false</returns>
        public bool ReadHeader(bool keepFileStreamsActive = false)
        {
            if (m_HeaderRead)
                return true;

            if (string.IsNullOrEmpty(m_FilePath))
            {
                Debug.LogError("Cannot read BuildLayout header, A file has not been selected to open. Open must be called before reading any data");
                return false;
            }

            try
            {
                if (m_FileStream == null)
                {
                    m_FileStream = System.IO.File.Open(m_FilePath, FileMode.Open);
                    m_StreamReader = new StreamReader(m_FileStream);
                }

                string fileJsonText = m_StreamReader.ReadLine();
                int lastComma = fileJsonText.LastIndexOf(',');
                if (lastComma > 0)
                {
                    fileJsonText = fileJsonText.Remove(lastComma) + '}';
                    try
                    {
                        EditorJsonUtility.FromJsonOverwrite(fileJsonText, this);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to read header for BuildLayout at {m_FilePath}, with exception: {e.Message}");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError($"Failed to read header for BuildLayout at {m_FilePath}, invalid json format");
                    return false;
                }

                m_HeaderRead = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            finally
            {
                if (!keepFileStreamsActive)
                    Close();
            }

            return true;
        }

        /// <summary>
        /// Reads the full build layout data from file
        /// </summary>
        /// <returns>true is successful, else false</returns>
        public bool ReadFull()
        {
            if (m_BodyRead)
                return true;

            if (string.IsNullOrEmpty(m_FilePath))
            {
                Debug.LogError("Cannot read BuildLayout header, BuildLayout has not open for a file");
                return false;
            }

            try
            {
                if (m_FileStream == null)
                {
                    m_FileStream = System.IO.File.Open(m_FilePath, FileMode.Open);
                    m_StreamReader = new StreamReader(m_FileStream);
                }
                else if (m_HeaderRead)
                {
                    // reset to read the whole file
                    m_FileStream.Position = 0;
                    m_StreamReader.DiscardBufferedData();
                }

                string fileJsonText = m_StreamReader.ReadToEnd();
                EditorJsonUtility.FromJsonOverwrite(fileJsonText, this);
                m_HeaderRead = true;
                m_BodyRead = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read header for BuildLayout at {m_FilePath}, with exception: {e.Message}");
                return false;
            }
            finally
            {
                Close();
            }

            return true;
        }

        /// <summary>
        /// Values set for the AddressablesAssetSettings at the time of building
        /// </summary>
        [Serializable]
        public class AddressablesEditorData
        {
            /// <summary>
            /// Hash value of the settings at the time of building
            /// </summary>
            public string SettingsHash;

            /// <summary>
            /// Active Addressables profile set at time of Building
            /// </summary>
            public Profile ActiveProfile;

            /// <summary>
            /// Addressables setting value set for building the remote catalog
            /// </summary>
            public bool BuildRemoteCatalog;

            /// <summary>
            /// Load path for the remote catalog if enabled
            /// </summary>
            public string RemoteCatalogLoadPath;

            /// <summary>
            /// Addressables setting value set for bundling the local catalog
            /// </summary>
            public bool BundleLocalCatalog;

            /// <summary>
            /// Addressables setting value set for optimising the catalog size
            /// </summary>
            public bool OptimizeCatalogSize;

            /// <summary>
            /// Addressables setting value set for time out when downloading catalogs
            /// </summary>
            public int CatalogRequestsTimeout;

            /// <summary>
            /// Runtime setting value set for the maximum number of concurrent web requests
            /// </summary>
            public int MaxConcurrentWebRequests;

            /// <summary>
            /// Addressables setting value set for is to update the remote catalog on startup
            /// </summary>
            public bool DisableCatalogUpdateOnStartup;

            /// <summary>
            /// Addressables setting value set for if the build created unique bundle ids
            /// </summary>
            public bool UniqueBundleIds;

            /// <summary>
            /// Addressables setting value set for if the build used non recursive dependency calculation
            /// </summary>
            public bool NonRecursiveBuilding;

            /// <summary>
            /// Addressables setting value set for if the build used contiguous bundle objects
            /// </summary>
            public bool ContiguousBundles;

            /// <summary>
            /// Addressables setting value set for disabling sub asset representation in the Bundle
            /// </summary>
            public bool DisableSubAssetRepresentations;

            /// <summary>
            /// Internal naming prefix of the built in shaders bundle
            /// </summary>
            public string ShaderBundleNaming;

            /// <summary>
            /// Internal naming prefix of the monoScript bundle,
            /// No MonoScript bundle is built if set to disabled
            /// </summary>
            public string MonoScriptBundleNaming;

            /// <summary>
            /// Addressables setting value set for is the unity version was stripped from the built bundles
            /// </summary>
            public bool StripUnityVersionFromBundleBuild;
        }

        /// <summary>
        /// Values set for runtime initialisation of Addressables
        /// </summary>
        [Serializable]
        public class AddressablesRuntimeData
        {
            /// <summary>
            /// Runtime setting value set for if the runtime will submit profiler events
            /// </summary>
            public bool ProfilerEvents;

            /// <summary>
            /// Runtime setting value set for if resource manager exceptions are logged or not
            /// </summary>
            public bool LogResourceManagerExceptions;

            /// <summary>
            /// Runtime setting value set for catalogs to load (First catalog found in the list is used)
            /// </summary>
            public List<string> CatalogLoadPaths = new List<string>();

            /// <summary>
            /// Hash of the build catalog
            /// </summary>
            public string CatalogHash;
        }

        /// <summary>
        /// Information about the AssetBundleObject
        /// </summary>
        [Serializable]
        public class AssetBundleObjectInfo
        {
            /// <summary>
            /// The size, in bytes, of the AssetBundleObject
            /// </summary>
            public ulong Size;
        }

        /// <summary>
        /// Key value pair of string type
        /// </summary>
        [Serializable]
        public struct StringPair
        {
            /// <summary>
            /// String key
            /// </summary>
            public string Key;

            /// <summary>
            /// String value
            /// </summary>
            public string Value;
        }

        /// <summary>
        /// Addressables Profile data
        /// </summary>
        [Serializable]
        public class Profile
        {
            /// <summary>
            /// Name of the profile
            /// </summary>
            public string Name;

            /// <summary>
            /// ID assigned within the ProfileSettings of the profile
            /// </summary>
            public string Id;

            /// <summary>
            /// Profile variables assigned to the profile
            /// </summary>
            public StringPair[] Values;
        }

        /// <summary>
        /// Data about the AddressableAssetGroup that gets processed during a build.
        /// </summary>
        [Serializable]
        public class Group
        {
            /// <summary>
            /// The Name of the AdressableAssetGroup
            /// </summary>
            public string Name;

            /// <summary>
            /// The Guid of the AddressableAssetGroup
            /// </summary>
            public string Guid;

            /// <summary>
            /// The packing mode as defined by the BundledAssetGroupSchema on the AddressableAssetGroup
            /// </summary>
            public string PackingMode;

            /// <summary>
            /// A list of the AssetBundles associated with the Group
            /// </summary>
            [SerializeReference]
            public List<Bundle> Bundles = new List<Bundle>();

            /// <summary>
            /// Data about the AddressableAssetGroupSchemas associated with the Group
            /// </summary>
            [SerializeReference]
            public List<SchemaData> Schemas = new List<SchemaData>();
        }

        /// <summary>
        /// Data container for AddressableAssetGroupSchemas
        /// </summary>
        [Serializable]
        public class SchemaData : ISerializationCallbackReceiver
        {
            /// <summary>
            /// The Guid of the AddressableAssetGroupSchema
            /// </summary>
            public string Guid;

            /// <summary>
            /// The class type of the AddressableAssetGroupSchema
            /// </summary>
            public string Type;

            /// <summary>
            /// These key-value-pairs include data about the AddressableAssetGroupSchema, such as PackingMode and Compression.
            /// </summary>
            public List<Tuple<string, string>> KvpDetails = new List<Tuple<string, string>>();

            [SerializeField]
            private StringPair[] SchemaDataPairs;

            /// <summary>
            /// Converts the unserializable KvpDetails to a serializable type for writing
            /// </summary>
            public void OnBeforeSerialize()
            {
                SchemaDataPairs = new StringPair[KvpDetails.Count];
                for (int i = 0; i < SchemaDataPairs.Length; ++i)
                    SchemaDataPairs[i] = new StringPair() {Key = KvpDetails[i].Item1, Value = KvpDetails[i].Item2};
            }

            /// <summary>
            /// Writes data to KvpDetails after Deserializing to temporary data fields
            /// </summary>
            public void OnAfterDeserialize()
            {
                for (int i = 0; i < SchemaDataPairs.Length; ++i)
                    KvpDetails.Add(new Tuple<string, string>(SchemaDataPairs[i].Key, SchemaDataPairs[i].Value));
                SchemaDataPairs = null;
            }
        }

        /// <summary>
        /// Data store for AssetBundle information.
        /// </summary>
        [Serializable]
        public class Bundle
        {
            /// <summary>
            /// The name of the AssetBundle
            /// </summary>
            public string Name;

            /// <summary>
            /// Name used to identify the asset bundle
            /// </summary>
            public string InternalName;

            /// <summary>
            /// The file size of the AssetBundle on disk, in bytes
            /// </summary>
            public ulong FileSize;

            /// <summary>
            /// Status of the bundle after an update build
            /// </summary>
            public BundleBuildStatus BuildStatus;

            /// <summary>
            /// The file size of all of the Expanded Dependencies of this AssetBundle, in bytes
            /// Expanded dependencies are the dependencies of this AssetBundle's dependencies
            /// </summary>
            public ulong ExpandedDependencyFileSize;

            /// <summary>
            /// The file size
            /// </summary>
            public ulong DependencyFileSize;

            /// <summary>
            /// The file size of the AssetBundle on disk when uncompressed, in bytes
            /// </summary>
            public ulong UncompressedFileSize
            {
                get
                {
                    ulong total = 0;
                    foreach (File file in Files)
                        total += file.UncompressedSize;
                    return total;
                }
            }

            /// <summary>
            /// The number of Assets contained within the bundle
            /// </summary>
            public int AssetCount = 0;

            /// <summary>
            /// Represents a dependency from the containing Bundle to dependentBundle, with AssetDependencies representing each of the assets in parentBundle that create the link to dependentBundle
            /// </summary>
            [Serializable]
            public class BundleDependency
            {
                /// <summary>
                /// The bundle that the parent bundle depends on
                /// </summary>
                [SerializeReference]
                public Bundle DependencyBundle;

                /// <summary>
                /// The list of assets that link the parent bundle to the DependencyBundle
                /// </summary>
                public List<AssetDependency> AssetDependencies;

                /// <summary>
                /// Percentage of Efficiency asset usage that uses the entire dependency tree of this bundle dependency.
                /// This includes DependencyBundle and all bundles beneath it.
                /// Value is equal to [Total Filesize of Dependency Assets] / [Total size of all dependency bundles on disk]
                /// Example: There are 3 bundles A, B, and C, that are each 10 MB on disk. A depends on 2 MB worth of assets in B, and B depends on 4 MB worth of assets in C.
                /// The Efficiency of the dependencyLink from A->B would be 2/10 -> 20% and the ExpandedEfficiency of A->B would be (2 + 4)/(10 + 10) -> 6/20 -> 30%
                ///  </summary>
                public float ExpandedEfficiency;

                /// <summary>
                /// The Efficiency of the connection between the parent bundle and DependencyBundle irrespective of the full dependency tree below DependencyBundle.
                /// Value is equal to [Serialized Filesize of assets In Dependency Bundle Referenced By Parent]/[Total size of Dependency Bundle on disk]
                /// Example: Given two Bundles A and B that are each 10 MB on disk, and A depends on 5 MB worth of assets in B, then the Efficiency of DependencyLink A->B is 5/10 = .5
                /// </summary>
                public float Efficiency;

                private HashSet<ExplicitAsset> referencedAssets = new HashSet<ExplicitAsset>();

                /// <summary>
                /// The number of uniquely assets that the parent bundle uniquely references in dependency bundle. This is used to calculate Efficiency without double counting.
                /// </summary>
                internal ulong referencedAssetsFileSize = 0;

                internal BundleDependency(Bundle b)
                {
                    DependencyBundle = b;
                    AssetDependencies = new List<AssetDependency>();
                }

                internal void CreateAssetDependency(ExplicitAsset root, ExplicitAsset dependencyAsset)
                {
                    if (referencedAssets.Contains(dependencyAsset))
                        return;
                    referencedAssets.Add(dependencyAsset);
                    AssetDependencies.Add(new AssetDependency(root, dependencyAsset));
                    referencedAssetsFileSize += dependencyAsset.SerializedSize;
                }


                /// <summary>
                /// Represents a dependency from a root Asset to a dependent Asset.
                /// </summary>
                [Serializable]
                public struct AssetDependency
                {
                    [SerializeReference]
                    internal ExplicitAsset rootAsset;

                    [SerializeReference]
                    internal ExplicitAsset dependencyAsset;

                    internal AssetDependency(ExplicitAsset root, ExplicitAsset depAsset)
                    {
                        rootAsset = root;
                        dependencyAsset = depAsset;
                    }
                }
            }

            internal Dictionary<Bundle, BundleDependency> BundleDependencyMap = new Dictionary<Bundle, BundleDependency>();

            /// <summary>
            /// A list of bundles that this bundle depends upon.
            /// </summary>
            [SerializeField]
            public BundleDependency[] BundleDependencies = Array.Empty<BundleDependency>();


            /// <summary>
            /// Convert BundleDependencyMap to a format that is able to be serialized and plays nicer with
            /// CalculateEfficiency - this must be called on a bundle before CalculateEfficiency can be called.
            /// </summary>
            internal void SerializeBundleToBundleDependency()
            {
                BundleDependencies = new BundleDependency[BundleDependencyMap.Values.Count];
                BundleDependencyMap.Values.CopyTo(BundleDependencies, 0);
            }

            /// <summary>
            /// Updates the BundleDependency from the current bundle to the bundle that contains referencedAsset. If no such BundleDependency exists,
            /// one is created. Does nothing if rootAsset's bundle is not the current bundle or
            /// if the two assets are in the same bundle.
            /// </summary>
            /// <param name="rootAsset"></param>
            /// <param name="referencedAsset"></param>
            internal void UpdateBundleDependency(ExplicitAsset rootAsset, ExplicitAsset referencedAsset)
            {
                if (rootAsset.Bundle != this || referencedAsset.Bundle == rootAsset.Bundle)
                    return;

                if (!BundleDependencyMap.ContainsKey(referencedAsset.Bundle))
                    BundleDependencyMap.Add(referencedAsset.Bundle, new BundleDependency(referencedAsset.Bundle));
                BundleDependencyMap[referencedAsset.Bundle].CreateAssetDependency(rootAsset, referencedAsset);
            }

            // Helper struct for calculating Efficiency
            internal struct EfficiencyInfo
            {
                internal ulong totalAssetFileSize;
                internal ulong referencedAssetFileSize;
            }


            /// <summary>
            /// The Compression method used for the AssetBundle.
            /// </summary>
            public string Compression;

            /// <summary>
            /// Cyclic redundancy check of the content contained inside of the asset bundle.
            /// This value will not change between identical asset bundles with different compression options.
            /// </summary>
            public uint CRC;

            /// <summary>
            /// The hash version of the contents contained inside of the asset bundle.
            /// This value will not change between identical asset bundles with different compression options.
            /// </summary>
            public Hash128 Hash;

            /// <summary>
            /// A reference to the Group data that this AssetBundle was generated from
            /// </summary>
            [SerializeReference]
            public Group Group;

            /// <summary>
            /// Path Provider uses to load the Asset Bundle
            /// </summary>
            public string LoadPath;

            /// <summary>
            /// Provider used to load the Asset Bundle
            /// </summary>
            public string Provider;

            /// <summary>
            /// Result provided by the Provider loading the Asset Bundle
            /// </summary>
            public string ResultType;

            /// <summary>
            /// List of the Files referenced by the AssetBundle
            /// </summary>
            [SerializeReference]
            public List<File> Files = new List<File>();

            /// <summary>
            /// A list of the bundles that directly depend on this AssetBundle
            /// </summary>
            [SerializeReference]
            public List<Bundle> DependentBundles = new List<Bundle>();

            /// <summary>
            /// A list of the direct dependencies of the AssetBundle
            /// </summary>
            [SerializeReference]
            public List<Bundle> Dependencies;

            /// <summary>
            /// The second order dependencies and greater of a bundle
            /// </summary>
            [SerializeReference]
            public List<Bundle> ExpandedDependencies;
        }

        /// <summary>
        /// Data store for resource files generated by the build pipeline and referenced by a main File
        /// </summary>
        [Serializable]
        public class SubFile
        {
            /// <summary>
            /// The name of the sub-file
            /// </summary>
            public string Name;

            /// <summary>
            /// If the main File is a serialized file, this will be true.
            /// </summary>
            public bool IsSerializedFile;

            /// <summary>
            /// The size of the sub-file, in bytes
            /// </summary>
            public ulong Size;
        }

        /// <summary>
        /// Data store for the main File created for the AssetBundle
        /// </summary>
        [Serializable]
        public class File
        {
            /// <summary>
            /// The name of the File.
            /// </summary>
            public string Name;

            /// <summary>
            /// The AssetBundle data that relates to a built file.
            /// </summary>
            [SerializeReference]
            public Bundle Bundle;

            /// <summary>
            /// The file size of the AssetBundle on disk when uncompressed, in bytes
            /// </summary>
            public ulong UncompressedSize
            {
                get
                {
                    ulong total = 0;
                    foreach (SubFile subFile in SubFiles)
                        total += subFile.Size;
                    return total;
                }
            }

            /// <summary>
            /// List of the resource files created by the build pipeline that a File references
            /// </summary>
            [SerializeReference]
            public List<SubFile> SubFiles = new List<SubFile>();

            /// <summary>
            /// A list of the explicit asset defined in the AssetBundle
            /// </summary>
            [SerializeReference]
            public List<ExplicitAsset> Assets = new List<ExplicitAsset>();

            /// <summary>
            /// A list of implicit assets built into the AssetBundle, typically through references by Assets that are explicitly defined.
            /// </summary>
            [SerializeReference]
            public List<DataFromOtherAsset> OtherAssets = new List<DataFromOtherAsset>();

            [SerializeReference]
            internal List<ExplicitAsset> ExternalReferences = new List<ExplicitAsset>();

            /// <summary>
            /// The final filename of the AssetBundle file
            /// </summary>
            public string WriteResultFilename;

            /// <summary>
            /// Data about the AssetBundleObject
            /// </summary>
            public AssetBundleObjectInfo BundleObjectInfo;

            /// <summary>
            /// The size of the data that needs to be preloaded for this File.
            /// </summary>
            public int PreloadInfoSize;

            /// <summary>
            /// The number of Mono scripts referenced by the File
            /// </summary>
            public int MonoScriptCount;

            /// <summary>
            /// The size of the Mono scripts referenced by the File
            /// </summary>
            public ulong MonoScriptSize;
        }

        /// <summary>
        /// A representation of an object in an asset file.
        /// </summary>
        [Serializable]
        public class ObjectData
        {
            /// <summary>
            /// FileId of Object in Asset File
            /// </summary>
            public long LocalIdentifierInFile;

            /// <summary>
            /// Object name within the Asset
            /// </summary>
            [SerializeField] internal string ObjectName;

            /// <summary>
            /// Component name if AssetType is a MonoBehaviour or Component
            /// </summary>
            [SerializeField] internal string ComponentName;

            /// <summary>
            /// Type of Object
            /// </summary>
            public AssetType AssetType;

            /// <summary>
            /// The size of the file on disk.
            /// </summary>
            public ulong SerializedSize;

            /// <summary>
            /// The size of the streamed Asset.
            /// </summary>
            public ulong StreamedSize;

            /// <summary>
            /// References to other Objects
            /// </summary>
            [SerializeField] internal List<ObjectReference> References = new List<ObjectReference>();
        }

        /// <summary>
        /// Identification of an Object within the same file
        /// </summary>
        [Serializable]
        internal class ObjectReference
        {
            public int AssetId;
            public List<int> ObjectIds;
        }

        /// <summary>
        /// Data store for Assets explicitly defined in an AssetBundle
        /// </summary>
        [Serializable]
        public class ExplicitAsset
        {
            /// <summary>
            /// The Asset Guid.
            /// </summary>
            public string Guid;

            /// <summary>
            /// The Asset path on disk
            /// </summary>
            public string AssetPath;

            /// <summary>
            /// Name used to identify the asset within the asset bundle containing it
            /// </summary>
            public string InternalId;

            /// <summary>
            /// Hash of the asset content
            /// </summary>
            public Hash128 AssetHash;

            /// <summary>
            /// Objects that consist of the overall asset
            /// </summary>
            public List<ObjectData> Objects = new List<ObjectData>();

            /// <summary>
            /// AssetType of the main Object for the Asset
            /// </summary>
            public AssetType MainAssetType;

            /// <summary>
            /// True if is a scene asset, else false
            /// </summary>
            public bool IsScene => AssetPath.EndsWith(".unity", StringComparison.Ordinal);

            /// <summary>
            /// Guid of the Addressable group this Asset entry was built using.
            /// </summary>
            public string GroupGuid;

            /// <summary>
            /// The Addressable address defined in the Addressable Group window for an Asset.
            /// </summary>
            public string AddressableName;

            /// <summary>
            /// Addressable labels for this asset entry.
            /// </summary>
            [SerializeField]
            public string[] Labels = Array.Empty<string>();

            /// <summary>
            /// The size of the file on disk.
            /// </summary>
            public ulong SerializedSize;

            /// <summary>
            /// The size of the streamed Asset.
            /// </summary>
            public ulong StreamedSize;

            /// <summary>
            /// The file that the Asset was added to
            /// </summary>
            [SerializeReference]
            public File File;

            /// <summary>
            /// The AssetBundle that contains the asset
            /// </summary>
            [SerializeReference]
            public Bundle Bundle;

            /// <summary>
            /// List of data from other Assets referenced by an Asset in the File
            /// </summary>
            [SerializeReference]
            public List<DataFromOtherAsset> InternalReferencedOtherAssets = new List<DataFromOtherAsset>();

            /// <summary>
            /// List of explicit Assets referenced by this asset that are in the same AssetBundle
            /// </summary>
            [SerializeReference]
            public List<ExplicitAsset> InternalReferencedExplicitAssets = new List<ExplicitAsset>();

            /// <summary>
            /// List of explicit Assets referenced by this asset that are in a different AssetBundle
            /// </summary>
            [SerializeReference]
            public List<ExplicitAsset> ExternallyReferencedAssets = new List<ExplicitAsset>();

            /// <summary>
            /// List of Assets that reference this Asset
            /// </summary>
            [SerializeReference]
            internal List<ExplicitAsset> ReferencingAssets = new List<ExplicitAsset>();
        }

        /// <summary>
        /// Data store for implicit Asset references
        /// </summary>
        [Serializable]
        public class DataFromOtherAsset
        {
            /// <summary>
            /// The Guid of the Asset
            /// </summary>
            public string AssetGuid;

            /// <summary>
            /// The Asset path on disk
            /// </summary>
            public string AssetPath;

            /// <summary>
            /// The file that the Asset was added to
            /// </summary>
            [SerializeReference]
            public File File;

            /// <summary>
            /// Objects that consist of the overall asset
            /// </summary>
            public List<ObjectData> Objects = new List<ObjectData>();

            /// <summary>
            /// AssetType of the main Object for the Asset
            /// </summary>
            public AssetType MainAssetType;

            /// <summary>
            /// True if is a scene asset, else false
            /// </summary>
            public bool IsScene => AssetPath.EndsWith(".unity", StringComparison.Ordinal);

            /// <summary>
            /// A list of Assets that reference this data
            /// </summary>
            [SerializeReference]
            public List<ExplicitAsset> ReferencingAssets = new List<ExplicitAsset>();

            /// <summary>
            /// The number of Objects in the data
            /// </summary>
            public int ObjectCount;

            /// <summary>
            /// The size of the data on disk
            /// </summary>
            public ulong SerializedSize;

            /// <summary>
            /// The size of the streamed data
            /// </summary>
            public ulong StreamedSize;
        }

        /// <summary>
        /// Data store for duplicated Implicit Asset information
        /// </summary>
        [Serializable]
        public class AssetDuplicationData
        {
            /// <summary>
            /// The Guid of the Asset with duplicates
            /// </summary>
            public string AssetGuid;
            /// <summary>
            /// A list of duplicated objects and the bundles that contain them.
            /// </summary>
            public List<ObjectDuplicationData> DuplicatedObjects = new List<ObjectDuplicationData>();
        }

        /// <summary>
        /// Data store for duplicated Object information
        /// </summary>
        [Serializable]
        public class ObjectDuplicationData
        {
            /// <summary>
            /// The local identifier for an object.
            /// </summary>
            public long LocalIdentifierInFile;
            /// <summary>
            /// A list of bundles that include the referenced file.
            /// </summary>
            [SerializeReference] public List<File> IncludedInBundleFiles = new List<File>();
        }
    }

    /// <summary>
    /// Utility used to quickly reference data built with the build pipeline
    /// </summary>
    public class LayoutLookupTables
    {
        /// <summary>
        /// The default AssetBundle name to the Bundle data map.
        /// </summary>
        public Dictionary<string, BuildLayout.Bundle> Bundles = new Dictionary<string, BuildLayout.Bundle>();

        /// <summary>
        /// File name to File data map.
        /// </summary>
        public Dictionary<string, BuildLayout.File> Files = new Dictionary<string, BuildLayout.File>();

        internal Dictionary<BuildLayout.File, FileObjectData> FileToFileObjectData = new Dictionary<BuildLayout.File, FileObjectData>();

        /// <summary>
        /// Guid to ExplicitAsset data map.
        /// </summary>
        public Dictionary<string, BuildLayout.ExplicitAsset> GuidToExplicitAsset = new Dictionary<string, BuildLayout.ExplicitAsset>();

        /// <summary>
        /// Group name to Group data map.
        /// </summary>
        public Dictionary<string, BuildLayout.Group> GroupLookup = new Dictionary<string, BuildLayout.Group>();

        /// <summary>
        /// The remapped AssetBundle name to the Bundle data map
        /// </summary>
        internal Dictionary<string, BuildLayout.Bundle> FilenameToBundle = new Dictionary<string, BuildLayout.Bundle>();


        /// Maps used for lookups while building the BuildLayout
        internal Dictionary<string, List<BuildLayout.DataFromOtherAsset>> UsedImplicits = new Dictionary<string, List<BuildLayout.DataFromOtherAsset>>();

        internal Dictionary<string, AssetBundleRequestOptions> BundleNameToRequestOptions = new Dictionary<string, AssetBundleRequestOptions>();

        internal Dictionary<string, AssetBundleRequestOptions> BundleNameToPreviousRequestOptions = new Dictionary<string, AssetBundleRequestOptions>();

        internal Dictionary<string, ContentCatalogDataEntry> BundleNameToCatalogEntry = new Dictionary<string, ContentCatalogDataEntry>();

        internal Dictionary<string, string> GroupNameToBuildPath = new Dictionary<string, string>();

        internal Dictionary<string, AddressableAssetEntry> GuidToEntry = new Dictionary<string, AddressableAssetEntry>();
        internal Dictionary<string, AssetType> AssetPathToTypeMap = new Dictionary<string, AssetType>();
    }

    internal class FileObjectData
    {
        // id's for internal explicit asset and implicit asset
        public Dictionary<ObjectIdentifier, (int, int)> InternalObjectIds = new Dictionary<ObjectIdentifier, (int, int)>();

        public Dictionary<BuildLayout.ObjectData, ObjectIdentifier> Objects = new Dictionary<BuildLayout.ObjectData, ObjectIdentifier>();

        public void Add(ObjectIdentifier buildObjectIdentifier, BuildLayout.ObjectData layoutObject, int assetId, int objectIndex)
        {
            InternalObjectIds[buildObjectIdentifier] = (assetId, objectIndex);
            Objects[layoutObject] = buildObjectIdentifier;
        }

        public bool TryGetObjectReferenceData(ObjectIdentifier obj, out (int, int) value)
        {
            if (!InternalObjectIds.TryGetValue(obj, out (int, int) data))
            {
                value = default;
                return false;
            }

            value = data;
            return true;
        }

        public bool TryGetObjectIdentifier(BuildLayout.ObjectData obj, out ObjectIdentifier objectIdOut)
        {
            if (!Objects.TryGetValue(obj, out objectIdOut))
            {
                objectIdOut = default;
                return false;
            }

            return true;
        }
    }
}
