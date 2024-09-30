#if ENABLE_ADDRESSABLE_PROFILER

using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement.Profiling
{
    internal class ProfilerFrameData<T1,T2>
    {
        private Dictionary<T1, T2> m_Data;
        internal Dictionary<T1, T2> Data => m_Data;

        private T2[] m_Array;
        private uint m_Version;
        private uint m_ArrayVersion;

        public ProfilerFrameData()
        {
            m_Data = new Dictionary<T1, T2>(32);
        }

        public ProfilerFrameData(int count)
        {
            m_Data = new Dictionary<T1, T2>(count);
        }

        public bool Add(T1 key, T2 value)
        {
            bool alreadyExist = m_Data.ContainsKey(key);
            m_Data[key] = value;
            m_Version++;
            return !alreadyExist;
        }

        internal bool Remove(T1 key)
        {
            bool removed = m_Data.Remove(key);
            if (removed)
                m_Version++;
            return removed;
        }

        public T2[] Values
        {
            get
            {
                if (m_ArrayVersion == m_Version)
                    return m_Array ?? Array.Empty<T2>();
                m_Array = new T2[m_Data.Count];
                m_Data.Values.CopyTo(m_Array, 0);
                m_ArrayVersion = m_Version;
                return m_Array;
            }
        }

        public T2 this[T1 key]
        {
            get
            {
                if (!m_Data.TryGetValue(key, out T2 value))
                    throw new System.ArgumentOutOfRangeException($"Key {key.ToString()} not found for FrameData");
                return value;
            }
            set
            {
                if (m_Array != null && m_Data.TryGetValue(key, out T2 oldValue))
                {
                    for (int i = 0; i < m_Array.Length; ++i)
                    {
                        if (m_Array[i].Equals(oldValue))
                        {
                            m_Array[i] = value;
                            break;
                        }
                    }
                }
                m_Data[key] = value;
            }
        }

        public bool TryGetValue(T1 key, out T2 value)
        {
            return m_Data.TryGetValue(key, out value);
        }

        public bool ContainsKey(T1 key)
        {
            return m_Data.ContainsKey(key);
        }

        public IEnumerable<KeyValuePair<T1,T2>> Enumerate()
        {
            foreach (var pair in m_Data)
            {
                yield return pair;
            }
        }
    }
}

#endif
