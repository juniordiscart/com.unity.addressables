using System;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;

internal class MonoBehaviourCallbackHooks : ComponentSingleton<MonoBehaviourCallbackHooks>
{
    internal Action<float> m_OnUpdateDelegate;
    internal Action<float> m_OnLateUpdateDelegate;

    public event Action<float> OnUpdateDelegate
    {
        add { m_OnUpdateDelegate += value; }

        remove { m_OnUpdateDelegate -= value; }
    }

    internal event Action<float> OnLateUpdateDelegate
    {
        add { m_OnLateUpdateDelegate += value; }

        remove { m_OnLateUpdateDelegate -= value; }
    }

    protected override string GetGameObjectName() => "ResourceManagerCallbacks";

    // Update is called once per frame
    internal void Update()
    {
        m_OnUpdateDelegate?.Invoke(Time.unscaledDeltaTime);
    }

    internal void LateUpdate()
    {
        m_OnLateUpdateDelegate?.Invoke(Time.unscaledDeltaTime);
    }
}
