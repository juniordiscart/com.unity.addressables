using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.GUI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.Serialization;

// ReSharper disable DelegateSubtraction

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// Contains user defined variables to control build parameters.
    /// </summary>
    [Serializable]
    public class AddressableAssetProfileSettings
    {
        internal delegate string ProfileStringEvaluationDelegate(string key);

        [NonSerialized]
        internal ProfileStringEvaluationDelegate onProfileStringEvaluation;

        internal void RegisterProfileStringEvaluationFunc(ProfileStringEvaluationDelegate f)
        {
            onProfileStringEvaluation -= f;
            onProfileStringEvaluation += f;
        }

        internal void UnregisterProfileStringEvaluationFunc(ProfileStringEvaluationDelegate f)
        {
            onProfileStringEvaluation -= f;
        }

        [Serializable]
        internal class BuildProfile
        {
            [Serializable]
            internal class ProfileEntry
            {
                [FormerlySerializedAs("m_id")]
                [SerializeField]
                string m_Id;

                public string id
                {
                    get { return m_Id; }
                    set { m_Id = value; m_CurrentHash = default; }
                }

                [FormerlySerializedAs("m_value")]
                [SerializeField]
                string m_Value;

                Hash128 m_CurrentHash;
                internal Hash128 currentHash
                {
                    get
                    {
                        if (!m_CurrentHash.isValid)
                        {
                            m_CurrentHash.Append(m_Id);
                            m_CurrentHash.Append(m_Value);
                        }
                        return m_CurrentHash;
                    }
                }

                public string value
                {
                    get { return m_Value; }
                    set { m_Value = value; m_CurrentHash = default; }
                }

                internal ProfileEntry()
                {
                }

                internal ProfileEntry(string id, string v)
                {
                    m_Id = id;
                    m_Value = v;
                }

                internal ProfileEntry(ProfileEntry copy)
                {
                    m_Id = copy.m_Id;
                    m_Value = copy.m_Value;
                }
            }

            [NonSerialized]
            internal AddressableAssetProfileSettings m_ProfileParent;

            [FormerlySerializedAs("m_inheritedParent")]
            [SerializeField]
            string m_InheritedParent;

            public string inheritedParent
            {
                get { return m_InheritedParent; }
                set { m_InheritedParent = value; m_CurrentHash = default; }
            }

            [FormerlySerializedAs("m_id")]
            [SerializeField]
            string m_Id;

            internal string id
            {
                get { return m_Id; }
                set { m_Id = value; m_CurrentHash = default; }
            }

            [FormerlySerializedAs("m_profileName")]
            [SerializeField]
            string m_ProfileName;

            internal string profileName
            {
                get { return m_ProfileName; }
                set { m_ProfileName = value; m_CurrentHash = default; }
            }

            [FormerlySerializedAs("m_values")]
            [SerializeField]
            List<ProfileEntry> m_Values = new List<ProfileEntry>();


            Hash128 m_CurrentHash;
            internal Hash128 currentHash
            {
                get
                {
                    if (!m_CurrentHash.isValid)
                    {
                        m_CurrentHash.Append(m_Id);
                        m_CurrentHash.Append(m_ProfileName);
                        m_CurrentHash.Append(m_InheritedParent);
                        foreach (var e in m_Values)
                        {
                            var peh = e.currentHash;
                            m_CurrentHash.Append(ref peh);
                        }
                    }
                    return m_CurrentHash;
                }
            }

            internal List<ProfileEntry> values
            {
                get { return m_Values; }
                set { m_Values = value; m_CurrentHash = default; }
            }

            internal BuildProfile(string name, BuildProfile copyFrom, AddressableAssetProfileSettings ps)
            {
                m_InheritedParent = null;
                id = GUID.Generate().ToString();
                profileName = name;
                values.Clear();
                m_ProfileParent = ps;

                if (copyFrom != null)
                {
                    foreach (var v in copyFrom.values)
                        values.Add(new ProfileEntry(v));
                    m_InheritedParent = copyFrom.m_InheritedParent;
                }
            }

            internal void OnAfterDeserialize(AddressableAssetProfileSettings ps)
            {
                m_ProfileParent = ps;
            }

            internal string GetValueById(string variableId)
            {
                var i = values.FindIndex(v => v.id == variableId);
                if (i >= 0)
                    return values[i].value;


                if (m_ProfileParent == null)
                {
                    return null;
                }

                return m_ProfileParent.GetValueById(m_InheritedParent, variableId);
            }

            internal void SetValueById(string variableId, string val)
            {
                var i = values.FindIndex(v => v.id == variableId);
                if (i >= 0)
                    values[i].value = val;
                m_CurrentHash = default;
            }
        }

        internal void OnAfterDeserialize(AddressableAssetSettings settings)
        {
            m_Settings = settings;
            foreach (var prof in m_Profiles)
            {
                prof.OnAfterDeserialize(this);
            }
        }

        [NonSerialized]
        AddressableAssetSettings m_Settings;

        [FormerlySerializedAs("m_profiles")]
        [SerializeField]
        List<BuildProfile> m_Profiles = new List<BuildProfile>();

        internal List<BuildProfile> profiles
        {
            get { return m_Profiles; }
        }

        /// <summary>
        /// A container for profile specific data, such as the name and ID of a profile.
        /// </summary>
        [Serializable]
        public class ProfileIdData
        {
            [FormerlySerializedAs("m_id")]
            [SerializeField]
            string m_Id;

            /// <summary>
            /// The unique ID set to identify a specific profile.
            /// </summary>
            public string Id
            {
                get { return m_Id; }
            }

            [FormerlySerializedAs("m_name")]
            [SerializeField]
            string m_Name;

            /// <summary>
            /// The name of the specific profile.
            /// </summary>
            public string ProfileName
            {
                get { return m_Name; }
            }

            Hash128 m_CurrentHash;
            internal Hash128 currentHash
            {
                get
                {
                    if (!m_CurrentHash.isValid)
                    {
                        m_CurrentHash.Append(m_Id);
                        m_CurrentHash.Append(m_Name);
                        m_CurrentHash.Append(ref m_InlineUsage);
                    }
                    return m_CurrentHash;
                }
            }
            /// <summary>
            /// Changes the name of a given profile and updates the values in the profile settings.
            /// </summary>
            /// <param name="newName">The new name you want to set the profile to.</param>
            /// <param name="profileSettings">The profile settings object that contains this profile.</param>
            public void SetName(string newName, AddressableAssetProfileSettings profileSettings)
            {
                if (!profileSettings.ValidateNewVariableName(newName))
                    return;

                var currRefStr = "[" + m_Name + "]";
                var newRefStr = "[" + newName + "]";

                m_Name = newName;

                foreach (var p in profileSettings.profiles)
                {
                    foreach (var v in p.values)
                        v.value = v.value.Replace(currRefStr, newRefStr);
                }
                m_CurrentHash = default;
                profileSettings.SetDirty(AddressableAssetSettings.ModificationEvent.ProfileModified, null, false);
                ProfileWindow.MarkForReload();
            }

            [FormerlySerializedAs("m_inlineUsage")]
            [SerializeField]
            bool m_InlineUsage;

            internal bool InlineUsage
            {
                get { return m_InlineUsage; }
            }

            internal ProfileIdData()
            {
            }

            /// <summary>
            /// Create a new ProfileIdData.
            /// </summary>
            /// <param name="entryId">The unique ID for this ProfileIdData</param>
            /// <param name="entryName">The name of the ProfileIdData.  ProfileIdData names should be unique in a given AddressableAssetProfileSettings.</param>
            /// <param name="inline">False by default, this informs the BuildProifleSettingsEditor on if it should evaluate the ProfileIdData directly (true) 
            /// or get the value by Id first before evaluation (false).</param>
            public ProfileIdData(string entryId, string entryName, bool inline)
            {
                m_Id = entryId;
                m_Name = entryName;
                m_InlineUsage = inline;
            }
        }

        [FormerlySerializedAs("m_profileEntryNames")]
        [SerializeField]
        List<ProfileIdData> m_ProfileEntryNames = new List<ProfileIdData>();

        [FormerlySerializedAs("m_profileVersion")]
        [SerializeField]
        internal int m_ProfileVersion;

        const int k_CurrentProfileVersion = 1;


        Hash128 m_CurrentHash;
        internal Hash128 currentHash
        {
            get
            {
                if (!m_CurrentHash.isValid)
                {
                    m_CurrentHash.Append(m_ProfileVersion);
                    foreach (var p in m_ProfileEntryNames)
                    {
                        var peh = p.currentHash;
                        m_CurrentHash.Append(ref peh);
                    }
                    foreach (var p in m_Profiles)
                    {
                        var peh = p.currentHash;
                        m_CurrentHash.Append(ref peh);
                    }
                }
                return m_CurrentHash;
            }
        }

        internal List<ProfileIdData> profileEntryNames
        {
            get
            {
                if (m_ProfileVersion < k_CurrentProfileVersion)
                {
                    m_ProfileVersion = k_CurrentProfileVersion;
                    //migration cleanup from old way of doing "custom" values...
                    var removeId = string.Empty;
                    foreach (var idPair in m_ProfileEntryNames)
                    {
                        if (idPair.ProfileName == customEntryString)
                        {
                            removeId = idPair.Id;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(removeId))
                        RemoveValue(removeId);
                }


                return m_ProfileEntryNames;
            }
        }

        /// <summary>
        /// Text that represents a custom profile entry.
        /// </summary>
        public const string customEntryString = "<custom>";

        /// <summary>
        /// Text that represents when the default settings path is being used.
        /// </summary>
        public const string defaultSettingsPathString = "<default settings path>";

        /// <summary>
        /// Text that represents an undefined profile entry.
        /// </summary>
        public const string undefinedEntryValue = "<undefined>";

        const string k_RootProfileName = "Default";

        /// <summary>
        /// Get the profile specific data for a given profile id.
        /// </summary>
        /// <param name="id">The profile id you're requesting data for.</param>
        /// <returns>A ProfileIdData with information about a specific profile.</returns>
        public ProfileIdData GetProfileDataById(string id)
        {
            return profileEntryNames.Find(p => p.Id == id);
        }

        /// <summary>
        /// Get the profile specific data for a given profile name.
        /// </summary>
        /// <param name="name">The profile name you're requesting data for.</param>
        /// <returns>A ProfileIdData with information about a specific profile.</returns>
        public ProfileIdData GetProfileDataByName(string name)
        {
            return profileEntryNames.Find(p => p.ProfileName == name);
        }

        /// <summary>
        /// Clears out the list of profiles, then creates a new default one.
        /// </summary>
        /// <returns>Returns the ID of the newly created default profile.</returns>
        public string Reset()
        {
            m_Profiles = new List<BuildProfile>();
            m_CurrentHash = default;
            return CreateDefaultProfile();
        }

        /// <summary>
        /// Evaluate a string given a profile id.
        /// </summary>
        /// <param name="profileId">The profile id to use for evaluation.</param>
        /// <param name="varString">The string to evaluate.  Any tokens surrounded by '[' and ']' will be replaced with matching variables.</param>
        /// <returns>The evaluated string.</returns>
        public string EvaluateString(string profileId, string varString)
        {
            Func<string, string> getVal = s =>
            {
                var v = GetValueByName(profileId, s);
                if (string.IsNullOrEmpty(v))
                {
                    if (onProfileStringEvaluation != null)
                    {
                        foreach (var i in onProfileStringEvaluation.GetInvocationList())
                        {
                            var del = (ProfileStringEvaluationDelegate)i;
                            v = del(s);
                            if (!string.IsNullOrEmpty(v))
                                return v;
                        }
                    }

                    v = AddressablesRuntimeProperties.EvaluateProperty(s);
                }

                return v;
            };

            return AddressablesRuntimeProperties.EvaluateString(varString, '[', ']', getVal);
        }

        internal void Validate(AddressableAssetSettings addressableAssetSettings)
        {
            CreateDefaultProfile();
        }

        internal string CreateDefaultProfile()
        {
            if (!ValidateProfiles())
            {
                m_ProfileEntryNames.Clear();
                m_Profiles.Clear();

                AddProfile(k_RootProfileName, null);
                CreateValue("BuildTarget", "[UnityEditor.EditorUserBuildSettings.activeBuildTarget]");
                CreateValue(AddressableAssetSettings.kLocalBuildPath, AddressableAssetSettings.kLocalBuildPathValue);
                CreateValue(AddressableAssetSettings.kLocalLoadPath, AddressableAssetSettings.kLocalLoadPathValue);
                CreateValue(AddressableAssetSettings.kRemoteBuildPath, AddressableAssetSettings.kRemoteBuildPathValue);
                CreateValue(AddressableAssetSettings.kRemoteLoadPath, AddressableAssetSettings.RemoteLoadPathValue);
            }

            return GetDefaultProfileId();
        }

        string GetDefaultProfileId()
        {
            var def = GetDefaultProfile();
            if (def != null)
                return def.id;
            return null;
        }

        BuildProfile GetDefaultProfile()
        {
            BuildProfile profile = null;
            if (m_Profiles.Count > 0)
                profile = m_Profiles[0];
            return profile;
        }

        bool ValidateProfiles()
        {
            if (m_Profiles.Count == 0)
                return false;

            var root = m_Profiles[0];
            if (root == null || root.values == null)
                return false;

            foreach (var i in profileEntryNames)
                if (string.IsNullOrEmpty(i.Id) || string.IsNullOrEmpty(i.ProfileName))
                    return false;

            var rootValueCount = root.values.Count;
            for (int index = 1; index < m_Profiles.Count; index++)
            {
                var profile = m_Profiles[index];

                if (profile == null || string.IsNullOrEmpty(profile.profileName))
                    return false;

                if (profile.values == null || profile.values.Count != rootValueCount)
                    return false;

                for (int i = 0; i < rootValueCount; i++)
                {
                    if (root.values[i].id != profile.values[i].id)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get all available variable names
        /// </summary>
        /// <returns>The variable names, sorted alphabetically.</returns>
        public List<string> GetVariableNames()
        {
            HashSet<string> names = new HashSet<string>();
            foreach (var entry in profileEntryNames)
                names.Add(entry.ProfileName);
            var list = names.ToList();
            list.Sort();
            return list;
        }

        /// <summary>
        /// Get all profile names.
        /// </summary>
        /// <returns>The list of profile names.</returns>
        public List<string> GetAllProfileNames()
        {
            CreateDefaultProfile();
            List<string> result = new List<string>();
            foreach (var p in m_Profiles)
                result.Add(p.profileName);
            return result;
        }

        /// <summary>
        /// Get a profile's display name.
        /// </summary>
        /// <param name="profileId">The profile id.</param>
        /// <returns>The display name of the profile.  Returns empty string if not found.</returns>
        public string GetProfileName(string profileId)
        {
            foreach (var p in m_Profiles)
            {
                if (p.id == profileId)
                    return p.profileName;
            }

            return "";
        }

        /// <summary>
        /// Get the id of a given display name.
        /// </summary>
        /// <param name="profileName">The profile name.</param>
        /// <returns>The id of the profile.  Returns empty string if not found.</returns>
        public string GetProfileId(string profileName)
        {
            foreach (var p in m_Profiles)
            {
                if (p.profileName == profileName)
                    return p.id;
            }

            return "";
        }

        /// <summary>
        /// Gets the set of all profile ids.
        /// </summary>
        /// <returns>The set of profile ids.</returns>
        public HashSet<string> GetAllVariableIds()
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (var v in profileEntryNames)
                ids.Add(v.Id);
            return ids;
        }

        /// <summary>
        /// Marks the object as modified.
        /// </summary>
        /// <param name="modificationEvent">The event type that is changed.</param>
        /// <param name="eventData">The object data that corresponds to the event.</param>
        /// <param name="postEvent">If true, the event is propagated to callbacks.</param>
        public void SetDirty(AddressableAssetSettings.ModificationEvent modificationEvent, object eventData, bool postEvent)
        {
            m_CurrentHash = default;
            if (m_Settings != null)
                m_Settings.SetDirty(modificationEvent, eventData, postEvent, true);
        }

        internal bool ValidateNewVariableName(string name)
        {
            foreach (var idPair in profileEntryNames)
                if (idPair.ProfileName == name)
                    return false;
            return !string.IsNullOrEmpty(name) && !name.Any(c => { return c == '[' || c == ']' || c == '{' || c == '}'; });
        }

        /// <summary>
        /// Adds a new profile.
        /// </summary>
        /// <param name="name">The name of the new profile.</param>
        /// <param name="copyFromId">The id of the profile to copy values from.</param>
        /// <returns>The id of the created profile.</returns>
        public string AddProfile(string name, string copyFromId)
        {
            var existingProfile = GetProfileByName(name);
            if (existingProfile != null)
                return existingProfile.id;
            var copyRoot = GetProfile(copyFromId);
            if (copyRoot == null && m_Profiles.Count > 0)
                copyRoot = GetDefaultProfile();
            var prof = new BuildProfile(name, copyRoot, this);
            m_Profiles.Add(prof);
            SetDirty(AddressableAssetSettings.ModificationEvent.ProfileAdded, prof, true);
            ProfileWindow.MarkForReload();
            return prof.id;
        }

        // Allows passing in the profile directly for internal methods, makes the profile window's code a bit cleaner
        // Can't be public since BuildProfile is an internal class
        internal bool RenameProfile(BuildProfile profile, string newName)
        {
            if (profile == null)
            {
                Addressables.LogError("Profile rename failed because profile passed in is null");
                return false;
            }

            if (profile == GetDefaultProfile())
            {
                Addressables.LogError("Profile rename failed because default profile cannot be renamed.");
                return false;
            }

            if (profile.profileName == newName) return false;

            // new name cannot only contain spaces
            if (newName.Trim().Length == 0)
            {
                Addressables.LogError("Profile rename failed because new profile name must not be only spaces.");
                return false;
            }


            bool profileExistsInSettingsList = false;

            for (int i = 0; i < m_Profiles.Count; i++)
            {
                // return false if there already exists a profile with the new name, no duplicates are allowed
                if (m_Profiles[i].profileName == newName)
                {
                    Addressables.LogError("Profile rename failed because new profile name is not unique.");
                    return false;
                }

                if (m_Profiles[i].id == profile.id)
                {
                    profileExistsInSettingsList = true;
                }
            }

            // Rename the profile
            profile.profileName = newName;

            if (profileExistsInSettingsList)
                SetDirty(AddressableAssetSettings.ModificationEvent.ProfileModified, profile, true);

            ProfileWindow.MarkForReload();
            return true;
        }

        /// <summary>
        /// Renames a profile. profileId must refer to an existing profile. Profile names must be unique and must not be comprised of only whitespace.
        /// Returns false if profileId or newName is invalid.
        /// </summary>
        /// <param name="profileId"> The id of the profile to be renamed. </param>
        /// <param name="newName"> The new name to be given to the profile. </param>
        /// <returns> True if the rename is successful, false otherwise. </returns>
        public bool RenameProfile(string profileId, string newName)
        {
            var profileToRename = GetProfile(profileId);

            if (profileToRename == null)
            {
                Addressables.LogError("Profile rename failed because profile with sought id does not exist.");
                return false;
            }

            return RenameProfile(profileToRename, newName);
        }

        /// <summary>
        /// Removes a profile.
        /// </summary>
        /// <param name="profileId">The id of the profile to remove.</param>
        public void RemoveProfile(string profileId)
        {
            m_Profiles.RemoveAll(p => p.id == profileId);
            m_Profiles.ForEach(p =>
            {
                if (p.inheritedParent == profileId) p.inheritedParent = null;
            });
            SetDirty(AddressableAssetSettings.ModificationEvent.ProfileRemoved, profileId, true);
            ProfileWindow.MarkForReload();
        }

        BuildProfile GetProfileByName(string profileName)
        {
            return m_Profiles.Find(p => p.profileName == profileName);
        }

        internal string GetUniqueProfileName(string name)
        {
            return GenerateUniqueName(name, m_Profiles.Select(p => p.profileName));
        }

        internal BuildProfile GetProfile(string profileId)
        {
            return m_Profiles.Find(p => p.id == profileId);
        }

        internal string GetVariableId(string variableName)
        {
            foreach (var idPair in profileEntryNames)
            {
                if (idPair.ProfileName == variableName)
                    return idPair.Id;
            }

            return null;
        }

        /// <summary>
        /// Set the value of a variable for a specified profile.
        /// </summary>
        /// <param name="profileId">The profile id.</param>
        /// <param name="variableName">The property name.</param>
        /// <param name="val">The value to set the property.</param>
        public void SetValue(string profileId, string variableName, string val)
        {
            var profile = GetProfile(profileId);
            if (profile == null)
            {
                Addressables.LogError("setting variable " + variableName + " failed because profile " + profileId + " does not exist.");
                return;
            }

            var id = GetVariableId(variableName);
            if (string.IsNullOrEmpty(id))
            {
                Addressables.LogError("setting variable " + variableName + " failed because variable does not yet exist. Call CreateValue() first.");
                return;
            }

            profile.SetValueById(id, val);
            SetDirty(AddressableAssetSettings.ModificationEvent.ProfileModified, profile, true);
            ProfileWindow.MarkForReload();
        }

        internal string GetUniqueProfileEntryName(string name)
        {
            return GenerateUniqueName(name, profileEntryNames.Select(p => p.ProfileName));
        }

        /// <summary>
        /// Create a new profile property.
        /// </summary>
        /// <param name="variableName">The name of the property.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The id of the created variable.</returns>
        public string CreateValue(string variableName, string defaultValue)
        {
            return CreateValue(variableName, defaultValue, false);
        }

        internal string CreateValue(string variableName, string defaultValue, bool inline)
        {
            if (m_Profiles.Count == 0)
            {
                Addressables.LogError("Attempting to add a profile variable in Addressables, but there are no profiles yet.");
            }

            var id = GetVariableId(variableName);
            if (string.IsNullOrEmpty(id))
            {
                id = GUID.Generate().ToString();
                profileEntryNames.Add(new ProfileIdData(id, variableName, inline));

                foreach (var pro in m_Profiles)
                {
                    pro.values.Add(new BuildProfile.ProfileEntry(id, defaultValue));
                }
            }

            SetDirty(AddressableAssetSettings.ModificationEvent.ProfileModified, null, true);
            return id;
        }

        /// <summary>
        /// Remove a profile property.
        /// </summary>
        /// <param name="variableId">The id of the property.</param>
        public void RemoveValue(string variableId)
        {
            foreach (var pro in m_Profiles)
            {
                pro.values.RemoveAll(x => x.id == variableId);
            }

            m_ProfileEntryNames.RemoveAll(x => x.Id == variableId);
            SetDirty(AddressableAssetSettings.ModificationEvent.ProfileModified, null, false);
            ProfileWindow.MarkForReload();
        }

        /// <summary>
        /// Get the value of a property.
        /// </summary>
        /// <param name="profileId">The profile id.</param>
        /// <param name="varId">The property id.</param>
        /// <returns></returns>
        public string GetValueById(string profileId, string varId)
        {
            BuildProfile profile = GetProfile(profileId);
            return profile == null ? varId : profile.GetValueById(varId);
        }

        /// <summary>
        /// Get the value of a property by name.
        /// </summary>
        /// <param name="profileId">The profile id.</param>
        /// <param name="varName">The variable name.</param>
        /// <returns></returns>
        public string GetValueByName(string profileId, string varName)
        {
            return GetValueById(profileId, GetVariableId(varName));
        }

        internal static string GenerateUniqueName(string baseName, IEnumerable<string> enumerable)
        {
            var set = new HashSet<string>(enumerable);
            int counter = 1;
            var newName = baseName;
            while (set.Contains(newName))
            {
                newName = baseName + counter;
                counter++;
                if (counter == int.MaxValue)
                    throw new OverflowException();
            }

            return newName;
        }

        internal void CreateDuplicateVariableWithNewName(AddressableAssetSettings addressableAssetSettings, string newVariableName, string variableNameToCopyFrom)
        {
            var activeProfileId = addressableAssetSettings.activeProfileId;
            string newVarId = CreateValue(newVariableName, GetValueByName(activeProfileId, variableNameToCopyFrom));
            string oldVarId = GetVariableId(variableNameToCopyFrom);
            foreach (var profile in profiles)
            {
                profile.SetValueById(newVarId, profile.GetValueById(oldVarId));
                SetDirty(AddressableAssetSettings.ModificationEvent.ProfileModified, profile, true);
            }
        }
    }
}
