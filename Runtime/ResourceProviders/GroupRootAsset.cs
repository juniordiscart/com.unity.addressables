#if ENABLE_CONTENT_DIRECTORIES
using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceProviders;


namespace UnityEngine.AddressableAssets.ResourceProviders
{
    [CreateAssetMenu(fileName = "GroupRootAsset", menuName = "Addressables/Group Root Asset")]
    public class GroupRootAsset : ScriptableObject, IAddressableRootAsset
    {
        Dictionary<string, LoadableInfo> m_CachedLookup;

        [SerializeField]
        private List<LoadableInfo> m_LoadableInfos = new List<LoadableInfo>();

        public List<LoadableInfo> Assets { get => m_LoadableInfos; set => m_LoadableInfos = value; }

        string m_Key;
        public string Key
        {
            get { return m_Key; }
            set { m_Key = value; }
        }

        public LoadableInfo GetLoadableInfo(string key, Type assetType)
        {
            if (m_CachedLookup == null)
            {
                m_CachedLookup = new Dictionary<string, LoadableInfo>();

                foreach (var info in m_LoadableInfos)
                {
                    if (info.type == null)
                        continue;

                    m_CachedLookup[info.key + info.type.FullName] = info;
                    m_CachedLookup[info.guid + info.type.FullName] = info;

                    foreach(var label in info.labels)
                    {
                        m_CachedLookup[label + info.type.FullName] = info;
                    }
                }
            }

            //combining the type info here because I think technically we allow building with identical keys across different types? If that's wrong, I'll pull that part.
            m_CachedLookup.TryGetValue(key + assetType.FullName, out var loadableInfo);
            return loadableInfo;
        }

        public List<LoadableInfo> GetAllSubAssets(string parentKey, Type subassetType)
        {
            var results = new List<LoadableInfo>();
            int leftBracketIndex = parentKey.IndexOf('[');
            if(leftBracketIndex < 0)
            {
                // If the parent key doesn't contain a '[', it can't have subassets in the expected format, so return an empty list.
                return results;
            }

            string searchPattern = parentKey.Substring(0, leftBracketIndex) + "[";
            foreach (var info in m_LoadableInfos)
            {
                // Check if this is a subasset of the parent (format: "ParentKey[SubAssetName]")
                if (info.key.StartsWith(searchPattern) && info.type == subassetType)
                {
                    results.Add(info);
                }
            }

            return results;
        }
    }
}
#endif
