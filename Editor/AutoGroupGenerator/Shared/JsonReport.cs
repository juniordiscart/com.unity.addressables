using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Serializable container for diagnostic summaries saved to disk.
    /// </summary>
    [Serializable]
    public class JsonReport
    {
        #region Types
        [Serializable]
        private class StringWrapper
        {
            public string Value;
        }

        [Serializable]
        private class ListWrapper<T>
        {
            public List<T> Items;
        }
        #endregion

        /// <summary>
        /// Summary text for the report.
        /// </summary>
        public string Summary;

        /// <summary>
        /// Serialized report payload data.
        /// </summary>
        public string Data;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonReport"/> class.
        /// </summary>
        /// <param name="summary">Summary text for the report.</param>
        /// <param name="data">Serialized data payload.</param>
        public JsonReport(string summary, string data)
        {
            Summary = summary;
            Data = data;
        }

        /// <summary>
        /// Saves a JSON report to disk with a custom filename.
        /// </summary>
        /// <param name="reporter">Type generating the report.</param>
        /// <param name="filename">File name to use for the report.</param>
        /// <param name="summary">Summary information to include.</param>
        /// <param name="data">Data payload to serialize.</param>
        public static void SaveJsonReport(Type reporter, string filename, string summary, object data)
        {
            var reportName = reporter.Name;
            var filePath = Path.Combine(Constants.FilePaths.PersistentDataFolder, $"{filename}.json");
            var serializedData = SerializeData(data);
            var jsonReport = new JsonReport($"Reporter = {reportName} | " + summary, serializedData);
            FileUtils.SaveToFile(filePath, JsonUtility.ToJson(jsonReport, true));
        }

        /// <summary>
        /// Saves a JSON report to disk using the reporter name as the filename.
        /// </summary>
        /// <param name="reporter">Type generating the report.</param>
        /// <param name="summary">Summary information to include.</param>
        /// <param name="data">Data payload to serialize.</param>
        public static void SaveJsonReport(Type reporter, string summary, object data)
        {
            SaveJsonReport(reporter, reporter.Name, summary, data);
        }

        private static string SerializeData(object data)
        {
            if (data == null)
            {
                return "null";
            }

            try
            {
                if (data is string stringData)
                {
                    return JsonUtility.ToJson(new StringWrapper { Value = stringData }, true);
                }

                var dataType = data.GetType();

                if (typeof(IList).IsAssignableFrom(dataType) && dataType.IsGenericType)
                {
                    var elementType = dataType.GetGenericArguments()[0];
                    var wrapperType = typeof(ListWrapper<>).MakeGenericType(elementType);
                    var wrapper = Activator.CreateInstance(wrapperType);
                    var field = wrapperType.GetField(nameof(ListWrapper<int>.Items), BindingFlags.Public | BindingFlags.Instance);
                    field?.SetValue(wrapper, data);

                    return JsonUtility.ToJson(wrapper, true);
                }

                return JsonUtility.ToJson(data, true);
            }
            catch (Exception)
            {
                return "{}";
            }
        }
    }
}
