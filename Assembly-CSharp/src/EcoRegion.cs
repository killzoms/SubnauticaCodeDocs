using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class EcoRegion
    {
        public delegate bool TargetFilter(IEcoTarget target);

        public Vector3 position;

        public float dimension;

        public Int3 listIndices = Int3.zero;

        public float timeStamp;

        private Dictionary<int, HashSet<IEcoTarget>> ecoTargets = new Dictionary<int, HashSet<IEcoTarget>>();

        private static ObjectPool<HashSet<IEcoTarget>> ecoTargetHashSetPool = ObjectPoolHelper.CreatePool<HashSet<IEcoTarget>>(512);

        public bool Empty()
        {
            return ecoTargets.Count == 0;
        }

        public void Initialize(Vector3 cornerPos, float dim, Int3 regionIndex)
        {
            position = cornerPos;
            dimension = dim;
            listIndices = regionIndex;
            timeStamp = Time.time;
        }

        public void RegisterTarget(IEcoTarget target)
        {
            ProfilingUtils.BeginSample("EcoRegion.RegisterTarget");
            timeStamp = Time.time;
            int targetType = (int)target.GetTargetType();
            if (!ecoTargets.TryGetValue(targetType, out var value))
            {
                value = ecoTargetHashSetPool.Get();
                ecoTargets.Add(targetType, value);
            }
            value.Add(target);
            ProfilingUtils.EndSample();
        }

        public void UnregisterTarget(IEcoTarget target)
        {
            ProfilingUtils.BeginSample("EcoRegion.UnregisterTarget");
            timeStamp = Time.time;
            int targetType = (int)target.GetTargetType();
            if (ecoTargets.TryGetValue(targetType, out var value))
            {
                value.Remove(target);
                if (value.Count == 0)
                {
                    ecoTargets.Remove(targetType);
                    ecoTargetHashSetPool.Return(value);
                }
            }
            ProfilingUtils.EndSample();
        }

        public void DrawDebug(Color c, float time, bool depthTest = true, int numGrid = 1)
        {
            Utils.DebugDrawAABB(position, position + new Vector3(dimension, 0f - dimension, dimension), numGrid, c, time, depthTest);
        }

        public void FindNearestTarget(EcoTargetType type, Vector3 wsPos, TargetFilter isTargetValid, ref float bestDist, ref IEcoTarget best)
        {
            ProfilingUtils.BeginSample("EcoRegion.FindNearestTarget");
            timeStamp = Time.time;
            if (!ecoTargets.TryGetValue((int)type, out var value))
            {
                ProfilingUtils.EndSample();
                return;
            }
            float num = float.MaxValue;
            HashSet<IEcoTarget>.Enumerator enumerator = value.GetEnumerator();
            while (enumerator.MoveNext())
            {
                IEcoTarget current = enumerator.Current;
                if (current != null && !current.Equals(null))
                {
                    float sqrMagnitude = (wsPos - current.GetPosition()).sqrMagnitude;
                    if (!(sqrMagnitude >= num) && (isTargetValid == null || isTargetValid(current)))
                    {
                        best = current;
                        num = sqrMagnitude;
                    }
                }
            }
            if (best != null)
            {
                bestDist = Mathf.Sqrt(num);
            }
            ProfilingUtils.EndSample();
        }

        public void LogDebugInfo()
        {
            foreach (KeyValuePair<int, HashSet<IEcoTarget>> ecoTarget in ecoTargets)
            {
                string text = "";
                foreach (IEcoTarget item in ecoTarget.Value)
                {
                    text = text + item.GetName() + ", ";
                }
                Debug.Log("Ents target for '" + ecoTarget.Key + "': " + text);
            }
        }
    }
}
