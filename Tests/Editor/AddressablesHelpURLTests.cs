using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using AutoGroupGenerator;

namespace UnityEditor.AddressableAssets.Tests
{
    /// <summary>
    /// Verifies that the Addressables ScriptableObjects shown in the Inspector advertise a working
    /// documentation page through <see cref="AddressablesHelpURLAttribute"/>. Without this, the
    /// Inspector header help (?) button resolves to a non-existent "class-&lt;Type&gt;" page and opens
    /// a "page is missing" error (UUM-125928).
    /// </summary>
    public class AddressablesHelpURLTests
    {
        // Each entry maps an inspectable type to the manual page its help (?) button must open.
        // Keep this in sync with the [AddressablesHelpURL("...")] attributes on the types.
        static readonly (Type type, string page)[] k_ExpectedHelpUrls =
        {
            (typeof(AddressableAssetSettings), "AddressableAssetSettings.html"),
            (typeof(AddressableAssetGroup), "Groups.html"),
            (typeof(AddressableAssetGroupTemplate), "GroupTemplates.html"),
            (typeof(BundledAssetGroupSchema), "group-inspector-settings-reference.html"),
            (typeof(ContentUpdateGroupSchema), "content-update-build-settings.html"),
            (typeof(PlayerDataGroupSchema), "GroupSchemas.html"),
            (typeof(ContentDirectoryGroupSchema), "GroupSchemas.html"),
            (typeof(CacheInitializationSettings), "AddressableAssetSettings.html#initialization-objects"),
            #if !ENABLE_JSON_CATALOG
                        (typeof(BinaryCatalogInitializationSettings), "build-content-catalogs.html"),
            #endif
            (typeof(ProfileDataSourceSettings), "AddressablesCCD.html"),
            (typeof(BuildScriptPackedMode), "builds-full-build.html"),
            (typeof(BuildScriptPackedPlayMode), "Builds.html"),
            (typeof(BuildScriptFastMode), "Builds.html"),
            (typeof(BuildScriptSchemaDriven), "Builds.html"),
            (typeof(AddressableAssetSettingsDefaultObject), "AddressableAssetSettings.html"),
            (typeof(AddressableAssetGroupSortSettings), "GroupsWindow.html"),
            (typeof(AutoGroupGeneratorSettings), "groups-auto-group-generator-reference.html"),
            (typeof(AssetSelectionInputRule), "groups-auto-group-generator-reference.html"),
            (typeof(DefaultOutputRule), "groups-auto-group-generator-reference.html"),
            (typeof(ImprovedNamesOutputRule), "groups-auto-group-generator-reference.html"),
        };

        static IEnumerable<TestCaseData> HelpUrlCases()
        {
            foreach (var (type, page) in k_ExpectedHelpUrls)
                yield return new TestCaseData(type, page).SetName($"HelpUrl_{type.Name}");
        }

        [TestCaseSource(nameof(HelpUrlCases))]
        public void Type_HasAddressablesHelpURL_PointingAtExpectedManualPage(Type type, string page)
        {
            // Resolve the URL through the exact entry point the Inspector header (?) button uses:
            // EditorGUI.HelpIconButton -> Help.HasHelpForObject / Help.ShowHelpForObject -> Help.GetHelpURLForObject.
            // That method calls obj.GetType().GetCustomAttributes(typeof(HelpURLAttribute), true) (managed
            // reflection, which finds subclasses AND runs their constructor), so this exercises the real
            // button behaviour rather than just confirming the attribute exists.
            var instance = ScriptableObject.CreateInstance(type);
            try
            {
                var resolvedUrl = Help.GetHelpURLForObject(instance);
                var expectedUrl = AddressableAssetUtility.GenerateDocsURL(page);

                Assert.AreEqual(expectedUrl, resolvedUrl,
                    $"{type.Name}: the Inspector (?) button does not resolve to the expected manual page " +
                    $"(empty means it would fall back to the missing class-{type.Name} page).");

                // The attribute itself should be our versioned subclass.
                Assert.IsInstanceOf<AddressablesHelpURLAttribute>(
                    Attribute.GetCustomAttribute(type, typeof(HelpURLAttribute), true),
                    $"{type.Name} should use [AddressablesHelpURL] so the URL tracks the installed package version.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GeneratedHelpUrl_IsAddressablesPackageManualUrl()
        {
            // Guards against the attribute silently producing an empty or malformed URL.
            var url = AddressableAssetUtility.GenerateDocsURL("AddressableAssetSettings.html");
            StringAssert.StartsWith("https://docs.unity3d.com/Packages/com.unity.addressables@", url);
            StringAssert.Contains("/manual/AddressableAssetSettings.html", url);
        }
    }
}
