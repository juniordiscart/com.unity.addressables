using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor.AddressableAssets.GUI;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings
{
    [Serializable]
    class LabelTable : IList<string>, IList, ICollection
    {
        [FormerlySerializedAs("m_labelNames")]
        [SerializeField]
        List<string> m_LabelNames = new List<string>() { "default" };
        [NonSerialized]
        HashSet<string> m_LabelSet;

        GUIStyle m_LabelStyle;

        // Calls functions that can only be called in OnGUI()
        internal void Initialize()
        {
            if (m_LabelStyle == null)
                m_LabelStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        }

        private HashSet<string> GetLabelSet()
        {
            if (m_LabelSet == null)
                m_LabelSet = new HashSet<string>(m_LabelNames);
            return m_LabelSet;
        }

        public bool Contains(string item)
        {
            return GetLabelSet().Contains(item);
        }

        public string this[int index]
        {
            get => m_LabelNames[index];
            set
            {
                var set = GetLabelSet();
                var oldValue = m_LabelNames[index];
                m_LabelNames[index] = value;

                if (!m_LabelNames.Contains(oldValue))
                    set.Remove(oldValue);
                set.Add(value);
            }
        }

        public int Count => m_LabelNames.Count;

        internal bool AddLabelName(string name)
        {
            if (!GetLabelSet().Add(name))
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
            if (!GetLabelSet().Add(name))
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
            var set = GetLabelSet();
            while (counter < 100)
            {
                if (!set.Contains(newName))
                    return newName;
                newName = name + counter;
                counter++;
            }

            return string.Empty;
        }

        internal bool RemoveLabelName(string name)
        {
            m_CurrentHash = default;
            if (GetLabelSet().Remove(name))
            {
                m_LabelNames.Remove(name);
                return true;
            }

            return false;
        }

        private bool RemoveLabelNameAt(int index)
        {
            m_CurrentHash = default;
            if (index < 0 || index >= m_LabelNames.Count)
                return false;
            var label = m_LabelNames[index];
            m_LabelSet.Remove(label);
            m_LabelNames.RemoveAt(index);
            return true;
        }

        internal string GetString(HashSet<string> val, float width)
        {
            if (val == null || val.Count == 0 || m_LabelStyle == null)
                return "";

            StringBuilder sb = new StringBuilder();

            var content = new GUIContent("");
            int remaining = val.Count;
            foreach (string s in val)
            {
                remaining--;
                content.text = s;
                var sx = m_LabelStyle.CalcSize(content);
                width -= sx.x;

                string labelName = GetLabelSet().Contains(s) ? s :
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

        public void Clear()
        {
            m_LabelNames.Clear();
        }

#region explicit interface implementations
        void ICollection<string>.Add(string item)
        {
            AddLabelName(item);
        }

        int IList.Add(object value)
        {
            if (AddLabelName((string)value))
                return m_LabelNames.Count - 1;
            return -1;
        }

        bool IList.Contains(object value)
        {
            return m_LabelSet.Contains((string)value);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)m_LabelNames).IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            AddLabelName((string)value, index);
        }

        void IList.Remove(object value)
        {
            RemoveLabelName((string)value);
        }

        void IList.RemoveAt(int index)
        {
            RemoveLabelNameAt(index);
        }

        bool IList.IsFixedSize => ((IList)m_LabelNames).IsFixedSize;

        bool IList.IsReadOnly => ((IList)m_LabelNames).IsReadOnly;

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (string)value;
        }

        void ICollection<string>.CopyTo(string[] array, int arrayIndex)
        {
            m_LabelNames.CopyTo(array, arrayIndex);
        }

        bool ICollection<string>.Remove(string item)
        {
            return m_LabelNames.Remove(item);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)m_LabelNames).CopyTo(array, index);
        }

        bool ICollection.IsSynchronized => ((ICollection)m_LabelNames).IsSynchronized;

        object ICollection.SyncRoot => ((ICollection)m_LabelNames).SyncRoot;

        bool ICollection<string>.IsReadOnly => ((ICollection<string>)m_LabelNames).IsReadOnly;

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return m_LabelNames.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)m_LabelNames).GetEnumerator();
        }

        int IList<string>.IndexOf(string item)
        {
            return m_LabelNames.IndexOf(item);
        }

        void IList<string>.Insert(int index, string item)
        {
            AddLabelName(item, index);
        }

        void IList<string>.RemoveAt(int index)
        {
            RemoveLabelNameAt(index);
        }
#endregion
    }
}
