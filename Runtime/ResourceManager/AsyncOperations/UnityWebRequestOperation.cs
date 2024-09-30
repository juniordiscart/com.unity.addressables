using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEngine.ResourceManagement.AsyncOperations
{
    internal class UnityWebRequestOperation : AsyncOperationBase<UnityWebRequest>
    {
        UnityWebRequest m_UWR;

        public UnityWebRequestOperation(UnityWebRequest webRequest)
        {
            m_UWR = webRequest;
        }

        protected override void Execute()
        {
            m_UWR.SendWebRequest().completed += (request) => { Complete(m_UWR, string.IsNullOrEmpty(m_UWR.error), m_UWR.error); };
        }
    }
}
