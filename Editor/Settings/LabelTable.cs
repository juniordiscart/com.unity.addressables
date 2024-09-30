using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.AddressableAssets.GUI;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings
{
    [Serializable]
    class LabelTable
    {
        [FormerlySerializedAs("m_labelNames")]
        [SerializeField]
        List<string> m_LabelNames = new List<string>(new[] {"default"});

        internal List<string> labelNames
        {
            get { return m_LabelNames; }
        }

        internal bool AddLabelName(string name)
        {
            if (m_LabelNames.Contains(name))
                return false;
#if NET_UNITY_4_8
            if (name.Contains('[', StringComparison.Ordinal) && name.Contains(']', StringComparison.Ordinal))
#else
            if (name.Contains("[") && name.Contains("]"))
#endif
            {
                Debug.LogErrorFormat("Label name '{0}' cannot contain '[ ]'.", name);
                return false;
            }

            m_CurrentHash = default;
            m_LabelNames.Add(name);
            return true;
        }

        internal bool AddLabelName(string name, int index)
        {
            if (m_LabelNames.Contains(name))
                return false;
#if NET_UNITY_4_8
            if (name.Contains('[', StringComparison.Ordinal) && name.Contains(']', StringComparison.Ordinal))
#else
            if (name.Contains("[") && name.Contains("]"))
#endif
            {
                Debug.LogErrorFormat("Label name '{0}' cannot contain '[ ]'.", name);
                return false;
            }
            m_CurrentHash = default;
            m_LabelNames.Insert(index, name);
            return true;
        }

        internal string GetUniqueLabelName(string name)
        {
            var newName = name;
            int counter = 1;
            while (counter < 100)
            {
                if (!m_LabelNames.Contains(newName))
                    return newName;
                newName = name + counter;
                counter++;
            }

            return string.Empty;
        }

        internal bool RemoveLabelName(string name)
        {
            m_CurrentHash = default;
            return m_LabelNames.Remove(name);
        }

        internal string GetString(HashSet<string> val, float width)
        {
            if (val == null || val.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();

            var content = new GUIContent("");
            int remaining = val.Count;
            foreach (string s in val)
            {
                remaining--;
                content.text = s;
                var sx = UnityEngine.GUI.skin.label.CalcSize(content);
                width -= sx.x;

                string labelName = m_LabelNames.Contains(s) ? s :
                    AddressablesGUIUtility.ConvertTextToStrikethrough(s);

                if (remaining > 0)
                    sb.Append($"{labelName}, ");
                else
                    sb.Append(labelName);

                if (width < 20)
                    break;
            }

            return sb.ToString();
        }

        internal int GetIndexOfLabel(string label)
        {
            return m_LabelNames.IndexOf(label);
        }

        internal long GetMask(HashSet<string> maskSet)
        {
            if (maskSet.Count == 0)
                return 0;
            long one = 1;
            long val = 0;
            for (int i = 0; i < m_LabelNames.Count; i++)
                if (maskSet.Contains(m_LabelNames[i]))
                    val |= one << i;
            return val;
        }

        Hash128 m_CurrentHash;
        internal Hash128 currentHash
        {
            get
            {
                if (!m_CurrentHash.isValid)
                {
                    foreach (var label in m_LabelNames)
                        m_CurrentHash.Append(label);
                }
                return m_CurrentHash;
            }
        }
    }
}
