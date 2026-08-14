using System;
using System.Linq;
using UnityEditor;

namespace UnityEngine.ResourceManagement.Util
{
    [ExecuteInEditMode]
    internal abstract class InternalComponentSingleton<T> : MonoBehaviour where T : InternalComponentSingleton<T>
    {
        static T s_Instance;

        /// <summary>
        /// Indicates whether or not there is an existing instance of the singleton.
        /// </summary>
        public static bool Exists => s_Instance != null;

        /// <summary>
        /// Stores the instance of the singleton.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindInstance() ?? CreateNewSingleton();
                }

                return s_Instance;
            }
        }

        static T FindInstance()
        {
#if UNITY_EDITOR
            foreach (T cb in Resources.FindObjectsOfTypeAll(typeof(T)).Cast<T>())
            {
                var go = cb.gameObject;
                if (!EditorUtility.IsPersistent(go.transform.root.gameObject) && !(go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave))
                    return cb;
            }

            return null;
#else
            return FindFirstObjectByType<T>();
#endif
        }

        /// <summary>
        /// Retrieves the name of the object.
        /// </summary>
        /// <returns>Returns the name of the object.</returns>
        protected virtual string GetGameObjectName() => typeof(T).Name;

        static T CreateNewSingleton()
        {
            var go = new GameObject();

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.DontSave;
            }
            else
            {
                go.hideFlags = HideFlags.HideAndDontSave;
            }

            var instance = go.AddComponent<T>();
            go.name = instance.GetGameObjectName();
            return instance;
        }

        protected virtual void Awake()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += PlayModeChanged;
#endif
            if (s_Instance != null && s_Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }

            s_Instance = this as T;
        }

        /// <summary>
        /// Destroys the singleton.
        /// </summary>
        public static void DestroySingleton()
        {
            if (Exists)
            {
                DestroyImmediate(Instance.gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= PlayModeChanged;
#endif
            if(s_Instance == this)
                s_Instance = null;
        }

#if UNITY_EDITOR
        void PlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode)
            {
                if (Exists && Instance == this)
                {
                    DestroyImmediate(Instance.gameObject);
                }
            }
        }

#endif
    }

    /// <summary>
    /// Creates a singleton.
    /// </summary>
    /// <typeparam name="T">The singleton type.</typeparam>
    [ExecuteInEditMode]
    [Obsolete("This class was used for internal tooling and is not supported anymore.")]
    public abstract class ComponentSingleton<T> : MonoBehaviour where T : ComponentSingleton<T>
    {
        static T s_Instance;

        /// <summary>
        /// Indicates whether or not there is an existing instance of the singleton.
        /// </summary>
        public static bool Exists => s_Instance != null;

        /// <summary>
        /// Stores the instance of the singleton.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindInstance() ?? CreateNewSingleton();
                }

                return s_Instance;
            }
        }


        static T FindInstance()
        {
#if UNITY_EDITOR
            foreach (T cb in Resources.FindObjectsOfTypeAll(typeof(T)).Cast<T>())
            {
                var go = cb.gameObject;
                if (!EditorUtility.IsPersistent(go.transform.root.gameObject) && !(go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave))
                    return cb;
            }

            return null;
#else
            return FindAnyObjectByType<T>();
#endif
        }

        /// <summary>
        /// Retrieves the name of the object.
        /// </summary>
        /// <returns>Returns the name of the object.</returns>
        protected virtual string GetGameObjectName() => typeof(T).Name;

        static T CreateNewSingleton()
        {
            var go = new GameObject();

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.DontSave;
            }
            else
            {
                go.hideFlags = HideFlags.HideAndDontSave;
            }

            var instance = go.AddComponent<T>();
            go.name = instance.GetGameObjectName();
            return instance;
        }

        private void Awake()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += PlayModeChanged;
#endif
            if (s_Instance != null && s_Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }

            s_Instance = this as T;
        }

        /// <summary>
        /// Destroys the singleton.
        /// </summary>
        public static void DestroySingleton()
        {
            if (Exists)
            {
                DestroyImmediate(Instance.gameObject);
            }
        }

        void OnDestroy()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= PlayModeChanged;
#endif
            if(s_Instance == this)
                s_Instance = null;
        }

#if UNITY_EDITOR
        void PlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode)
            {
                if (Exists && Instance == this)
                {
                    DestroyImmediate(Instance.gameObject);
                }
            }
        }

#endif
    }
}
