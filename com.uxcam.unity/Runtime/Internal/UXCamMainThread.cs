using System;
using System.Collections.Generic;
using UnityEngine;

namespace UXCamSDK.Internal
{
    internal static class UXCamMainThread
    {
        static readonly Queue<Action> s_Queue = new Queue<Action>();
        static readonly List<Action> s_Batch = new List<Action>();

        internal static void Post(Action action)
        {
            if (action == null) return;
            lock (s_Queue) s_Queue.Enqueue(action);
        }

        internal static void Drain()
        {
            lock (s_Queue)
            {
                if (s_Queue.Count == 0) return;
                s_Batch.AddRange(s_Queue);
                s_Queue.Clear();
            }

            foreach (var action in s_Batch)
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            s_Batch.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            lock (s_Queue) s_Queue.Clear();
            s_Batch.Clear();
        }
    }
}
