using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Helpers for deriving common names and asset types.
    /// </summary>
    public static class NamingUtil
    {
        #region Constants
        private static readonly Dictionary<string, string> ExtensionToType = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".png", "Texture" }, { ".jpg", "Texture" }, { ".jpeg", "Texture" }, { ".tga", "Texture" },
            { ".psd", "Texture" }, { ".psb", "Texture" },
            { ".mp3", "Audio" }, { ".wav", "Audio" }, { ".ogg", "Audio" },
            { ".prefab", "Prefab" },
            { ".fbx", "Model" }, { ".obj", "Model" },
            { ".anim", "Animation" }, { ".controller", "Animator" },
            { ".mat", "Material" },
            { ".shader", "Shader" },
            { ".asset", "Asset" }
        };
        #endregion

        #region Static Methods
        /// <summary>
        /// Determines whether most items satisfy a predicate.
        /// </summary>
        /// <param name="paths">Items to evaluate.</param>
        /// <param name="predicate">Predicate used for matching.</param>
        /// <param name="minRatio">Minimum ratio of matches required.</param>
        /// <returns>True when the ratio of matches meets or exceeds the threshold.</returns>
        public static bool MostMatch(IEnumerable<string> paths, Func<string, bool> predicate, float minRatio = 0.75f)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            if (minRatio < 0f || minRatio > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(minRatio), "minRatio must be between 0 and 1.");
            }

            int total = 0, matchCount = 0;

            foreach (var path in paths)
            {
                total++;

                if (predicate(path))
                {
                    matchCount++;
                }
            }

            return total > 0 && matchCount >= total * minRatio;
        }

        /// <summary>
        /// Returns the most common element if it meets the ratio threshold.
        /// </summary>
        /// <param name="items">Items to analyze.</param>
        /// <param name="minRatio">Minimum ratio of occurrences required.</param>
        /// <returns>The most common element or null if the threshold is not met.</returns>
        public static string GetMajorityElement(IEnumerable<string> items, float minRatio = 0.75f)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (minRatio < 0f || minRatio > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(minRatio), "minRatio must be between 0 and 1.");
            }

            var counts = new Dictionary<string, int>();

            int total = 0;

            foreach (var item in items)
            {
                if (item == null)
                {
                    continue;
                }


                total++;

                if (!counts.TryAdd(item, 1))
                {
                    counts[item]++;
                }
            }

            if (total == 0)
            {
                return null;
            }


            var (mostCommon, maxCount) = counts.OrderByDescending(kv => kv.Value).First();

            return maxCount >= total * minRatio ? mostCommon : null;
        }

        /// <summary>
        /// Returns the most common asset type based on file extensions.
        /// </summary>
        /// <param name="filePaths">File paths to inspect.</param>
        /// <param name="minRatio">Minimum ratio of occurrences required.</param>
        /// <returns>The most common asset type or null if the threshold is not met.</returns>
        public static string GetMajorityAssetType(IEnumerable<string> filePaths, float minRatio = 0.75f)
        {
            if (filePaths == null)
            {
                throw new ArgumentNullException(nameof(filePaths));
            }


            var types = filePaths
                .Select(Path.GetExtension)
                .Where(ext => !string.IsNullOrEmpty(ext))
                .Select(ext => ExtensionToType.TryGetValue(ext, out var type) ? type : "Unknown")
                .ToList();

            return GetMajorityElement(types, minRatio);
        }

        /// <summary>
        /// Finds the most common word across a set of names.
        /// </summary>
        /// <param name="names">Names to analyze.</param>
        /// <param name="minRatio">Minimum ratio of occurrences required.</param>
        /// <returns>The most common word or null if the threshold is not met.</returns>
        public static string FindMostCommonWord(IEnumerable<string> names, float minRatio = 0.75f)
        {
            if (names == null)
            {
                throw new ArgumentNullException(nameof(names));
            }

            if (minRatio < 0f || minRatio > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(minRatio), "minRatio must be between 0 and 1.");
            }

            var tokenCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            int totalEntries = 0;

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }


                totalEntries++;

                var tokens = ExtractTokens(name);

                foreach (var token in tokens)
                {
                    if (!tokenCounts.TryAdd(token, 1))
                    {
                        tokenCounts[token]++;
                    }
                }
            }

            if (totalEntries == 0 || tokenCounts.Count == 0)
            {
                return null;
            }


            var (mostCommon, count) = tokenCounts.OrderByDescending(kv => kv.Value).First();

            return count >= totalEntries * minRatio ? mostCommon : null;
        }

        private static IEnumerable<string> ExtractTokens(string input)
        {
            IEnumerable<string> parts = Regex.Matches(input, @"[A-Z]?[a-z]+|[A-Z]+(?![a-z])")
                .Cast<Match>()
                .Select(m => m.Value);

            var basicSplit = Regex.Split(input, @"[\s_\-\.0-9]+");

            return parts.Concat(basicSplit)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }
        #endregion
    }
}
