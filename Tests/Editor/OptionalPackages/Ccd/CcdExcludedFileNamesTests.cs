#if (ENABLE_CCD && ENABLE_CONTENT_DIRECTORIES)
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace UnityEditor.AddressableAssets.Tests.OptionalPackages.Ccd
{
    public class CcdExcludedFileNamesTests : AddressableAssetTestBase
    {
        [Test]
        public void GetExcludedFileNamesIncludesEnabledContentDirectoryCatalogAndManifest()
        {
            const string catalogName = "MyCustomContentCatalog";
            var group = Settings.CreateGroup("Content Directory Group", false, false, false, null);
            try
            {
                var schema = group.AddSchema<ContentDirectoryGroupSchema>();
                schema.CatalogId = catalogName;
                schema.IsEnabled = true;

                var excluded = CcdBuildEvents.GetExcludedFileNames(Settings);

                Assert.IsTrue(excluded.Contains($"{catalogName}.bin"), $"{catalogName}.bin should be excluded");
                Assert.IsTrue(excluded.Contains($"{catalogName}.hash"), $"{catalogName}.hash should be excluded");
                Assert.IsTrue(excluded.Contains(ContentDirectorySchemaBuilder.ContentDirectoryArchiver.kBuildManifestHashFileName),
                    $"{ContentDirectorySchemaBuilder.ContentDirectoryArchiver.kBuildManifestHashFileName} should be excluded");
                // Exclusion is case-insensitive
                Assert.IsTrue(excluded.Contains($"{catalogName.ToUpperInvariant()}.BIN"), "Exclusion should be case-insensitive");
            }
            finally
            {
                Settings.RemoveGroup(group);
            }
        }

        [Test]
        public void GetExcludedFileNamesExcludesDisabledContentDirectoryCatalog()
        {
            const string catalogName = "DisabledContentCatalog";
            var group = Settings.CreateGroup("Disabled Content Directory Group", false, false, false, null);
            try
            {
                var schema = group.AddSchema<ContentDirectoryGroupSchema>();
                schema.CatalogId = catalogName;
                schema.IsEnabled = false;

                var excluded = CcdBuildEvents.GetExcludedFileNames(Settings);

                Assert.IsFalse(excluded.Contains($"{catalogName}.bin"), "Disabled catalog .bin should not be excluded");
                Assert.IsFalse(excluded.Contains($"{catalogName}.hash"), "Disabled catalog .hash should not be excluded");
                // The build manifest hash is always excluded regardless of any enabled schema
                Assert.IsTrue(excluded.Contains(ContentDirectorySchemaBuilder.ContentDirectoryArchiver.kBuildManifestHashFileName),
                    $"{ContentDirectorySchemaBuilder.ContentDirectoryArchiver.kBuildManifestHashFileName} should always be excluded");
            }
            finally
            {
                Settings.RemoveGroup(group);
            }
        }
    }
}
#endif
