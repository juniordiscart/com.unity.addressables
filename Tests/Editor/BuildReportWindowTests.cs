#if UNITY_2022_2_OR_NEWER
using NUnit.Framework;
using System;
using UnityEditor.AddressableAssets.BuildReportVisualizer;

namespace UnityEditor.AddressableAssets.Tests
{
    public class BuildReportWindowTests
    {
        public class TimeAgoGetStringTests
        {
            [Test]
            public void SecondLevelGranularitySingularWorks()
            {
                var current = DateTime.UtcNow;
                var input = current.AddSeconds(-1);
                var expected = "Just now";
                var actual = BuildReportUtility.TimeAgo.GetString(input);
                Assert.AreEqual(expected, actual);
            }

            [Test]
            public void MinuteLevelGranularitySingularWorks()
            {
                var current = DateTime.UtcNow;
                var input = current.AddSeconds(-60);
                var expected = "a minute ago";
                var actual = BuildReportUtility.TimeAgo.GetString(input);
                Assert.AreEqual(expected, actual);
            }

            [Test]
            public void MinuteLevelGranularityPluralWorks()
            {
                var current = DateTime.UtcNow;
                var input = current.AddSeconds(-60 * 16);
                var expected = "16 minutes ago";
                var actual = BuildReportUtility.TimeAgo.GetString(input);
                Assert.AreEqual(expected, actual);
            }

            [Test]
            public void HourLevelGranularitySingularWorks()
            {
                var current = DateTime.UtcNow;
                var input = current.AddSeconds(-60 * 60 * 1);
                var expected = "an hour ago";
                var actual = BuildReportUtility.TimeAgo.GetString(input);
                Assert.AreEqual(expected, actual);
            }

            [Test]
            public void HourLevelGranularityPluralWorks()
            {
                var current = DateTime.UtcNow;
                var input = current.AddSeconds(-60 * 60 * 5);
                var expected = "5 hours ago";
                var actual = BuildReportUtility.TimeAgo.GetString(input);
                Assert.AreEqual(expected, actual);
            }
            [Test]
            public void DayLevelGranularitySingularWorks()
            {
                var current = DateTime.UtcNow;
                var input = current.AddSeconds(-60 * 60 * 24);
                var expected = "yesterday";
                var actual = BuildReportUtility.TimeAgo.GetString(input);
                Assert.AreEqual(expected, actual);
            }

            [Test]
            public void DayLevelGranularityPluralWorks()
            {
                var current = DateTime.UtcNow;
                var input = current.AddSeconds(-60 * 60 * 24 * 5);
                var expected = "5 days ago";
                var actual = BuildReportUtility.TimeAgo.GetString(input);
                Assert.AreEqual(expected, actual);
            }
            [Test]
            public void MonthLevelGranularitySingularWorks()
            {
                var current = DateTime.UtcNow;
                var input = current.AddSeconds(-60 * 60 * 24 * 30);
                var expected = "a month ago";
                var actual = BuildReportUtility.TimeAgo.GetString(input);
                Assert.AreEqual(expected, actual);
            }

            [Test]
            public void MonthLevelGranularityPluralWorks()
            {
                var current = DateTime.UtcNow;
                var input = current.AddSeconds(-60 * 60 * 24 * 30 * 5);
                var expected = "5 months ago";
                var actual = BuildReportUtility.TimeAgo.GetString(input);
                Assert.AreEqual(expected, actual);
            }
            [Test]
            public void YearLevelGranularitySingularWorks()
            {
                var current = DateTime.UtcNow;
                var input = current.AddSeconds(-60 * 60 * 24 * 365);
                var expected = "one year ago";
                var actual = BuildReportUtility.TimeAgo.GetString(input);
                Assert.AreEqual(expected, actual);
            }

            [Test]
            public void YearLevelGranularityPluralWorks()
            {
                var current = DateTime.UtcNow;
                var input = current.AddSeconds(-60 * 60 * 24 * 365 * 5);
                var expected = "5 years ago";
                var actual = BuildReportUtility.TimeAgo.GetString(input);
                Assert.AreEqual(expected, actual);
            }
        }
    }
}
#endif
