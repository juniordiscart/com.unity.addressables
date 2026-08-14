#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Generic;
using Unity.Loading;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    [Serializable]
    public class LoadableInfo
    {
        [SerializeField]
        private SerializedType m_SerializedType;
        private Type m_CachedType;

        public string guid;
        public string key;
        public List<string> labels = new List<string>();

        //I don't love that these are two separate things. Afaik there's no way to combine these
        //in a way that isn't more complex than just having both.
        public Loadable<Object> loadable;
        public LoadableSceneId scene;

        public bool IsScene
        {
            get
            {
                return scene != default;
            }
        }

        public Type type
        {
            get
            {
                if (m_CachedType == null && m_SerializedType.Value != null)
                    m_CachedType = m_SerializedType.Value;
                return m_CachedType;
            }
            set
            {
                m_CachedType = value;
                m_SerializedType = new SerializedType() { Value = value };
            }
        }
    }
}
#endif
