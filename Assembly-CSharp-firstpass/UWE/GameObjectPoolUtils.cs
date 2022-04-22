using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace UWE
{
    public static class GameObjectPoolUtils
    {
        private static Stopwatch sw = new Stopwatch();

        private static double sTotalInstTime = 0.0;

        private static LinkedList<double> sInstTimeBuffer = new LinkedList<double>();

        private static double sAvgInstantiateTime = 0.0;

        public static bool PoolsEnabled { get; private set; }

        public static double AvgInstantiateTime => sAvgInstantiateTime;

        public static void OnPoolsEnabledToggled(bool value)
        {
            PoolsEnabled = value;
            if (!PoolsEnabled)
            {
                GameObjectPool.ClearPools();
            }
            sTotalInstTime = 0.0;
            sAvgInstantiateTime = 0.0;
            GameObjectPool.HitCount = 0;
            sInstTimeBuffer.Clear();
        }

        public static GameObject InstantiateWrap(GameObject prefab, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion))
        {
            sw.Reset();
            sw.Start();
            GameObject gameObject = null;
            gameObject = (PoolsEnabled ? GameObjectPool.Instantiate(prefab, position, rotation) : Object.Instantiate(prefab, position, rotation));
            sw.Stop();
            if (sInstTimeBuffer.Count < 1000)
            {
                sInstTimeBuffer.AddFirst(sw.Elapsed.TotalMilliseconds);
                sTotalInstTime += sw.Elapsed.TotalMilliseconds;
            }
            else
            {
                sTotalInstTime -= sInstTimeBuffer.Last.Value;
                sTotalInstTime += sw.Elapsed.TotalMilliseconds;
                sInstTimeBuffer.RemoveLast();
                sInstTimeBuffer.AddFirst(sw.Elapsed.TotalMilliseconds);
            }
            sAvgInstantiateTime = sTotalInstTime / (double)sInstTimeBuffer.Count;
            return gameObject;
        }

        private static GameObject InstantiateDeactivatedImpl(GameObject prefab, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion))
        {
            sw.Reset();
            sw.Start();
            GameObject gameObject = null;
            gameObject = ((!PoolsEnabled) ? Utils.InstantiateDeactivated(prefab, position, rotation) : GameObjectPool.Instantiate(prefab, position, rotation, active: false));
            sw.Stop();
            if (sInstTimeBuffer.Count < 1000)
            {
                sInstTimeBuffer.AddFirst(sw.Elapsed.TotalMilliseconds);
                sTotalInstTime += sw.Elapsed.TotalMilliseconds;
            }
            else
            {
                sTotalInstTime -= sInstTimeBuffer.Last.Value;
                sTotalInstTime += sw.Elapsed.TotalMilliseconds;
                sInstTimeBuffer.RemoveLast();
                sInstTimeBuffer.AddFirst(sw.Elapsed.TotalMilliseconds);
            }
            sAvgInstantiateTime = sTotalInstTime / (double)sInstTimeBuffer.Count;
            return gameObject;
        }

        public static void DestroyWrap(Object o, float time = 0f)
        {
            if (Application.isPlaying)
            {
                if (!PoolsEnabled)
                {
                    Object.Destroy(o, time);
                    return;
                }
                GameObject gameObject = o as GameObject;
                if ((bool)gameObject)
                {
                    GameObjectPool.Return(gameObject, time);
                }
                else
                {
                    Object.Destroy(o, time);
                }
            }
            else
            {
                Object.DestroyImmediate(o);
            }
        }
    }
}
