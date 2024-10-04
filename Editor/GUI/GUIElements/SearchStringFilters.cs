using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.AddressableAssets.GUIElements
{
    /// <summary>
    /// string based filter with identifying char
    /// </summary>
    internal struct Filter
    {
        public char FilterIdentifier;
        public string FilterValue;

        public Filter(char id, string value)
        {
            FilterIdentifier = id;
            FilterValue = value;
        }
    }

    internal class FilterString
    {
        private Dictionary<string, char> m_LongHandToFilterChar = new Dictionary<string, char>();
        private string m_PreviousStringQueryValue = null;

        private List<Filter> m_Filters = new List<Filter>();
        private List<string> m_NonFilterStringValues = new List<string>();

        /// <summary>
        /// true if the parsed string query resulted is valid search queries
        /// </summary>
        public bool IsValid => m_Filters.Count > 0 || m_NonFilterStringValues.Count > 0;


        public List<Filter> Filters => m_Filters;
        public List<string> StringFilters => m_NonFilterStringValues;

        public void AddFilterLongHand(string longHand, char shortHand)
        {
            m_LongHandToFilterChar[longHand.ToLowerInvariant()] = char.ToLowerInvariant(shortHand);
        }

        public void Clear()
        {
            m_PreviousStringQueryValue = null;
            m_Filters.Clear();
            m_NonFilterStringValues.Clear();
        }

        /// <summary>
        /// Take the given string and convert to content search queries
        /// </summary>
        /// <param name="value">string to parse</param>
        public void ProcessSearchValue(string value)
        {
            if (value == m_PreviousStringQueryValue)
                return;

            Clear();
            if (string.IsNullOrWhiteSpace(value))
                return;
            m_PreviousStringQueryValue = value.Replace(',', ' ');

            List<(int, int)> filterBlocks = new List<(int, int)>();
            int filterStart;
            int filterEnd = -1;

            for (int i = value.IndexOf(':'); i > -1; i = value.IndexOf(':', i + 1))
            {
                if (i > 0 && filterEnd > 0 && filterEnd >= i)
                    continue;

                char filterChar = 'q';

                if (i == 0)
                {
                    filterStart = 0;
                }
                else if (i == 1)
                {
                    filterChar = char.ToLowerInvariant(value[0]);
                    filterStart = 0;
                }
                else
                {
                    int prevWhitespace = i - 1;
                    while (prevWhitespace >= 0)
                    {
                        if (value[prevWhitespace] == ' ')
                            break;
                        prevWhitespace--;
                    }

                    if (prevWhitespace == i - 1)
                    {
                        // whitespace right before : (no filter defined)
                        filterStart = i;
                    }
                    else
                    {
                        filterStart = prevWhitespace + 1;
                        int length = i - (prevWhitespace + 1);
                        if (length > 1)
                        {
                            string filterDefinitionString = value.Substring(prevWhitespace + 1, length).ToLowerInvariant();
                            if (m_LongHandToFilterChar.TryGetValue(filterDefinitionString, out char shortHand))
                                filterChar = shortHand;
                        }
                        else
                            filterChar = char.ToLowerInvariant(value[i - 1]);
                    }
                }

                int nextSpaceIndex = value.IndexOf(' ', i);
                string filterValue;
                if (nextSpaceIndex > 0)
                {
                    filterEnd = nextSpaceIndex;
                    filterValue = value.Substring(i + 1, nextSpaceIndex - (i + 1));
                }
                else
                {
                    filterEnd = value.Length;
                    filterValue = value.Substring(i + 1);
                }

                filterBlocks.Add((filterStart, filterEnd));
                if (filterChar == 'q' || string.IsNullOrEmpty(filterValue))
                    continue;

                m_Filters.Add(new Filter(filterChar, filterValue));
            }

            // remaining string content is used to query the name filter
            string nameSearchString = value;
            for (int i = filterBlocks.Count - 1; i >= 0; --i)
            {
                int length = filterBlocks[i].Item2 - filterBlocks[i].Item1;
                if (length > 0)
                    nameSearchString = nameSearchString.Remove(filterBlocks[i].Item1, length);
            }

            string[] nameFilters = nameSearchString.Split(' ');
            foreach (string nameFilter in nameFilters)
            {
                if (string.IsNullOrWhiteSpace(nameFilter))
                    continue;
                m_NonFilterStringValues.Add(nameFilter);
            }
        }
    }

    internal struct NumericQuery
    {
        private enum Equality
        {
            NotSpecified = 0,
            Equal,
            GreaterThan,
            LessThan
        }

        private const Equality k_DefaultEquality = Equality.Equal;
        private Equality m_Equality;
        private int m_Value;

        /// <summary>
        /// True if the numeric search query has been correctly parsed
        /// </summary>
        public bool IsValid => m_Equality != Equality.NotSpecified;

        /// <summary>
        /// Parse a numeric equality string to get the equality symbol and number value
        /// </summary>
        /// <param name="parseString">string to parse for numeric equality</param>
        /// <returns>true if the parse was successful, else false</returns>
        public bool Parse(string parseString)
        {
            if (int.TryParse(parseString, out m_Value))
            {
                // string has no equality, just number
                m_Equality = k_DefaultEquality;
                return true;
            }

            if (parseString.Length == 1)
            {
                // not a full query
                m_Equality = Equality.NotSpecified;
                m_Value = 0;
                return false;
            }

            char firstChar = parseString[0];
            m_Equality = firstChar == '<' ? Equality.LessThan :
                firstChar == '>' ? Equality.GreaterThan :
                firstChar == '=' ? Equality.Equal : Equality.NotSpecified;

            if (m_Equality == Equality.NotSpecified)
            {
                // not numeric or equality character at the start
                m_Value = 0;
                return false;
            }

            string numberString = parseString.Substring(1);
            if (!int.TryParse(numberString, out m_Value))
            {
                // not a valid number, invalidate
                m_Equality = Equality.NotSpecified;
                m_Value = 0;
            }

            return true;
        }

        /// <summary>
        /// Evaluates a value based on the equality set for the query
        /// </summary>
        /// <param name="value">value to check for evaluation</param>
        /// <returns>true if the evaluation is true, else false</returns>
        public bool Evaluate(int value)
        {
            switch (m_Equality)
            {
                case Equality.Equal: return value == m_Value;
                case Equality.GreaterThan: return value > m_Value;
                case Equality.LessThan: return value < m_Value;
                default: return value == m_Value;
            }
        }

        /// <summary>
        /// invalidates and clears the query
        /// </summary>
        public void Clear()
        {
            m_Equality = Equality.NotSpecified;
            m_Value = 0;
        }
    }
}
