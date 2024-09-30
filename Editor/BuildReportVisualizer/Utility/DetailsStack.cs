#if UNITY_2022_2_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEditor.AddressableAssets.BuildReportVisualizer
{
    internal static class DetailsStack
    {
        public static int Count => m_Stack.Count;
        public static Action<DetailsContents> OnPop;
        public static Action<DetailsContents> OnPush;
        static Stack<DetailsContents> m_Stack = new Stack<DetailsContents>();

        public static void Push(DetailsContents item)
        {
            m_Stack.Push(item);
            OnPush(item);
        }

        public static void Pop()
        {
            if (m_Stack.Count == 0)
                return;

            DetailsContents item = m_Stack.Pop();
            OnPop(item);
        }

        public static void Clear()
        {
            m_Stack.Clear();
        }
    }
}
#endif
