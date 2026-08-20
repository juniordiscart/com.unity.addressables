using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Build.DataBuilders.SchemaBuilders;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace UnityEditor.AddressableAssets.Tests
{
    /// <summary>
    /// Tests for BundledAssetSchemaBuilder.SetPrimaryKey – the O(1)
    /// dependency-slot rewrite introduced on dev/jacob/post-process-bundles.
    ///
    /// SetPrimaryKey relies on a cached index of (location, slotIndex) tuples
    /// rather than scanning every depender list on each rename.  These tests
    /// verify:
    ///   - Keys[0] is updated on the target entry
    ///   - Every slot that references the old key is rewritten to the new key
    ///   - Unrelated slots are left untouched
    ///   - The lookup maps are updated consistently
    ///   - The result matches a naïve full-scan (regression guard)
    /// </summary>
    [TestFixture]
    public class BundledAssetSchemaBuilderSetPrimaryKeyTests
    {
        // ── helpers ─────────────────────────────────────────────────────────

        static ContentCatalogDataEntry MakeEntry(string primaryKey, params string[] depKeys)
        {
            var keys = new List<object> { primaryKey };
            var deps = depKeys.Cast<object>().ToList();
            return new ContentCatalogDataEntry(
                typeof(object), primaryKey, "provider", keys, deps);
        }

        /// <summary>
        /// Creates a BundledAssetSchemaBuilder with both internal lookup maps
        /// pre-seeded from <paramref name="locations"/> and returns the paired
        /// aaContext so tests can call SetPrimaryKey directly.
        /// </summary>
        static (BundledAssetSchemaBuilder builder, AddressableAssetsBuildContext aaContext)
            MakeBuilderWithLocations(List<ContentCatalogDataEntry> locations)
        {
            var aaContext = new AddressableAssetsBuildContext
            {
                locations = locations
            };
            var builder = new BundledAssetSchemaBuilder();
            // Prime both maps so SetPrimaryKey's first call doesn't re-build them.
            builder.GetPrimaryKeyToLocation(locations);
            builder.GetPrimaryKeyToDependerLocations(locations);
            return (builder, aaContext);
        }

        // ── cases ────────────────────────────────────────────────────────────

        [Test]
        public void SetPrimaryKey_RewritesKeyOnTargetLocation()
        {
            var target = MakeEntry("key-A");
            var (builder, ctx) = MakeBuilderWithLocations(new List<ContentCatalogDataEntry> { target });

            builder.SetPrimaryKey(target, "key-B", ctx);

            Assert.AreEqual("key-B", target.Keys[0] as string,
                "Keys[0] of the renamed entry should become the new primary key");
        }

        [Test]
        public void SetPrimaryKey_RewritesAllDependerSlotsToNewKey()
        {
            var target = MakeEntry("bundle-A");
            // Three separate entries, each with one slot pointing at bundle-A
            var dep1 = MakeEntry("asset-1", "bundle-A");
            var dep2 = MakeEntry("asset-2", "bundle-A");
            var dep3 = MakeEntry("asset-3", "bundle-A");
            var locations = new List<ContentCatalogDataEntry> { target, dep1, dep2, dep3 };
            var (builder, ctx) = MakeBuilderWithLocations(locations);

            builder.SetPrimaryKey(target, "bundle-B", ctx);

            Assert.AreEqual("bundle-B", dep1.Dependencies[0] as string);
            Assert.AreEqual("bundle-B", dep2.Dependencies[0] as string);
            Assert.AreEqual("bundle-B", dep3.Dependencies[0] as string);
        }

        [Test]
        public void SetPrimaryKey_RewritesEveryOccurrence_WhenLocationDependsOnKeyInMultipleSlots()
        {
            // A single depender referencing the same bundle key in two slots.
            // The index stores one (location, slotIndex) entry per occurrence,
            // so both must be rewritten.
            var target = MakeEntry("bundle-X");
            var depender = MakeEntry("asset-1", "bundle-X", "bundle-X");
            var locations = new List<ContentCatalogDataEntry> { target, depender };
            var (builder, ctx) = MakeBuilderWithLocations(locations);

            builder.SetPrimaryKey(target, "bundle-Y", ctx);

            Assert.AreEqual("bundle-Y", depender.Dependencies[0] as string,
                "First slot should be rewritten");
            Assert.AreEqual("bundle-Y", depender.Dependencies[1] as string,
                "Second slot should also be rewritten");
        }

        [Test]
        public void SetPrimaryKey_LeavesUnrelatedDependencySlotsUntouched()
        {
            var targetA = MakeEntry("bundle-A");
            var targetB = MakeEntry("bundle-B");
            // depender references A in slot-0 and B in slot-1; rename A only
            var depender = MakeEntry("asset-1", "bundle-A", "bundle-B");
            var locations = new List<ContentCatalogDataEntry> { targetA, targetB, depender };
            var (builder, ctx) = MakeBuilderWithLocations(locations);

            builder.SetPrimaryKey(targetA, "bundle-A-new", ctx);

            Assert.AreEqual("bundle-A-new", depender.Dependencies[0] as string,
                "Renamed slot should be updated");
            Assert.AreEqual("bundle-B", depender.Dependencies[1] as string,
                "Unrelated slot must not be touched");
        }

        [Test]
        public void SetPrimaryKey_NoDependers_UpdatesLocationMapOnly_NoThrow()
        {
            // A bundle nobody depends on; SetPrimaryKey should still rename it
            // cleanly without touching any other entry.
            var target = MakeEntry("orphan-bundle");
            var unrelated = MakeEntry("other-bundle");
            var locations = new List<ContentCatalogDataEntry> { target, unrelated };
            var (builder, ctx) = MakeBuilderWithLocations(locations);

            Assert.DoesNotThrow(() => builder.SetPrimaryKey(target, "orphan-bundle-new", ctx),
                "No dependers should not cause any exception");
            Assert.AreEqual("orphan-bundle-new", target.Keys[0] as string);
        }

        [Test]
        public void SetPrimaryKey_UpdatesLookupMaps()
        {
            var target = MakeEntry("key-old");
            var depender = MakeEntry("dep", "key-old");
            var locations = new List<ContentCatalogDataEntry> { target, depender };
            var (builder, ctx) = MakeBuilderWithLocations(locations);

            builder.SetPrimaryKey(target, "key-new", ctx);

            var locMap = builder.GetPrimaryKeyToLocation(locations);
            Assert.IsFalse(locMap.ContainsKey("key-old"),
                "Old key must be removed from the location map");
            Assert.IsTrue(locMap.ContainsKey("key-new"),
                "New key must be present in the location map");
            Assert.AreSame(target, locMap["key-new"],
                "Location map entry should point to the same object");

            var depMap = builder.GetPrimaryKeyToDependerLocations(locations);
            Assert.IsFalse(depMap.ContainsKey("key-old"),
                "Old key must be removed from the depender map");
            Assert.IsTrue(depMap.ContainsKey("key-new"),
                "New key must be present in the depender map");
        }

        [Test]
        public void SetPrimaryKey_InvalidEntry_Throws()
        {
            var dummy = MakeEntry("x");
            var (builder, ctx) = MakeBuilderWithLocations(new List<ContentCatalogDataEntry> { dummy });

            // Null entry
            Assert.Throws<ArgumentException>(() => builder.SetPrimaryKey(null, "x-new", ctx));

            // Entry with no keys
            var noKeys = new ContentCatalogDataEntry(
                typeof(object), "id", "provider",
                new List<object>() /* empty keys */);
            Assert.Throws<ArgumentException>(() => builder.SetPrimaryKey(noKeys, "x-new", ctx));
        }

        [Test]
        public void SetPrimaryKey_MatchesNaiveFullScan_Golden()
        {
            // Build a non-trivial graph: 4 bundles, 10 assets each depending on
            // a subset of bundles (including multi-slot and multi-occurrence cases).
            // Rename bundle-1 and compare against a brute-force full scan.
            const string oldKey = "bundle-1";
            const string newKey = "bundle-1-renamed";

            var bundle1 = MakeEntry(oldKey);
            var bundle2 = MakeEntry("bundle-2");
            var bundle3 = MakeEntry("bundle-3");

            var assets = new List<ContentCatalogDataEntry>();
            // asset-0: depends on bundle-1 only
            assets.Add(MakeEntry("asset-0", oldKey));
            // asset-1: depends on bundle-1 twice (multi-slot)
            assets.Add(MakeEntry("asset-1", oldKey, oldKey));
            // asset-2: depends on bundle-2 and bundle-1
            assets.Add(MakeEntry("asset-2", "bundle-2", oldKey));
            // asset-3: no dependency on bundle-1
            assets.Add(MakeEntry("asset-3", "bundle-2", "bundle-3"));
            // asset-4 … asset-6: one slot each pointing at bundle-1
            for (int i = 4; i <= 6; i++)
                assets.Add(MakeEntry($"asset-{i}", oldKey));
            // asset-7: bundle-1 in slots 0 and 2, bundle-3 in slot 1
            assets.Add(MakeEntry("asset-7", oldKey, "bundle-3", oldKey));

            var allLocations = new List<ContentCatalogDataEntry> { bundle1, bundle2, bundle3 };
            allLocations.AddRange(assets);

            // Capture a deep copy of all Dependencies before rename for the golden comparison
            var snapshotBefore = allLocations
                .Select(loc => loc.Dependencies.Cast<string>().ToList())
                .ToList();

            var (builder, ctx) = MakeBuilderWithLocations(allLocations);
            builder.SetPrimaryKey(bundle1, newKey, ctx);

            // Naïve full-scan expected result: replace every oldKey with newKey
            for (int li = 0; li < allLocations.Count; li++)
            {
                var loc = allLocations[li];
                for (int si = 0; si < loc.Dependencies.Count; si++)
                {
                    string expected = snapshotBefore[li][si] == oldKey ? newKey : snapshotBefore[li][si];
                    Assert.AreEqual(expected, loc.Dependencies[si] as string,
                        $"Location '{loc.Keys[0]}' slot {si}: O(1) rewrite should match full-scan result");
                }
            }

            // Primary key of the renamed entry itself
            Assert.AreEqual(newKey, bundle1.Keys[0] as string);
        }
    }
}
