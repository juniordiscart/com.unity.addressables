using JetBrains.Annotations;
using System;
using UnityEditor;
using UnityEngine;

namespace AutoGroupGenerator
{
    /// <summary>
    /// Wrapper for EditorPrefs-backed values that survive domain reloads.
    /// </summary>
    /// <typeparam name="T">A type that can be serialized by the <see cref="JsonUtility"/> class.</typeparam>
    public struct EditorPersistentValue<T>
    {
        #region Fields
        private readonly string _persistenceKey;

        private readonly Action _onValueChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the persisted value.
        /// </summary>
        public T Value
        {
            get => Load();
            set
            {
                if (object.Equals(Load(), value))
                {
                    return;
                }


                Save(value);

                _onValueChanged?.Invoke();
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Initializes a new persisted value.
        /// </summary>
        /// <param name="defaultValue">Default value used when no data is stored.</param>
        /// <param name="persistenceKey">EditorPrefs key used for persistence.</param>
        /// <param name="onValueChanged">Callback invoked when the value changes.</param>
        public EditorPersistentValue(T defaultValue, [NotNull] string persistenceKey, Action onValueChanged = null)
        {
            _persistenceKey = persistenceKey;
            _onValueChanged = onValueChanged;

        }

        private void Save(T value)
        {
            try
            {
                string jsonValue = JsonUtility.ToJson(value);

                EditorPrefs.SetString(_persistenceKey, jsonValue);
            }
            catch (Exception e)
            {
                Debug.LogError($"Save failed!: {e}");
            }
        }

        private T Load()
        {
            try
            {
                if (EditorPrefs.HasKey(_persistenceKey))
                {
                    string jsonValue = EditorPrefs.GetString(_persistenceKey);

                    T value = JsonUtility.FromJson<T>(jsonValue);

                    return value;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Load failed: {e}");
            }


            return default;
        }

        /// <summary>
        /// Removes the persisted data from EditorPrefs.
        /// </summary>
        public void ClearPersistentData()
        {
            EditorPrefs.DeleteKey(_persistenceKey);
        }
        #endregion
    }
}
