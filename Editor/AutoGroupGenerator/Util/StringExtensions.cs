using System.Text.RegularExpressions;

namespace AutoGroupGenerator
{
    /// <summary>
    /// String helpers for formatting identifiers and file names.
    /// </summary>
    public static class StringExtensions
    {
        #region Static Methods
        /// <summary>
        /// Converts a string to a more readable, spaced format.
        /// </summary>
        /// <param name="input">The source string.</param>
        /// <returns>A readable representation of the string.</returns>
        public static string ToReadableFormat(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }


            input = Regex.Replace(input, "^(m_|k_)", "");

            input = Regex.Replace(input, "(\\B[A-Z])", " $1");

            return char.ToUpper(input[0]) + input.Substring(1);
        }

        /// <summary>
        /// Removes the extension from a file name.
        /// </summary>
        /// <param name="fileName">File name to process.</param>
        /// <returns>The file name without its extension.</returns>
        public static string RemoveExtension(this string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return fileName;
            }


            int index = fileName.LastIndexOf('.');

            return index > 0 ? fileName.Substring(0, index) : fileName;
        }
        #endregion
    }
}
