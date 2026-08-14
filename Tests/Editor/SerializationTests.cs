using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets.Tests
{
    /*
     * SERIALIZATION DETERMINISM — how these tests work
     *
     * Goal: Addressables should serialize collections (profiles, groups, labels, etc.) in a
     * stable order so Unity saves predictable asset YAML. Verification uses self-consistency:
     *
     *   1) Baseline repeat-save: save the asset twice without shuffling collections; both serialized texts must match.
     *      Catches nondeterministic YAML unrelated to list order (complements shuffle checks below).
     *   2) Build a realistic asset (groups, profiles, environments, …).
     *   3) Shuffle in-memory lists with Fisher–Yates using System.Random (pass A).
     *   4) Save → read the full asset file text as T1.
     *   5) Reload the asset from disk so memory matches what was written.
     *   6) Shuffle again with a different seed (pass B).
     *   7) Save → read file text as T2.
     *   8) Assert T1 == T2 (after normalizing line endings).
     *
     * If serialization is deterministic, both shuffles should collapse to the same canonical
     * YAML regardless of input order. Pass A and B share the same asset GUIDs and IDs on disk;
     * only list order before save differs.
     *
     * Random seeds: each test run picks two distinct seeds (unless you set the reproduction
     * static fields below). That exercises different permutations over CI time while keeping
     * failures reproducible via the seeds printed in the assertion message.
     *
     * Intermittent failures can mean nondeterministic serialization (bug). Do not ignore them.
     */
    public class SerializationTests : AddressableAssetTestBase
    {
        /// <summary>
        /// When both this and <see cref="ReproduceDeterminismShuffleSeedPassB"/> are set, determinism tests use these
        /// shuffle seeds instead of random values. Use the values from a failed assertion message to reproduce locally.
        /// Set both back to null when finished debugging.
        /// </summary>
        public static int? ReproduceDeterminismShuffleSeedPassA;

        /// <summary>
        /// Pair with <see cref="ReproduceDeterminismShuffleSeedPassA"/>; see that property for usage.
        /// </summary>
        public static int? ReproduceDeterminismShuffleSeedPassB;

        /// <summary>
        /// Shared RNG for label shuffling during setup (<see cref="CreateAndShuffleLabels"/>) and for
        /// determinism shuffles after we assign <see cref="m_Rnd"/> from pass A / pass B seeds.
        /// </summary>
        private System.Random m_Rnd;

        /// <summary>
        /// Seed for setup-only shuffling (labels). Determinism passes use seeds from <see cref="GetDeterminismShuffleSeeds"/>.
        /// </summary>
        private int m_Seed = 0;

        private List<Type> m_SchemaTypes;

        /// <summary>
        /// Permutes list order in place with Fisher–Yates, using <see cref="m_Rnd"/>. Simulates arbitrary list order
        /// before save without relying on a non-transitive <c>Sort</c> comparer.
        /// </summary>
        private void Shuffle<T>(List<T> toShuffle)
        {
            for (int i = toShuffle.Count - 1; i > 0; i--)
            {
                int j = m_Rnd.Next(i + 1);
                T temp = toShuffle[i];
                toShuffle[i] = toShuffle[j];
                toShuffle[j] = temp;
            }
        }

        /// <summary>
        /// Shuffles every mutable collection on <see cref="AddressableAssetSettings"/> that must serialize in a
        /// canonical order (profiles, profile variable rows, profile entry metadata, groups).
        /// </summary>
        private void ShuffleAddressableAssetSettingsCollections()
        {
            foreach (var profile in Settings.profileSettings.profiles)
            {
                Shuffle(profile.values);
            }
            Shuffle(Settings.profileSettings.profiles);
            Shuffle(Settings.profileSettings.profileEntryNames);
            Shuffle(Settings.groups);
        }

        /// <summary>
        /// Shuffles serialized entries and schema list on a group — both must end up in deterministic order on save.
        /// </summary>
        private void ShuffleGroupCollections(AddressableAssetGroup group)
        {
            Shuffle(group.m_SerializeEntries);
            Shuffle(group.Schemas);
        }

        /// <summary>
        /// Shuffles schema objects on a group template (order must not affect final YAML once canonical sorting runs).
        /// </summary>
        private void ShuffleGroupTemplateSchemas(AddressableAssetGroupTemplate template)
        {
            Shuffle(template.SchemaObjects);
        }

        /// <summary>
        /// Shuffles profile group types, their variables, and environments — all lists that must serialize deterministically.
        /// </summary>
        private void ShuffleProfileDataSourceSettingsCollections(ProfileDataSourceSettings profileDataSourceSettings)
        {
            Shuffle(profileDataSourceSettings.profileGroupTypes);
            foreach (var groupType in profileDataSourceSettings.profileGroupTypes)
            {
                Shuffle(groupType.Variables);
            }
            Shuffle(profileDataSourceSettings.environments);
        }

        /// <summary>
        /// Reimports the asset from disk, then returns <see cref="AssetDatabase.LoadAssetAtPath{T}"/>.
        /// Unity normally keeps one loaded <see cref="ScriptableObject"/> instance per asset path;
        /// <see cref="AssetDatabase.ImportAsset"/> with <see cref="ImportAssetOptions.ForceUpdate"/> reapplies serialized
        /// state onto that instance after pass A&apos;s save. Pass B must not reuse stale list order without this reload.
        /// </summary>
        private static T ReloadScriptableFromPath<T>(string assetPath) where T : ScriptableObject
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }

        /// <summary>
        /// Produces two different integer seeds for pass A and pass B. If both reproduction properties are set,
        /// returns those (so you can replay a failing CI run). Otherwise draws random seeds so different runs
        /// stress different permutations; seeds are always unequal so pass B is not a no-op.
        /// </summary>
        private static void GetDeterminismShuffleSeeds(out int seedPassA, out int seedPassB)
        {
            if (ReproduceDeterminismShuffleSeedPassA.HasValue && ReproduceDeterminismShuffleSeedPassB.HasValue)
            {
                seedPassA = ReproduceDeterminismShuffleSeedPassA.Value;
                seedPassB = ReproduceDeterminismShuffleSeedPassB.Value;
                return;
            }

            // Mix several sources so automated runs on the same machine still vary seeds across test fixtures.
            var entropy = unchecked(Environment.TickCount ^ (int)DateTime.UtcNow.Ticks ^ Guid.NewGuid().GetHashCode());
            var rng = new System.Random(entropy);
            seedPassA = rng.Next();
            seedPassB = rng.Next();
            while (seedPassB == seedPassA)
            {
                seedPassB = rng.Next();
            }
        }

        /// <summary>
        /// Embeds instructions in assertion failures so developers can plug seeds into <see cref="ReproduceDeterminismShuffleSeedPassA"/> /
        /// <see cref="ReproduceDeterminismShuffleSeedPassB"/> and rerun one test.
        /// </summary>
        private static string BuildDeterminismReproductionHint(int seedPassA, int seedPassB)
        {
            return "To reproduce: assign both static seeds (then rerun only the failing test), for example at the top of that test method:\n" +
                $"  SerializationTests.ReproduceDeterminismShuffleSeedPassA = {seedPassA};\n" +
                $"  SerializationTests.ReproduceDeterminismShuffleSeedPassB = {seedPassB};\n" +
                "Clear both fields to null when finished. File: Packages/com.unity.addressables/Tests/Editor/SerializationTests.cs";
        }

        /// <summary>
        /// Compares two full YAML/text snapshots for byte equality (after newline normalization). They should match
        /// if serialization order is independent of how we shuffled lists beforehand.
        /// </summary>
        private void AssertDeterministicSerializationEqual(string serializedPassA, string serializedPassB, string summary,
            int seedPassA, int seedPassB)
        {
            var msg = $"{summary} Shuffle seeds — passA: {seedPassA}, passB: {seedPassB}. {BuildDeterminismReproductionHint(seedPassA, seedPassB)}";
            AssertSerializedAreEqual(serializedPassA, serializedPassB, msg);
        }

        /// <summary>
        /// Baseline repeat-save: save twice without reordering serialized collections and compare YAML text.
        /// If snapshots differ, serialization is unstable without any shuffle (distinct from shuffle determinism checks).
        /// </summary>
        private void AssertBaselineRepeatSaveSameSerializedText(string assetPath, UnityEngine.Object asset)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            var textAfterFirstSave = File.ReadAllText(assetPath);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            var textAfterSecondSave = File.ReadAllText(assetPath);

            AssertSerializedAreEqual(textAfterFirstSave, textAfterSecondSave,
                "Baseline repeat-save failed: two consecutive saves without shuffling collections should produce " +
                "identical serialized asset text.");
        }

        [OneTimeSetUp]
        public new void Init()
        {
            base.Init();
            m_Rnd = new System.Random(m_Seed);

            m_SchemaTypes = new ()
            {
                typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema)
            };
        }
        [SetUp]
        public void Setup()
        {
            // Ensure each test starts from a known settings shape; accessing DefaultGroup recreates default group if needed.
            Settings.groups.Clear();
            Settings.GroupTemplateObjects.Clear();
            var defaultGroup = Settings.DefaultGroup;
        }

        /// <summary>
        /// End-to-end on an <see cref="AddressableAssetGroup"/>: entries, schemas, and entry labels are populated so we
        /// verify sorting for all of those serialized lists.
        /// </summary>
        [TestCase]
        public void TestAssetGroupSerialization()
        {
            var group = Settings.CreateGroup("testGroup", false, false, false,
                new List<AddressableAssetGroupSchema>(), m_SchemaTypes.ToArray());

            var labels = CreateAndShuffleLabels();
            AddAssetEntries(group, labels);
            EditorUtility.SetDirty(group);
            AssetDatabase.SaveAssetIfDirty(group);
            AssetDatabase.SaveAssetIfDirty(Settings);

            var groupPath = AssetDatabase.GetAssetPath(group);
            AssetDatabase.Refresh();
            group = AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>(groupPath);
            Assert.IsFalse(string.IsNullOrEmpty(group.Guid)); // group loaded correctly from disk after Refresh

            AssertBaselineRepeatSaveSameSerializedText(groupPath, group);

            // Pass A: shuffle with seed A → YAML snapshot T1
            GetDeterminismShuffleSeeds(out int seedPassA, out int seedPassB);
            m_Rnd = new System.Random(seedPassA);
            ShuffleGroupCollections(group);
            EditorUtility.SetDirty(group);
            AssetDatabase.SaveAssetIfDirty(group);
            var serializedPassA = File.ReadAllText(groupPath);

            // Reload so pass B starts from disk state, not stale list order in memory
            group = ReloadScriptableFromPath<AddressableAssetGroup>(groupPath);

            // Pass B: different shuffle seed → YAML snapshot T2 (must equal T1 if serialization is deterministic)
            m_Rnd = new System.Random(seedPassB);
            ShuffleGroupCollections(group);
            EditorUtility.SetDirty(group);
            AssetDatabase.SaveAssetIfDirty(group);
            var serializedPassB = File.ReadAllText(groupPath);

            AssertDeterministicSerializationEqual(serializedPassA, serializedPassB,
                "determinism: AddressableAssetGroup should serialize identically after two independent shuffles.",
                seedPassA, seedPassB);
        }

        /// <summary>
        /// Builds label sets for three entries and shuffles each set so label *order* on each entry is nondeterministic
        /// going into save — serialization should still emit labels in canonical order per Addressables rules.
        /// </summary>
        private List<List<string>> CreateAndShuffleLabels()
        {
            var labels = new List<List<string>>
            {
                new() {"c", "a", "b"},
                new()  {"a5", "a2", "a"},
                new()  {"5", "22", "2"}
            };
            foreach (var label in labels)
            {
                Shuffle<string>(label);
            }

            return labels;
        }

        /// <summary>
        /// Adds three entries with distinct addresses and synthetic asset GUIDs (stable fake identities).
        /// Labels come from <see cref="CreateAndShuffleLabels"/>.
        /// </summary>
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

        /// <summary>
        /// Group template schema object order must serialize deterministically; template description includes newlines on purpose.
        /// </summary>
        [TestCase]
        public void TestAssetGroupTemplateSerialization()
        {
            var newAssetGroupTemplate = Settings.CreateAndAddGroupTemplateInternal("myTemplate", "my description\nwith carriage return", m_SchemaTypes.ToArray());
            var assetPath = AssetDatabase.GetAssetPath(newAssetGroupTemplate);
            EditorUtility.SetDirty(newAssetGroupTemplate);
            AssetDatabase.SaveAssetIfDirty(newAssetGroupTemplate);
            AssetDatabase.SaveAssetIfDirty(Settings);
            AssetDatabase.Refresh();

            var template = AssetDatabase.LoadAssetAtPath<AddressableAssetGroupTemplate>(assetPath);

            AssertBaselineRepeatSaveSameSerializedText(assetPath, template);

            GetDeterminismShuffleSeeds(out int seedPassA, out int seedPassB);
            // Pass A / pass B — same pattern as TestAssetGroupSerialization (see comments there).
            m_Rnd = new System.Random(seedPassA);
            ShuffleGroupTemplateSchemas(template);
            EditorUtility.SetDirty(template);
            AssetDatabase.SaveAssetIfDirty(template);
            var serializedPassA = File.ReadAllText(assetPath);

            template = ReloadScriptableFromPath<AddressableAssetGroupTemplate>(assetPath);

            m_Rnd = new System.Random(seedPassB);
            ShuffleGroupTemplateSchemas(template);
            EditorUtility.SetDirty(template);
            AssetDatabase.SaveAssetIfDirty(template);
            var serializedPassB = File.ReadAllText(assetPath);

            AssertDeterministicSerializationEqual(serializedPassA, serializedPassB,
                "determinism: AddressableAssetGroupTemplate should serialize identically after two independent shuffles.",
                seedPassA, seedPassB);
        }

        /// <summary>
        /// Profile data source settings contain multiple list types (group types, variables per type, environments).
        /// We add several environments and a custom profile group type so shuffling has real material to reorder — the test
        /// verifies that sorted output does not depend on insertion order.
        /// </summary>
        [TestCase]
        public void TestProfileDataSourceSettingsSerialization()
        {
            var profileDataSourceSettings = ProfileDataSourceSettings.Create(ConfigFolder, "ProfileDataSourceSettings");
            // When CCD is enabled, an extra automatic profile group type appears; remove it so pass A/B compare like-for-like.
            DeleteCcdProfile(profileDataSourceSettings);
            AddProfileGroupTypes(profileDataSourceSettings);
            AddEnvironments(profileDataSourceSettings);
            AssetDatabase.SaveAssetIfDirty(profileDataSourceSettings);

            AssetDatabase.Refresh();

            string assetPath = AssetDatabase.GetAssetPath(profileDataSourceSettings);

            AssertBaselineRepeatSaveSameSerializedText(assetPath, profileDataSourceSettings);

            GetDeterminismShuffleSeeds(out int seedPassA, out int seedPassB);
            // Pass A / B: environments + profileGroupTypes + nested Variables lists must converge to identical YAML.
            m_Rnd = new System.Random(seedPassA);
            ShuffleProfileDataSourceSettingsCollections(profileDataSourceSettings);
            AssetDatabase.SaveAssetIfDirty(profileDataSourceSettings);
            var serializedPassA = File.ReadAllText(assetPath);

            profileDataSourceSettings = ReloadScriptableFromPath<ProfileDataSourceSettings>(assetPath);

            m_Rnd = new System.Random(seedPassB);
            ShuffleProfileDataSourceSettingsCollections(profileDataSourceSettings);
            AssetDatabase.SaveAssetIfDirty(profileDataSourceSettings);
            var serializedPassB = File.ReadAllText(assetPath);

            AssertDeterministicSerializationEqual(serializedPassA, serializedPassB,
                "determinism: ProfileDataSourceSettings should serialize identically after two independent shuffles.",
                seedPassA, seedPassB);
        }

        /// <summary>
        /// Adds one custom prefix with multiple variables so <see cref="ShuffleProfileDataSourceSettingsCollections"/> can
        /// reorder both the group-type list and nested variable lists — exercises sorting at multiple depths.
        /// </summary>
        private void AddProfileGroupTypes(ProfileDataSourceSettings profileDataSourceSettings)
        {
            ProfileGroupType profileGroupType = new ProfileGroupType("testPrefix");
            profileGroupType.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kBuildPath, "Build/"));
            profileGroupType.AddVariable(new ProfileGroupType.GroupTypeVariable(AddressableAssetSettings.kLoadPath, "https://example.com/a/"));
            profileGroupType.AddVariable(new ProfileGroupType.GroupTypeVariable(ProfileDataSourceSettings.ENVIRONMENT_NAME, "production"));
            profileDataSourceSettings.profileGroupTypes.Add(profileGroupType);
        }

        /// <summary>
        /// Removes the CCD "Automatic" profile group type when present so the serialized asset matches non-CCD layouts.
        /// </summary>
        private void DeleteCcdProfile(ProfileDataSourceSettings profileDataSourceSettings)
        {
            var toDelete = profileDataSourceSettings.profileGroupTypes.Find((x) => x.GroupTypePrefix == "Automatic");
            if (toDelete!= null)
            {
                profileDataSourceSettings.profileGroupTypes.Remove(toDelete);
            }
        }

        /// <summary>
        /// Populates the environments list with fixed name/id pairs so we have multiple rows to shuffle. The GUIDs are
        /// arbitrary stable test data; self-consistency (T1 vs T2) does not require specific values, only multiple items.
        /// </summary>
        private void AddEnvironments(ProfileDataSourceSettings profileDataSourceSettings)
        {
            profileDataSourceSettings.environments = new List<ProfileDataSourceSettings.Environment>
            {
                new() {name = "production", id = "0214d8a4-af63-4534-814f-431d180926d6"},
                new() {name = "staging", id = "7c9f9aeb-2b9d-4258-bf52-0d2b6118ac39"},
                new() {name = "development", id = "a9ec150c-9a63-46ab-9a21-a3434afc7ab3"}
            };
        }

        /// <summary>
        /// Largest scenario: extra group, added profile, initialization objects from fixtures, then shuffle of profiles
        /// (values, profile list, profile entry names) and groups. Uses <see cref="ReloadSettingsAssetFromDisk"/> between
        /// passes because settings are held via the test base <see cref="AddressableAssetTestBase.Settings"/> property.
        /// </summary>
        [TestCase]
        public void TestAddressableAssetSettingsSerialization()
        {
            var group = Settings.CreateGroup("testGroup", false, false, false,
                new List<AddressableAssetGroupSchema>(), m_SchemaTypes.ToArray());
            Settings.DefaultGroup = Settings.groups.Find((g) => g.Default);
            Settings.ContentDirectoryGroupTemplateCreated = false;
            Settings.Validate();

            AddProfile();
            AddInitializationObjects();
            AssetDatabase.SaveAssetIfDirty(Settings);
            AssetDatabase.Refresh();

            AssertBaselineRepeatSaveSameSerializedText(AssetDatabase.GetAssetPath(Settings), Settings);

            GetDeterminismShuffleSeeds(out int seedPassA, out int seedPassB);
            // Pass A: shuffle settings collections → read full settings .asset as T1
            m_Rnd = new System.Random(seedPassA);
            ShuffleAddressableAssetSettingsCollections();

            EditorUtility.SetDirty(Settings);
            AssetDatabase.SaveAssetIfDirty(Settings);
            var serializedPassA = File.ReadAllText(AssetDatabase.GetAssetPath(Settings));

            // Replace base Settings instance so pass B applies to the same on-disk asset as pass A
            ReloadSettingsAssetFromDisk();

            // Pass B: second shuffle → T2 must equal T1
            m_Rnd = new System.Random(seedPassB);
            ShuffleAddressableAssetSettingsCollections();

            EditorUtility.SetDirty(Settings);
            AssetDatabase.SaveAssetIfDirty(Settings);
            var serializedPassB = File.ReadAllText(AssetDatabase.GetAssetPath(Settings));

            AssertDeterministicSerializationEqual(serializedPassA, serializedPassB,
                "determinism: AddressableAssetSettings should serialize identically after two independent shuffles.",
                seedPassA, seedPassB);
        }

        /// <summary>
        /// Adds a second named profile so the profiles collection has more than one row to shuffle and serialize.
        /// </summary>
        private void AddProfile()
        {
            Settings.profileSettings.AddProfile("testProfile", null);
        }

        /// <summary>
        /// Registers init providers from packaged fixture assets — ensures initialization object ordering is covered too.
        /// </summary>
        private void AddInitializationObjects()
        {
            Settings.AddInitializationObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(GetFixturePath("InitFixture1.asset")) as IObjectInitializationDataProvider);
            Settings.AddInitializationObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(GetFixturePath("InitFixture2.asset")) as IObjectInitializationDataProvider);
        }

        /// <summary>
        /// Normalizes Windows/macOS line endings then compares strings. Both arguments are serialized text from the same
        /// asset path (shuffle pass A vs shuffle pass B), passed as expected/actual for <see cref="Assert.AreEqual"/>.
        /// </summary>
        private void AssertSerializedAreEqual(string expected, string actual, string msg)
        {
            expected = expected.Replace("\r\n", "\n");
            actual = actual.Replace("\r\n", "\n");
            Assert.AreEqual(expected, actual, msg);
        }
    }
}
