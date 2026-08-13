using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// File system helpers for synchronous and coroutine-based IO.
    /// </summary>
    public static class FileUtils
    {
        #region Static Methods
        /// <summary>
        /// Loads all text from a file.
        /// </summary>
        /// <param name="filePath">Path to the file to read.</param>
        /// <returns>The file contents as text.</returns>
        public static string LoadFromFile(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BufferedStream bs = new BufferedStream(fs))
            using (StreamReader sr = new StreamReader(bs, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }

        /// <summary>
        /// Saves text to a file, creating directories as needed.
        /// </summary>
        /// <param name="filePath">Path to the file to write.</param>
        /// <param name="data">Text to write.</param>
        public static void SaveToFile(string filePath, string data)
        {
            EnsureDirectoryExist(filePath);

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (BufferedStream bs = new BufferedStream(fs))
            using (StreamWriter sw = new StreamWriter(bs, Encoding.UTF8))
            {
                sw.Write(data);
            }
        }

        /// <summary>
        /// Loads JSON data from a file asynchronously as a coroutine.
        /// </summary>
        /// <typeparam name="T">Type to deserialize.</typeparam>
        /// <param name="filePath">Path to the file to read.</param>
        /// <param name="onComplete">Callback invoked with the deserialized data.</param>
        /// <param name="bytesPerStep">Maximum characters read per frame.</param>
        /// <returns>An enumerator for coroutine execution.</returns>
        public static IEnumerator LoadFromFileAsync<T>(string filePath, Action<T> onComplete, int bytesPerStep = 1024 * 1024)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError("File not found: " + filePath);

                yield break;
            }


            StreamReader streamReader = new StreamReader(filePath);

            StringBuilder data = new StringBuilder();

            while (true)
            {
                try
                {
                    char[] buffer = new char[bytesPerStep];

                    int bytesRead = streamReader.Read(buffer, 0, bytesPerStep);

                    if (bytesRead > 0)
                    {
                        data.Append(buffer, 0, bytesRead);
                    }
                    else
                    {

                        streamReader.Close();

                        onComplete?.Invoke(JsonUtility.FromJson<T>(data.ToString()));

                        break;
                    }

                }
                catch (Exception ex)
                {

                    Debug.LogError($"{nameof(LoadFromFileAsync)} Error : {ex.Message}");

                    streamReader.Close();

                    break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Saves text to a file asynchronously as a coroutine.
        /// </summary>
        /// <param name="serializedData">Text to write.</param>
        /// <param name="filePath">Path to the file to write.</param>
        /// <param name="onComplete">Callback invoked with success state.</param>
        /// <param name="bytesPerStep">Maximum characters written per frame.</param>
        /// <returns>An enumerator for coroutine execution.</returns>
        public static IEnumerator SaveToFileAsync(string serializedData, string filePath,
            Action<bool> onComplete = null, int bytesPerStep = 1024 * 1024)
        {
            EnsureDirectoryExist(filePath);

            var data = new StringBuilder(serializedData);

            StreamWriter streamWriter = new StreamWriter(filePath, false, Encoding.UTF8);

            while (data.Length > 0)
            {
                try
                {

                    int lengthToWrite = Math.Min(bytesPerStep, data.Length);

                    char[] buffer = new char[lengthToWrite];

                    data.CopyTo(0, buffer, 0, lengthToWrite);

                    streamWriter.Write(buffer, 0, lengthToWrite);

                    data.Remove(0, lengthToWrite);
                }
                catch (Exception ex)
                {

                    Debug.LogError($"{nameof(SaveToFileAsync)} Error : {ex.Message}");

                    streamWriter?.Close();

                    onComplete?.Invoke(false);

                    yield break;
                }

                yield return null;
            }

            streamWriter.Close();

            onComplete?.Invoke(true);
        }

        /// <summary>
        /// Ensures the directory for the specified file path exists.
        /// </summary>
        /// <param name="filePath">File path whose directory should exist.</param>
        public static void EnsureDirectoryExist(string filePath)
        {
            string directoryPath = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        #endregion
    }
}
