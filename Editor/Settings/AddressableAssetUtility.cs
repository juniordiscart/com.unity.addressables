using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Utilities;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.VersionControl;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Settings
{
    using Object = UnityEngine.Object;

    internal static class AddressableAssetUtility
    {
#if !UNITY_2020_3_OR_NEWER
        //these extention methods are needed prior to 2020.3 since they are not available
        public static void Append(this ref Hash128 thisHash, string val)
        {
            Hash128 valHash = Hash128.Compute(val);
            HashUtilities.AppendHash(ref valHash, ref thisHash);
        }

        public static void Append(this ref Hash128 thisHash, int val)
        {
            Hash128 valHash = default;
            HashUtilities.ComputeHash128(ref val, ref valHash);
            HashUtilities.AppendHash(ref valHash, ref thisHash);
        }

        public static void Append(this ref Hash128 thisHash, Hash128[] vals)
        {
            Hash128 valHash = default;
            for (int i = 0; i < vals.Length; i++)
            {
                HashUtilities.ComputeHash128(ref vals[i], ref valHash);
                HashUtilities.AppendHash(ref valHash, ref thisHash);
            }
        }

        public static void Append<T>(this ref Hash128 thisHash, ref T val) where T : unmanaged
        {
            Hash128 valHash = default;
            HashUtilities.ComputeHash128(ref val, ref valHash);
            HashUtilities.AppendHash(ref valHash, ref thisHash);
        }
#endif

        internal static bool IsInResources(string path)
        {
#if NET_UNITY_4_8
            return path.Replace('\\', '/').Contains("/Resources/", StringComparison.OrdinalIgnoreCase);
#else
            return path.Replace('\\', '/').ToLower().Contains("/resources/");
#endif
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static bool StringContains(string input, string value, StringComparison comp)
        {
#if NET_UNITY_4_8
            return input.Contains(value, comp);
#else
            return input.Contains(value);
#endif
        }

        internal static bool TryGetPathAndGUIDFromTarget(Object target, out string path, out string guid)
        {
            if (target == null)
            {
                guid = string.Empty;
                path = string.Empty;
                return false;
            }
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out guid, out long id))
            {
                guid = string.Empty;
                path = string.Empty;
                return false;
            }
            path = AssetDatabase.GetAssetOrScenePath(target);
            if (!IsPathValidForEntry(path))
                return false;
            return true;
        }

        private static string isEditorFolder = $"{Path.DirectorySeparatorChar}Editor";
        private static string insideEditorFolder = $"{Path.DirectorySeparatorChar}Editor{Path.DirectorySeparatorChar}";
        static HashSet<string> excludedExtensions = new HashSet<string>(new string[] { ".cs", ".js", ".boo", ".exe", ".dll", ".meta", ".preset", ".asmdef" });
        internal static bool IsPathValidForEntry(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (path.Contains('\\'))
                path = path.Replace('\\', Path.DirectorySeparatorChar);

            if (Path.DirectorySeparatorChar != '/' && path.Contains('/'))
                path = path.Replace('/', Path.DirectorySeparatorChar);

            if (!path.StartsWith("Assets", StringComparison.OrdinalIgnoreCase) && !IsPathValidPackageAsset(path))
                return false;

            string ext = Path.GetExtension(path);
            if (string.IsNullOrEmpty(ext))
            {
                // is folder
                if (path == "Assets")
                    return false;
                int editorIndex = path.IndexOf(isEditorFolder, StringComparison.OrdinalIgnoreCase);
                if (editorIndex != -1)
                {
                    int length = path.Length;
                    if (editorIndex == length - 7)
                        return false;
                    if (path[editorIndex + 7] == '/')
                        return false;
                    // Could still have something like Assets/editorthings/Editor/things, but less likely
                    if (StringContains(path, insideEditorFolder, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                if (String.Equals(path, CommonStrings.UnityEditorResourcePath, StringComparison.Ordinal) ||
                    String.Equals(path, CommonStrings.UnityDefaultResourcePath, StringComparison.Ordinal) ||
                    String.Equals(path, CommonStrings.UnityBuiltInExtraPath, StringComparison.Ordinal))
                    return false;
            }
            else
            {
                // asset type
                if (StringContains(path, insideEditorFolder, StringComparison.OrdinalIgnoreCase))
                    return false;
                if (excludedExtensions.Contains(ext))
                    return false;
            }

            var settings = AddressableAssetSettingsDefaultObject.SettingsExists ? AddressableAssetSettingsDefaultObject.Settings : null;
            if (settings != null && path.StartsWith(settings.ConfigFolder, StringComparison.Ordinal))
                return false;

            return true;
        }

        internal static bool IsPathValidPackageAsset(string pathLowerCase)
        {
            string[] splitPath = pathLowerCase.Split(Path.DirectorySeparatorChar);

            if (splitPath.Length < 3)
                return false;
            if (!String.Equals(splitPath[0], "packages", StringComparison.OrdinalIgnoreCase))
                return false;
            if (String.Equals(splitPath[2], "package.json", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        static HashSet<Type> validTypes = new HashSet<Type>();

        internal static Type MapEditorTypeToRuntimeType(Type t, bool allowFolders)
        {
            //type is valid and already seen (most common)
            if (validTypes.Contains(t))
                return t;

            //removes the need to check this outside of this call
            if (t == null)
                return t;

            //check for editor type, this will get hit once for each new type encountered
            if (!t.Assembly.IsDefined(typeof(AssemblyIsEditorAssembly), true) && !Build.BuildUtility.IsEditorAssembly(t.Assembly))
            {
                validTypes.Add(t);
                return t;
            }

            if (t == typeof(DefaultAsset))
                return typeof(DefaultAsset);

            //try to remap the editor type to a runtime type
            return MapEditorTypeToRuntimeTypeInternal(t);
        }

        static Type MapEditorTypeToRuntimeTypeInternal(Type t)
        {
            if (t == typeof(UnityEditor.Animations.AnimatorController))
                return typeof(RuntimeAnimatorController);
            if (t == typeof(UnityEditor.SceneAsset))
                return typeof(UnityEngine.ResourceManagement.ResourceProviders.SceneInstance);
            if (t.FullName == "UnityEditor.Audio.AudioMixerController")
                return typeof(UnityEngine.Audio.AudioMixer);
            if (t.FullName == "UnityEditor.Audio.AudioMixerGroupController")
                return typeof(UnityEngine.Audio.AudioMixerGroup);
            return null;
        }

        internal static void ConvertAssetBundlesToAddressables()
        {
            AssetDatabase.RemoveUnusedAssetBundleNames();
            var bundleList = AssetDatabase.GetAllAssetBundleNames();

            float fullCount = bundleList.Length;
            int currCount = 0;

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            foreach (var bundle in bundleList)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Converting Legacy Asset Bundles", bundle, currCount / fullCount))
                    break;

                currCount++;
                var group = settings.CreateGroup(bundle, false, false, false, null);
                var schema = group.AddSchema<BundledAssetGroupSchema>();
                schema.Validate();
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                group.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

                var assetList = AssetDatabase.GetAssetPathsFromAssetBundle(bundle);

                foreach (var asset in assetList)
                {
                    var guid = AssetDatabase.AssetPathToGUID(asset);
                    settings.CreateOrMoveEntry(guid, group, false, false);
                    var imp = AssetImporter.GetAtPath(asset);
                    if (imp != null)
                        imp.SetAssetBundleNameAndVariant(string.Empty, string.Empty);
                }
            }

            if (fullCount > 0)
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            EditorUtility.ClearProgressBar();
            AssetDatabase.RemoveUnusedAssetBundleNames();
        }

        /// <summary>
        /// Get all types that can be assigned to type T
        /// </summary>
        /// <typeparam name="T">The class type to use as the base class or interface for all found types.</typeparam>
        /// <returns>A list of types that are assignable to type T.  The results are cached.</returns>
        public static List<Type> GetTypes<T>()
        {
            return TypeManager<T>.Types;
        }

        /// <summary>
        /// Get all types that can be assigned to type rootType.
        /// </summary>
        /// <param name="rootType">The class type to use as the base class or interface for all found types.</param>
        /// <returns>A list of types that are assignable to type T.  The results are not cached.</returns>
        public static List<Type> GetTypes(Type rootType)
        {
            return TypeManager.GetManagerTypes(rootType);
        }

        class TypeManager
        {
            public static List<Type> GetManagerTypes(Type rootType)
            {
                var types = new List<Type>();
                try
                {
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (a.IsDynamic)
                            continue;
                        foreach (var t in a.ExportedTypes)
                        {
                            if (t != rootType && rootType.IsAssignableFrom(t) && !t.IsAbstract)
                                types.Add(t);
                        }
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

                return types;
            }
        }

        class TypeManager<T> : TypeManager
        {
            // ReSharper disable once StaticMemberInGenericType
            static List<Type> s_Types;

            public static List<Type> Types
            {
                get
                {
                    if (s_Types == null)
                        s_Types = GetManagerTypes(typeof(T));

                    return s_Types;
                }
            }
        }

        internal static bool SafeMoveResourcesToGroup(AddressableAssetSettings settings, AddressableAssetGroup targetGroup, List<string> paths, List<string> guids, bool showDialog = true)
        {
            if (targetGroup == null)
            {
                Debug.LogWarning("No valid group to move Resources to");
                return false;
            }

            if (paths == null || paths.Count == 0)
            {
                Debug.LogWarning("No valid Resources found to move");
                return false;
            }

            if (guids == null)
            {
                guids = new List<string>();
                foreach (var p in paths)
                    guids.Add(AssetDatabase.AssetPathToGUID(p));
            }

            Dictionary<string, string> guidToNewPath = new Dictionary<string, string>();

            var message = "Any assets in Resources that you wish to mark as Addressable must be moved within the project. We will move the files to:\n\n";
            for (int i = 0; i < guids.Count; i++)
            {
                var newName = paths[i].Replace("\\", "/");
                newName = newName.Replace("Resources", "Resources_moved");
                newName = newName.Replace("resources", "resources_moved");
                if (newName == paths[i])
                    continue;

                guidToNewPath.Add(guids[i], newName);
                message += newName + "\n";
            }

            message += "\nAre you sure you want to proceed?";
            if (!showDialog || EditorUtility.DisplayDialog("Move From Resources", message, "Yes", "No"))
            {
                settings.MoveAssetsFromResources(guidToNewPath, targetGroup);
                return true;
            }

            return false;
        }

        static Dictionary<Type, string> s_CachedDisplayNames = new Dictionary<Type, string>();

        internal static string GetCachedTypeDisplayName(Type type)
        {
            string result = "<none>";
            if (type != null)
            {
                if (!s_CachedDisplayNames.TryGetValue(type, out result))
                {
                    var displayNameAtr = type.GetCustomAttribute<DisplayNameAttribute>();
                    if (displayNameAtr != null)
                    {
                        result = (string)displayNameAtr.DisplayName;
                    }
                    else
                        result = type.Name;

                    s_CachedDisplayNames.Add(type, result);
                }
            }

            return result;
        }

        struct PackageData
        {
            public string version;
        }

        private static string m_Version = null;

        internal static string GetVersionFromPackageData()
        {
            if (string.IsNullOrEmpty(m_Version))
            {
                var jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.unity.addressables/package.json");
                var packageData = JsonUtility.FromJson<PackageData>(jsonFile.text);
                var split = packageData.version.Split('.');
                if (split.Length < 2)
                    throw new Exception("Could not get correct version data for Addressables package");
                m_Version = $"{split[0]}.{split[1]}";
            }

            return m_Version;
        }

        public static string GenerateDocsURL(string page)
        {
            return $"https://docs.unity3d.com/Packages/com.unity.addressables@{GetVersionFromPackageData()}/manual/{page}";
        }

        internal static bool IsUsingVCIntegration()
        {
            return Provider.isActive && Provider.enabled;
        }

        internal static bool IsVCAssetOpenForEdit(string path)
        {
            AssetList VCAssets = GetVCAssets(path);
            foreach (Asset vcAsset in VCAssets)
            {
                if (vcAsset.path == path)
                    return Provider.IsOpenForEdit(vcAsset);
            }

            return false;
        }

        internal static AssetList GetVCAssets(string path)
        {
            VersionControl.Task op = Provider.Status(path);
            op.Wait();
            return op.assetList;
        }

        private static bool MakeAssetEditable(Asset asset)
        {
            if (!AssetDatabase.IsOpenForEdit(asset.path))
                return AssetDatabase.MakeEditable(asset.path);
            return false;
        }

        internal static bool OpenAssetIfUsingVCIntegration(Object target, bool exitGUI = false)
        {
            if (!IsUsingVCIntegration() || target == null)
                return false;
            return OpenAssetIfUsingVCIntegration(AssetDatabase.GetAssetOrScenePath(target), exitGUI);
        }

        internal static bool OpenAssetIfUsingVCIntegration(string path, bool exitGUI = false)
        {
            if (!IsUsingVCIntegration() || string.IsNullOrEmpty(path))
                return false;

            AssetList assets = GetVCAssets(path);
            var uneditableAssets = new List<Asset>();
            string message = "Check out file(s)?\n\n";
            foreach (Asset asset in assets)
            {
                if (!Provider.IsOpenForEdit(asset))
                {
                    uneditableAssets.Add(asset);
                    message += $"{asset.path}\n";
                }
            }

            if (uneditableAssets.Count == 0)
                return false;

            bool openedAsset = true;
            if (EditorUtility.DisplayDialog("Attempting to modify files that are uneditable", message, "Yes", "No"))
            {
                foreach (Asset asset in uneditableAssets)
                {
                    if (!MakeAssetEditable(asset))
                        openedAsset = false;
                }
            }
            else
                openedAsset = false;

            if (exitGUI)
                GUIUtility.ExitGUI();
            return openedAsset;
        }

        internal static bool InstallCCDPackage()
        {
#if !ENABLE_CCD
            var confirm = EditorUtility.DisplayDialog("Install CCD Management SDK Package",
                "Are you sure you want to install the CCD Management SDK package and enable CCD features within Addressables?\nTo remove this package and its related features please use the Package manager, or uncheck the Addressable Asset Settings > Cloud Content Delivery > Enable CCD Features toggle.",
                "Yes", "No");
            if (confirm)
            {
                AddressableAnalytics.ReportUsageEvent(AddressableAnalytics.UsageEventType.InstallCCDManagementPackage);
                Client.Add("com.unity.services.ccd.management@2.1.0");
                AddressableAssetSettingsDefaultObject.Settings.CCDEnabled = true;
            }
#endif
            return AddressableAssetSettingsDefaultObject.Settings.CCDEnabled;
        }

        internal static bool RemoveCCDPackage()
        {
            var confirm = EditorUtility.DisplayDialog("Remove CCD Management SDK Package", "Are you sure you want to remove the CCD Management SDK package?", "Yes", "No");
            if (confirm)
            {
#if (UNITY_2019_4_OR_NEWER)
                Client.Remove("com.unity.services.ccd.management");
                AddressableAssetSettingsDefaultObject.Settings.CCDEnabled = false;
#endif
            }

            return AddressableAssetSettingsDefaultObject.Settings.CCDEnabled;
        }

        internal static string GetMd5Hash(string path)
        {
            string hashString;
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    var hash = md5.ComputeHash(stream);
                    hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }

            return hashString;
        }


        internal static System.Threading.Tasks.Task ParallelForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, System.Threading.Tasks.Task> body)
        {
            async System.Threading.Tasks.Task AwaitPartition(IEnumerator<T> partition)
            {
                using (partition)
                {
                    while (partition.MoveNext())
                    {
                        await body(partition.Current);
                    }
                }
            }

            return System.Threading.Tasks.Task.WhenAll(
                Partitioner
                    .Create(source)
                    .GetPartitions(dop)
                    .AsParallel()
                    .Select(p => AwaitPartition(p)));
        }

        internal class SortedDelegate<T1, T2, T3, T4>
        {
            struct QueuedValues
            {
                public T1 arg1;
                public T2 arg2;
                public T3 arg3;
                public T4 arg4;
            }

            public delegate void Delegate(T1 arg1, T2 arg2, T3 arg3, T4 arg4);

            private readonly SortedList<int, Delegate> m_SortedInvocationList = new SortedList<int, Delegate>();

            private readonly List<QueuedValues> m_InvokeQueue = new List<QueuedValues>();
            private readonly List<(int, Delegate)> m_RegisterQueue = new List<(int, Delegate)> ();
            private bool m_IsInvoking;

            /// <summary>
            /// Removes a delegate from the invocation list.
            /// </summary>
            /// <param name="toUnregister">Delegate to remove</param>
            public void Unregister(Delegate toUnregister)
            {
                IList<int> keys = m_SortedInvocationList.Keys;
                for (int i = 0; i < keys.Count; ++i)
                {
                    m_SortedInvocationList[keys[i]] -= toUnregister;
                    if (m_SortedInvocationList[keys[i]] == null)
                    {
                        m_SortedInvocationList.Remove(keys[i]);
                        break;
                    }
                }

                if (m_IsInvoking && m_RegisterQueue.Count > 0)
                {
                    for (int i = m_RegisterQueue.Count - 1; i >= 0; --i)
                    {
                        if (m_RegisterQueue[i].Item2 == toUnregister)
                        {
                            m_RegisterQueue.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            /// <summary>
            /// Add a delegate to the invocation list
            /// </summary>
            /// <param name="toRegister">Delegate to add</param>
            /// <param name="order">Order to call the delegate in the invocation list</param>
            public void Register(Delegate toRegister, int order)
            {
                if (m_IsInvoking)
                {
                    m_RegisterQueue.Add((order, toRegister));
                    return;
                }

                FlushRegistrationQueue();
                RegisterToInvocationList(toRegister, order);
                FlushInvokeQueue();
            }

            private void RegisterToInvocationList(Delegate toRegister, int order)
            {
                // unregister first, this will remove the delegate from another order if it is added
                Unregister(toRegister);
                if (m_SortedInvocationList.ContainsKey(order))
                    m_SortedInvocationList[order] += toRegister;
                else
                    m_SortedInvocationList.Add(order, toRegister);
            }

            /// <summary>
            /// Invoke all delegates in the invocation list for the given parameters
            /// </summary>
            /// <param name="arg1"></param>
            /// <param name="arg2"></param>
            /// <param name="arg3"></param>
            /// <param name="arg4"></param>
            public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                if (m_IsInvoking)
                    return;

                FlushRegistrationQueue();
                Invoke_Internal(arg1, arg2, arg3, arg4);
                FlushInvokeQueue();
            }

            private void Invoke_Internal(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                m_IsInvoking = true;
                foreach (var invocationList in m_SortedInvocationList)
                {
                    invocationList.Value?.Invoke(arg1, arg2, arg3, arg4);
                }

                m_IsInvoking = false;
            }

            private void FlushRegistrationQueue()
            {
                if (m_RegisterQueue.Count > 0)
                {
                    for (int i = m_RegisterQueue.Count - 1; i >= 0; --i)
                        RegisterToInvocationList(m_RegisterQueue[i].Item2, m_RegisterQueue[i].Item1);
                }
            }

            private void FlushInvokeQueue()
            {
                if (m_InvokeQueue.Count > 0)
                {
                    // keep looping the invoke buffer in case new invokes get added during invoke
                    while (m_InvokeQueue.Count > 0)
                    {
                        for (int i = m_InvokeQueue.Count - 1; i >= 0; --i)
                        {
                            Invoke_Internal(m_InvokeQueue[i].arg1, m_InvokeQueue[i].arg2, m_InvokeQueue[i].arg3, m_InvokeQueue[i].arg4);
                            m_InvokeQueue.RemoveAt(i);
                        }
                    }
                }
            }

            /// <summary>
            /// Will invoke with the given parameters if there is any delegates in the invocation list, and not currently invoking
            /// else, will save the values and invoke when there is a delegate registered.
            /// </summary>
            /// <param name="arg1"></param>
            /// <param name="arg2"></param>
            /// <param name="arg3"></param>
            /// <param name="arg4"></param>
            public void TryInvokeOrDelayToReady(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                if (m_SortedInvocationList.Count == 0 || m_IsInvoking)
                {
                    m_InvokeQueue.Add(new QueuedValues {arg1 = arg1, arg2 = arg2, arg3 = arg3, arg4 = arg4});
                }
                else
                {
                    Invoke(arg1, arg2, arg3, arg4);
                }
            }

            public static SortedDelegate<T1, T2, T3, T4> operator +(SortedDelegate<T1, T2, T3, T4> self, Delegate delegateToAdd)
            {
                int lastInOrder = self.m_SortedInvocationList.Keys[self.m_SortedInvocationList.Count - 1];
                self.Register(delegateToAdd, lastInOrder + 1);
                return self;
            }

            public static SortedDelegate<T1, T2, T3, T4> operator -(SortedDelegate<T1, T2, T3, T4> self, Delegate delegateToRemove)
            {
                self.Unregister(delegateToRemove);
                return self;
            }

            public static bool operator ==(SortedDelegate<T1, T2, T3, T4> obj1, SortedDelegate<T1, T2, T3, T4> obj2)
            {
                bool aNull = ReferenceEquals(obj1, null);
                bool bNull = ReferenceEquals(obj2, null);

                if (aNull && bNull)
                    return true;
                if (!aNull && bNull)
                    return obj1.m_SortedInvocationList.Count == 0;
                if (aNull && !bNull)
                    return obj2.m_SortedInvocationList.Count == 0;
                if (ReferenceEquals(obj1, obj2))
                    return true;
                return obj1.Equals(obj2);
            }

            public static bool operator !=(SortedDelegate<T1, T2, T3, T4> lhs, SortedDelegate<T1, T2, T3, T4> rhs)
            {
                return !(lhs == rhs);
            }

            protected bool Equals(SortedDelegate<T1, T2, T3, T4> other)
            {
                return Equals(m_SortedInvocationList, other.m_SortedInvocationList);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((SortedDelegate<T1, T2, T3, T4>)obj);
            }

            public override int GetHashCode()
            {
                return (m_SortedInvocationList != null ? m_SortedInvocationList.GetHashCode() : 0);
            }
        }

        internal static void MoveEntriesToGroup(AddressableAssetSettings settings, List<AddressableAssetEntry> entries, AddressableAssetGroup group)
        {
            foreach (AddressableAssetEntry entry in entries)
            {
                if (entry.parentGroup != group)
                {
                    settings.MoveEntry(entry, group, entry.ReadOnly, true);
                }
            }
        }
    }
}
