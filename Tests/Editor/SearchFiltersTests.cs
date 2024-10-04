using System;
using NUnit.Framework;
using UnityEditor.AddressableAssets.GUIElements;

namespace UnityEditor.AddressableAssets.Tests
{
    internal class SearchFiltersTests
    {
        [Test]
        public void FilterString_GetsCorrectFiltersForShortHand()
        {
            FilterString str = new FilterString();
            str.ProcessSearchValue("T:GameObject");
            Assert.AreEqual(1, str.Filters.Count);
            Assert.AreEqual('t', str.Filters[0].FilterIdentifier, "Filter Identifier expected to be lower case character t");
            Assert.AreEqual("GameObject", str.Filters[0].FilterValue, "GameObject string was expected to be separated");
        }

        [Test]
        public void FilterString_FiltersClear()
        {
            FilterString str = new FilterString();
            str.ProcessSearchValue("F:Filter");
            Assert.AreEqual(1, str.Filters.Count);
            str.Clear();
            Assert.AreEqual(0, str.Filters.Count);
        }

        [Test]
        public void FilterString_IsValid()
        {
            FilterString str = new FilterString();
            Assert.IsFalse(str.IsValid);
            str.ProcessSearchValue("F:Filter");
            Assert.IsTrue(str.IsValid);
            str.Clear();
            Assert.IsFalse(str.IsValid);
            str.ProcessSearchValue("this is some text");
            Assert.IsTrue(str.IsValid);
            Assert.AreEqual(0, str.Filters.Count);
            Assert.AreEqual(4, str.StringFilters.Count);
        }

        [Test]
        public void FilterString_CorrectNonFilters()
        {
            FilterString str = new FilterString();
            str.ProcessSearchValue("text1 T:GameObject text2");
            Assert.AreEqual(1, str.Filters.Count);
            Assert.AreEqual(2, str.StringFilters.Count);
            Assert.AreEqual("text1", str.StringFilters[0]);
            Assert.AreEqual("text2", str.StringFilters[1]);
        }

        [Test]
        public void FilterString_GetsCorrectFiltersForLongHand()
        {
            FilterString str = new FilterString();
            str.AddFilterLongHand("AssetType", 'T');
            str.ProcessSearchValue("AssetType:GameObject");
            Assert.AreEqual(1, str.Filters.Count);
            Assert.AreEqual('t', str.Filters[0].FilterIdentifier, "Filter Identifier expected to be lower case character t");
            Assert.AreEqual("GameObject", str.Filters[0].FilterValue, "GameObject string was expected to be separated");
        }

        [Test]
        public void NumericQuery_Equal()
        {
            NumericQuery q = new NumericQuery();
            q.Parse("50");
            Assert.IsFalse(q.Evaluate(49));
            Assert.IsTrue(q.Evaluate(50));
            Assert.IsFalse(q.Evaluate(51));
            q.Parse("=50");
            Assert.IsFalse(q.Evaluate(49));
            Assert.IsTrue(q.Evaluate(50));
            Assert.IsFalse(q.Evaluate(51));
        }
        [Test]
        public void NumericQuery_LessThan()
        {
            NumericQuery q = new NumericQuery();
            q.Parse("<5");
            Assert.IsFalse(q.Evaluate(6));
            Assert.IsFalse(q.Evaluate(5));
            Assert.IsTrue(q.Evaluate(4));
            Assert.IsTrue(q.Evaluate(3));
        }
        [Test]
        public void NumericQuery_GreaterThan()
        {
            NumericQuery q = new NumericQuery();
            q.Parse(">5");
            Assert.IsFalse(q.Evaluate(4));
            Assert.IsFalse(q.Evaluate(5));
            Assert.IsTrue(q.Evaluate(6));
            Assert.IsTrue(q.Evaluate(7));
        }
    }
}
