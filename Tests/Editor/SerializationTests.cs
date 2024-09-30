using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;
using Object = UnityEngine.Object;

namespace UnityEditor.AddressableAssets.Tests
{
    /*
     * This is a series of tests to verify that our serialization is deterministic and does not
     * trigger version control changes.
     *
     * If you get instabilities or one-off test failures, it's probably because something has
     * changed in serialization of the object in question and we're not sorting the output.
     *
     * This test is made to be unstable if there are changes that are not deterministic. So if
     * you see intermittent failures that's the sign there's a bug and should NOT be ignored.
     */
    public class SerializationTests : AddressableAssetTestBase
    {

        public string groupGuid = "422b6705-092c-4699-b57b-abfe7a6245d0";
        private System.Random m_Rnd;
        private int m_Seed = 0;
        private List<Type> m_SchemaTypes;
        private string m_PackagePath;

        private void Shuffle<T>(List<T> toShuffle)
        {
            toShuffle.Sort((x, y) => m_Rnd.Next() - m_Rnd.Next());
        }


        [OneTimeSetUp]
        public new void Init()
        {
            base.Init();
            m_Rnd = new System.Random(m_Seed);

            m_SchemaTypes = new List<Type>()
            {
                typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema)
            };
        }
        //
        [SetUp]
        public void Setup()
        {
            Settings.groups.Clear();
            // this lazy creates the default group
            var defaultGroup = Settings.DefaultGroup;
            // the way our package isolation tests work the tests are built into their own package
            // which means we have to load expected files from a different location
            m_PackagePath = AddressablesTestUtility.GetPackagePath() + "/Tests/Editor";
        }

        internal string GetExpectedPath(string filename)
        {
            return $"{m_PackagePath}/Expected/{filename}";
        }

        internal string GetFixturePath(string filename)
        {
            return $"{m_PackagePath}/Fixtures/{filename}";
        }

        [TestCase]
        public void TestAssetGroupSerialization()
        {
            var group = Settings.CreateGroup("testGroup", false, false, false,
                new List<AddressableAssetGroupSchema>(), m_SchemaTypes.ToArray());

            var labels = CreateAndShuffleLabels();
            AddAssetEntries(group, labels);
            EditorUtility.SetDirty(group);
            AssetDatabase.SaveAssets();

            var groupPath = AssetDatabase.GetAssetPath(group);
            RemapMetaGuids(group);
            AssetDatabase.Refresh();
            group = AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>(groupPath);
            Assert.AreEqual("16cd2736586abc441a3ef8bffa03b61f", group.Guid);
            Shuffle(group.m_SerializeEntries);
            Shuffle(group.Schemas);
            EditorUtility.SetDirty(group);
            AssetDatabase.SaveAssets();

            var expectedSerializedGroup = File.ReadAllText(GetExpectedPath("~SerializationTests_Group.unity"));
            var serializedGroup = File.ReadAllText(groupPath);
            AssertSerializedAreEqual(expectedSerializedGroup, serializedGroup);
        }

        private List<List<string>> CreateAndShuffleLabels()
        {
            var labels = new List<List<string>>
            {
                new List<string>() {"c", "a", "b"},
                new List<string>()  {"a5", "a2", "a"},
                new List<string>()  {"5", "22", "2"}
            };
            foreach (var label in labels)
            {
                Shuffle<string>(label);
            }

            return labels;
        }

        private void AddAssetEntries(AddressableAssetGroup group, List<List<string>> labels)
        {
            var entry1 = new AddressableAssetEntry("4df50598-ce2c-4265-a0f9-4e943a2991b0", "secondAsset", group, false);
            foreach (var label in labels[0])
            {
                entry1.SetLabel(label, true, false, false);
            }
            var entry2 = new AddressableAssetEntry("2269b1fb-67ee-4b32-a936-4647ff4c45b4", "firstAsset", group, false);
            foreach (var label in labels[1])
            {
                entry2.SetLabel(label, true, false, false);
            }
            var entry3 = new AddressableAssetEntry("9e86b64f-f58e-4d4f-aa9d-6e8be96505ec", "thirdAsset", group, false);
            foreach (var label in labels[2])
            {
                entry3.SetLabel(label, true, false, false);
            }
            group.AddAssetEntry(entry1);
            group.AddAssetEntry(entry2);
            group.AddAssetEntry(entry3);
        }

        [TestCase]
        public void TestAssetGroupTemplateSerialization()
        {
            Assert.True(Settings.CreateAndAddGroupTemplate("myTemplate", "my description\nwith carriage return", m_SchemaTypes.ToArray()));
            var newAssetGroupTemplate = Settings.GetGroupTemplateObject(Settings.GroupTemplateObjects.Count - 1) as AddressableAssetGroupTemplate;
            Assert.NotNull(newAssetGroupTemplate);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(newAssetGroupTemplate as ScriptableObject, out string guid, out long templateFileId);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(newAssetGroupTemplate.GetSchemaByType(typeof(ContentUpdateGroupSchema)), out string cugsguid, out long cugsFileId);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(newAssetGroupTemplate.GetSchemaByType(typeof(BundledAssetGroupSchema)), out string bagsguid, out long bagsFileId);

            var monoBehaviorMap = new Dictionary<string, string>()
            {
                {bagsFileId.ToString(), "-6794523166426839361"},
                {cugsFileId.ToString(), "-1107740541918034454"},
                {templateFileId.ToString(), "11400000"},
            };

            RemapMetaGuids(null, monoBehaviorMap);
            AssetDatabase.Refresh();
            Shuffle(newAssetGroupTemplate.SchemaObjects);
            EditorUtility.SetDirty(newAssetGroupTemplate);
            AssetDatabase.SaveAssets();

            var expectedSerializedTemplate = File.ReadAllText(GetExpectedPath("~SerializationTests_GroupTemplate.unity"));
            var serializedTemplate = File.ReadAllText(AssetDatabase.GetAssetPath(newAssetGroupTemplate));
            AssertSerializedAreEqual(serializedTemplate, expectedSerializedTemplate);
        }

        [TestCase]
        public void TestProfileDataSourceSettingsSerialization()
        {
            var profileDataSourceSettings = ProfileDataSourceSettings.Create(ConfigFolder, "ProfileDataSourceSettings");
            // another profile is added when CCD_ENABLED is defined. We remove that to keep the test consistent.
            DeleteCcdProfile(profileDataSourceSettings);
            AddProfileGroupTypes(profileDataSourceSettings);
            AddEnvironments(profileDataSourceSettings);
            AssetDatabase.SaveAssets();

            RemapMetaGuids(null);
            AssetDatabase.Refresh();

            // shuffle
            Shuffle(profileDataSourceSettings.profileGroupTypes);

            foreach (var groupType in profileDataSourceSettings.profileGroupTypes)
            {
                Shuffle(groupType.Variables);
            }
            Shuffle(profileDataSourceSettings.environments);
            AssetDatabase.SaveAssets();

            var expectedProfileDataSourceSettings = File.ReadAllText(GetExpectedPath("~SerializationTests_ProfileDataSourceSettings.unity"));
            var serializedProfileDataSourceSettings = File.ReadAllText(AssetDatabase.GetAssetPath(profileDataSourceSettings));
            AssertSerializedAreEqual(remapExpectedString(expectedProfileDataSourceSettings), serializedProfileDataSourceSettings);
        }

        private void AddProfileGroupTypes(ProfileDataSourceSettings profileDataSourceSettings)
        {
            ProfileGroupType profileGroupType = new ProfileGroupType("testPrefix");
            profileGroupType.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kBuildPath, "Build/"));
            profileGroupType.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kLoadPath, "https://example.com/a/"));
            profileGroupType.AddVariable(new ProfileGroupType.GroupTypeVariable(ProfileDataSourceSettings.ENVIRONMENT_NAME, "production"));
            profileDataSourceSettings.profileGroupTypes.Add(profileGroupType);
        }

        private void DeleteCcdProfile(ProfileDataSourceSettings profileDataSourceSettings)
        {
            var toDelete = profileDataSourceSettings.profileGroupTypes.Find((x) => x.GroupTypePrefix == "Automatic");
            if (toDelete!= null)
            {
                profileDataSourceSettings.profileGroupTypes.Remove(toDelete);
            }
        }

        private void AddEnvironments(ProfileDataSourceSettings profileDataSourceSettings)
        {
            profileDataSourceSettings.environments = new List<ProfileDataSourceSettings.Environment>
            {
                new ProfileDataSourceSettings.Environment() {name = "production", id = "0214d8a4-af63-4534-814f-431d180926d6"},
                new ProfileDataSourceSettings.Environment() {name = "staging", id = "7c9f9aeb-2b9d-4258-bf52-0d2b6118ac39"},
                new ProfileDataSourceSettings.Environment() {name = "development", id = "a9ec150c-9a63-46ab-9a21-a3434afc7ab3"}
            };
        }

        [TestCase]
        public void TestAddressableAssetSettingsSerialization()
        {
            var group = Settings.CreateGroup("testGroup", false, false, false,
                new List<AddressableAssetGroupSchema>(), m_SchemaTypes.ToArray());
            Settings.DefaultGroup = Settings.groups.Find((g) => g.Default);
            Settings.NonRecursiveBuilding = true;
            Settings.ContiguousBundles = true;

            AddProfile();
            AddInitializationObjects();
            AssetDatabase.SaveAssets();
            RemapMetaGuids(group);
            AssetDatabase.Refresh();

            // shuffle
            foreach (var profile in Settings.profileSettings.profiles)
            {
                Shuffle(profile.values);
            }
            Shuffle(Settings.profileSettings.profiles);
            Shuffle(Settings.profileSettings.profileEntryNames);
            Shuffle(Settings.groups);

            EditorUtility.SetDirty(Settings);
            AssetDatabase.SaveAssets();

#if CCD_3_OR_NEWER
            var expectedSerializedSettings = File.ReadAllText(GetExpectedPath("~SerializationTests_AddressableAssetSettings.ccd3.unity"));
#elif ENABLE_CCD
            var expectedSerializedSettings = File.ReadAllText(GetExpectedPath("~SerializationTests_AddressableAssetSettings.ccd2.unity"));
#else
            var expectedSerializedSettings = File.ReadAllText(GetExpectedPath("~SerializationTests_AddressableAssetSettings.unity"));
#endif
            var serializedSettings = File.ReadAllText(AssetDatabase.GetAssetPath(Settings));
            AssertSerializedAreEqual(remapExpectedString(expectedSerializedSettings), serializedSettings);
        }

        private void AddProfile()
        {
            // ok so we need to add a profile here
            Settings.profileSettings.AddProfile("testProfile", null);
        }

        private void AddInitializationObjects()
        {
            Settings.AddInitializationObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(GetFixturePath("InitFixture1.asset")) as IObjectInitializationDataProvider);
            Settings.AddInitializationObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(GetFixturePath("InitFixture2.asset")) as IObjectInitializationDataProvider);
        }

        private void AddProfileValueMappings(Dictionary<string, string> mappings)
        {
            foreach (var profile in Settings.profileSettings.profiles)
            {
                foreach (var value in profile.values)
                {
                    // this emulates TryAdd logic
                    if (mappings.ContainsKey(value.id))
                        continue;
                    switch (value.value)
                    {
                        case "[UnityEditor.EditorUserBuildSettings.activeBuildTarget]":
                            mappings.Add(value.id, "0507cc90998e0a04f94da7055d0cc638");
                            break;
                        case "[UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]":
                            mappings.Add(value.id, "8f726afcd2923be469c05fdfc9963d44");
                            break;
                        case "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/[BuildTarget]":
                            mappings.Add(value.id, "44693b9e8b6e8ab4e8bada109cd9e70d");
                            break;
                        case "ServerData/[BuildTarget]":
                            mappings.Add(value.id, "0d5680944a6e4bb47a35cd423b95cb36");
                            break;
                        case "http://[PrivateIpAddress]:[HostingServicePort]":
                            mappings.Add(value.id, "42ec52cd576b8b9439d83010f809d5b5");
                            break;
                        default:
                            throw new Exception($"unknown value in profile settings {value.value}");
                    }
                }
            }

        }

        private void AddProfileEntryMappings(Dictionary<string, string> mappings)
        {
            foreach (var profileEntryName in Settings.profileSettings.profileEntryNames)
            {
                // this emulates TryAdd logic
                if (mappings.ContainsKey(profileEntryName.m_Id))
                    continue;
                switch (profileEntryName.ProfileName)
                {
                    case "BuildTarget":
                        mappings.Add(profileEntryName.m_Id, "0507cc90998e0a04f94da7055d0cc638");
                        break;
                    case "Local.BuildPath":
                        mappings.Add(profileEntryName.m_Id, "8f726afcd2923be469c05fdfc9963d44");
                        break;
                    case "Local.LoadPath":
                        mappings.Add(profileEntryName.m_Id, "44693b9e8b6e8ab4e8bada109cd9e70d");
                        break;
                    case "Remote.BuildPath":
                        mappings.Add(profileEntryName.m_Id, "0d5680944a6e4bb47a35cd423b95cb36");
                        break;
                    case "Remote.LoadPath":
                        mappings.Add(profileEntryName.m_Id, "42ec52cd576b8b9439d83010f809d5b5");
                        break;
                    default:
                        throw new Exception($"unknown value in profile entry names {profileEntryName.ProfileName}");
                }
            }
        }

        private void AddEscapedValues(Dictionary<string, string> mappings)
        {
#if UNITY_2023_1_OR_NEWER
            // in 2023.1+ we escape text fields that in in a square brace
            // https://github.cds.internal.unity3d.com/unity/unity/commit/7eedeea6a800cbe1c7ef8b956acd32f17eb96333
            mappings.Add("ServerData/[BuildTarget]", "'ServerData/[BuildTarget]'");
            mappings.Add("http://[PrivateIpAddress]:[HostingServicePort]", "'http://[PrivateIpAddress]:[HostingServicePort]'");
#endif
        }

        private void RemapMetaGuids(AddressableAssetGroup group)
        {
            var remapped = new Dictionary<string, string>();
            RemapMetaGuids(group, remapped);
        }

        // this remaps GUIDs from the current values to the saved values in our expected files. So for instance we map the
        // current default group guid to 2308cd47506141c4aae9737b7d567105 which is what is contained in
        // ~SerializationTests_AddressableAssetSettings.unity
        private void RemapMetaGuids(AddressableAssetGroup group, Dictionary<string, string> remapped)
        {
            // we should be order by GUID
            var defaultGroup = Settings.groups.Find((v) => v.Default);
            // var secondGroup = Settings.groups.Find((v) => !v.Default);
            var buildScriptFast = Settings.DataBuilders.Find((b) => b.name == "BuildScriptFastMode");
            var buildScriptPackedPlay = Settings.DataBuilders.Find((b) => b.name == "BuildScriptPackedPlayMode");
            var buildScriptPacked = Settings.DataBuilders.Find((b) => b.name == "BuildScriptPackedMode");
            var buildScriptVirtual = Settings.DataBuilders.Find((b) => b.name == "BuildScriptVirtualMode");
            var defaultProfile = Settings.profileSettings.profiles.Find((p) => p.profileName == "Default");
            var secondProfile = Settings.profileSettings.profiles.Find((p) => p.profileName != "Default");

            // this mapping is from the GUID in the file (ex. ~testAddressableAssetSettings.unity) to the currently in use guid
            // we replace all the current guids with our static guids so that we can compare the sorting
            remapped.Add(GetMetaGuidFromObject(Settings), "3bf47571d203fa84d8dd31832e7c9339");
            remapped.Add(GetMetaGuidFromObject(buildScriptFast), "271b00b9e756a6d448f4ae7a08b88509");
            remapped.Add(GetMetaGuidFromObject(buildScriptPacked), "1694decfa7f2ffd4983ca3978e171998");
            remapped.Add(GetMetaGuidFromObject(buildScriptVirtual), "bde352c6f43e40c88c6e408f188a954d");
            remapped.Add(GetMetaGuidFromObject(buildScriptPackedPlay), "533ad9bddde5e2540a2a76c0203d0acb");

            if (Settings?.DefaultGroup?.Guid != null) {
                remapped.Add(Settings.DefaultGroup.Guid, "73831a73d82c83d4183d7e0477f7e745");
            }
            if (defaultGroup != null) {
                remapped.Add(GetMetaGuidFromObject(defaultGroup), "2308cd47506141c4aae9737b7d567105");
            }
            if (Settings.GroupTemplateObjects.Count > 0) {
                remapped.Add(GetMetaGuidFromObject(Settings.GroupTemplateObjects[0]), "97a492a095c0434448524d71cc7f0b0d");
            }
            if (group != null) {
                remapped.Add(group.Guid, "16cd2736586abc441a3ef8bffa03b61f");
                remapped.Add(GetMetaGuidFromObject(group), "e3940d5982f85734ca7aec5a9b7a90ee");
                remapped.Add(GetMetaGuidFromObject(group.Schemas[0]), "798716054e8a18a479c179e6d6f5ad2d");
                remapped.Add(GetMetaGuidFromObject(group.Schemas[1]), "7991916e228786548a8c905c2235f71f");
            }
            if (defaultProfile != null) {
                remapped.Add(defaultProfile.id, "5550bbbe2a7ee8c4f9d4600df43218be");
            }
            if (secondProfile != null) {
                remapped.Add(secondProfile.id, "c901f922cc200454b815a5b33a8427c6");
            }
            AddProfileValueMappings(remapped);
            AddProfileEntryMappings(remapped);
            RemapFiles(remapped, ConfigFolder);

            // clear any caches
            Settings.groups.Clear(); // this should be repopulated on AssetDatabase.Refresh()
            Settings.ClearFindAssetEntryCache();
        }

        private void RemapFiles(Dictionary<string, string> mappings, string dirName)
        {
            foreach (string dir in Directory.EnumerateDirectories(dirName))
            {
                RemapFiles(mappings, dir);
            }

            foreach (string file in Directory.EnumerateFiles(dirName))
            {
                var inFile = file;
                var outFile = $"{file}.tmp";
                var reader = new StreamReader(inFile);
                var writer = new StreamWriter(outFile);
                RemapStream(mappings, reader, writer);
                reader.Close();
                writer.Close();
                File.Delete(inFile);
                File.Move(outFile, inFile);
            }
        }

        private void RemapStream(Dictionary<string, string> mappings, TextReader reader, TextWriter writer)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                foreach (var pair in mappings)
                {
                    if (line.Contains(pair.Key))
                    {
                        line = line.Replace(pair.Key, pair.Value);
                    }
                }
                writer.WriteLine(line);
            }
        }

        // this allows us to find a yaml key in the stream and cut out a certian number of lines including
        // the key. We can't parse the yaml because we don't want to serialize it again.
        private void RemoveKeys(Dictionary<string, int> mappings, TextReader reader, TextWriter writer)
        {
            string line;
            bool inDelete = false;
            int toDelete = 0;
            while ((line = reader.ReadLine()) != null)
            {
                if (inDelete && toDelete == 0)
                    inDelete = false;
                if (inDelete && toDelete > 0)
                {
                    toDelete = toDelete - 1;
                    continue;
                }


                foreach (var pair in mappings)
                {

                    if (line.Contains(pair.Key))
                    {
                        inDelete = true;
                        toDelete = pair.Value;
                        break;
                    }
                }

                if (!inDelete)
                {
                    writer.WriteLine(line);
                }
            }
        }

        // this is for remapping things that change in serialization between Unity version. Since we
        // are changing guids, saving the files, and then refreshing them we cannot change how
        // values are serialized in our main remapping code.
        private String remapExpectedString(string input)
        {
            var remapped = new Dictionary<string, string>();
            AddEscapedValues(remapped);
            var remappedOutput = new StringWriter();
            RemapStream(remapped, new StringReader(input), remappedOutput);
            var output = new StringWriter();
#if UNITY_2021_1_OR_NEWER
            var removedMap = new Dictionary<string, int> {};
#else
            var removedMap = new Dictionary<string, int> { { "m_currentHash", 2 } };
#endif
            RemoveKeys(removedMap, new StringReader(remappedOutput.ToString()), output);
            return output.ToString();
        }

        private string GetMetaGuidFromObject(Object obj)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long templateFileId);
            return guid;
        }

        private void AssertSerializedAreEqual(string expected, string actual)
        {
            expected = expected.Replace("\r\n", "\n");
            actual = actual.Replace("\r\n", "\n");
            Assert.AreEqual(expected, actual);
        }
    }
}
