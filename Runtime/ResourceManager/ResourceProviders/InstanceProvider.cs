using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.SceneManagement;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Basic implementation of IInstanceProvider.
    /// </summary>
    public class InstanceProvider : IInstanceProvider
    {
        Dictionary<GameObject, AsyncOperationHandle<GameObject>> m_InstanceObjectToPrefabHandle = new Dictionary<GameObject, AsyncOperationHandle<GameObject>>();

        /// <summary>
        /// Provide an instance of a loaded GameObject
        /// </summary>
        /// <param name="resourceManager">The resource manager to use</param>
        /// <param name="prefabHandle">The operation handle with a reference to the object to instantiate</param>
        /// <param name="instantiateParameters">Container for data to specficy how to instantiate</param>
        /// <returns>The instance GameObject</returns>
        public GameObject ProvideInstance(ResourceManager resourceManager, AsyncOperationHandle<GameObject> prefabHandle, InstantiationParameters instantiateParameters)
        {
            GameObject result = instantiateParameters.Instantiate(prefabHandle.Result);
            m_InstanceObjectToPrefabHandle.Add(result, prefabHandle);
            return result;
        }

        /// <summary>
        /// Destroy the instance and release one ref count on the operaiton handle
        /// </summary>
        /// <param name="resourceManager">The resource manager used to instantiate the GameObject</param>
        /// <param name="instance">The instance GameObject to destroy</param>
        public void ReleaseInstance(ResourceManager resourceManager, GameObject instance)
        {
            // Guard for null - note that Unity overloads equality for GameObject so `default(GameObject) == null` is true so must use explicit `is null` type guard
            if (instance is null)
                return;

            AsyncOperationHandle<GameObject> resource;
            if (!m_InstanceObjectToPrefabHandle.TryGetValue(instance, out resource))
            {
                Debug.LogWarningFormat("Releasing unknown GameObject {0} to InstanceProvider.", instance);
            }
            else
            {
                resource.Release();
                m_InstanceObjectToPrefabHandle.Remove(instance);
            }

            if (instance != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(instance);
                else
                    Object.DestroyImmediate(instance);
            }
        }
    }
}
