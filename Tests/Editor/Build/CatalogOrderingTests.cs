using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace UnityEditor.AddressableAssets.Tests
{
    /// <summary>
    /// Verifies that catalog entry ordering in WriteMergedCatalogs is
    /// deterministic: entries are sorted by InternalId using
    /// StringComparison.Ordinal, and the builder passes them through
    /// to SetData without re-sorting.
    /// </summary>
    public class CatalogOrderingTests
    {
        [Test]
        public void EntrySort_OrdinalOrder_UppercaseBeforeLowercase()
        {
            // Ordinal: 'B'(66) < 'Z'(90) < 'a'(97) — different from
            // culture-sensitive order where 'a' < 'B' < 'Z'.
            var entries = new List<ContentCatalogDataEntry>
            {
                Entry("a_path"),
                Entry("Z_path"),
                Entry("B_path"),
            };

            entries.Sort((a, b) =>
                string.Compare(a.InternalId, b.InternalId, StringComparison.Ordinal));

            Assert.AreEqual("B_path", entries[0].InternalId);
            Assert.AreEqual("Z_path", entries[1].InternalId);
            Assert.AreEqual("a_path", entries[2].InternalId);
        }

        [Test]
        public void EntrySort_AlreadySorted_IsUnchanged()
        {
            var entries = new List<ContentCatalogDataEntry>
            {
                Entry("A_path"),
                Entry("B_path"),
                Entry("C_path"),
            };

            entries.Sort((a, b) =>
                string.Compare(a.InternalId, b.InternalId, StringComparison.Ordinal));

            Assert.AreEqual("A_path", entries[0].InternalId);
            Assert.AreEqual("B_path", entries[1].InternalId);
            Assert.AreEqual("C_path", entries[2].InternalId);
        }

        [Test]
        public void EntrySort_Reversed_BecomesSorted()
        {
            var entries = new List<ContentCatalogDataEntry>
            {
                Entry("C_path"),
                Entry("B_path"),
                Entry("A_path"),
            };

            entries.Sort((a, b) =>
                string.Compare(a.InternalId, b.InternalId, StringComparison.Ordinal));

            Assert.AreEqual("A_path", entries[0].InternalId);
            Assert.AreEqual("B_path", entries[1].InternalId);
            Assert.AreEqual("C_path", entries[2].InternalId);
        }

        [Test]
        public void SetData_PreservesInputOrder_BuilderNoLongerSorts()
        {
            // The builder now passes catalogDataEntries straight through to
            // SetData (sorting moved to WriteMergedCatalogs). This test
            // confirms JsonContentCatalogData.SetData does not internally
            // reorder entries: the internalIds in the serialized JSON appear
            // in the same order as the input list.
            var entries = new List<ContentCatalogDataEntry>
            {
                Entry("Z_path"),
                Entry("A_path"),
                Entry("M_path"),
            };

            var catalog = new JsonContentCatalogData("test_catalog");
            catalog.SetData(entries);

            var json = JsonUtility.ToJson(catalog);
            int zIdx = json.IndexOf("Z_path", StringComparison.Ordinal);
            int aIdx = json.IndexOf("A_path", StringComparison.Ordinal);
            int mIdx = json.IndexOf("M_path", StringComparison.Ordinal);

            Assert.That(zIdx, Is.GreaterThanOrEqualTo(0), "Z_path not found in JSON");
            Assert.That(aIdx, Is.GreaterThanOrEqualTo(0), "A_path not found in JSON");
            Assert.That(mIdx, Is.GreaterThanOrEqualTo(0), "M_path not found in JSON");
            Assert.Greater(aIdx, zIdx, "A_path should appear after Z_path (input order preserved, no re-sort)");
            Assert.Greater(mIdx, aIdx, "M_path should appear after A_path (input order preserved, no re-sort)");
        }

        private static ContentCatalogDataEntry Entry(string internalId) =>
            new ContentCatalogDataEntry(typeof(object), internalId, typeof(object).FullName, new[] { internalId });
    }
}
