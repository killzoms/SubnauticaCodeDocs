using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class EcoRegionManager
    {
        private static EcoRegionManager _main;

        private const int kNumXZRegions = 256;

        private const int kNumYRegions = 128;

        private Int3 regionBoundsMax = new Int3(255, 127, 255);

        private float kRegionSize = 16f;

        private float kMaxAboveWaterHeight = 100f;

        private Bounds ecoRegionsBounds;

        private Dictionary<Int3, EcoRegion> regionMap = new Dictionary<Int3, EcoRegion>();

        private EcoRegion cameraRegion;

        private Int3 cameraRegionIndices;

        private EcoRegionPool regionPool;

        private Vector3 nearestPos;

        public static EcoRegionManager main
        {
            get
            {
                if (_main == null)
                {
                    _main = new EcoRegionManager();
                }
                return _main;
            }
        }

        public static void Deinitialize()
        {
            _main = null;
        }

        private EcoRegionManager()
        {
            ProfilingUtils.BeginSample("EcoRegionManager.Start");
            float num = 256f * kRegionSize * 0.5f;
            float num2 = 128f * kRegionSize * 0.5f;
            ecoRegionsBounds = default(Bounds);
            ecoRegionsBounds.center = new Vector3(0f, kMaxAboveWaterHeight - num2, 0f);
            ecoRegionsBounds.extents = new Vector3(num, num2, num);
            regionPool = new EcoRegionPool();
            ProfilingUtils.EndSample();
        }

        private EcoRegion CreateRegion(Int3 pos)
        {
            ProfilingUtils.BeginSample("EcoRegionManager.CreateRegion");
            EcoRegion ecoRegion = regionPool.Get();
            Vector3 cornerPos = new Vector3(pos.x, pos.y, pos.z) * kRegionSize + ecoRegionsBounds.min;
            ecoRegion.Initialize(cornerPos, kRegionSize, pos);
            ProfilingUtils.EndSample();
            return ecoRegion;
        }

        private bool CheckBounds(Int3 index)
        {
            if (index.x >= 0 && index.x < 256 && index.y >= 0 && index.y < 128 && index.z >= 0)
            {
                return index.z < 256;
            }
            return false;
        }

        private EcoRegion GetInitializedRegion(Int3 index)
        {
            try
            {
                if (!CheckBounds(index))
                {
                    return null;
                }
                EcoRegion value = null;
                if (!regionMap.TryGetValue(index, out value))
                {
                    value = CreateRegion(index);
                    regionMap.Add(index, value);
                }
                return value;
            }
            finally
            {
            }
        }

        private bool GetNearestRegionXYZ(Vector3 pos, Vector3 offset, out Int3 index)
        {
            nearestPos = ecoRegionsBounds.ClosestPoint(pos + offset);
            return GetRegionXYZ(nearestPos, out index);
        }

        private bool GetRegionXYZ(Vector3 pos, out Int3 index)
        {
            bool result = false;
            if (ecoRegionsBounds.Contains(pos))
            {
                index.x = (int)((pos.x - ecoRegionsBounds.min.x) / kRegionSize);
                index.y = (int)((pos.y - ecoRegionsBounds.min.y) / kRegionSize);
                index.z = (int)((pos.z - ecoRegionsBounds.min.z) / kRegionSize);
                index = index.Clamp(Int3.zero, regionBoundsMax);
                result = true;
            }
            else
            {
                index = Int3.zero;
            }
            return result;
        }

        public EcoRegion GetRegionIfExists(Int3 index)
        {
            EcoRegion value = null;
            if (CheckBounds(index))
            {
                regionMap.TryGetValue(index, out value);
            }
            return value;
        }

        public EcoRegion GetRegion(Vector3 pos, EcoRegion currentRegion = null)
        {
            EcoRegion result = null;
            if (GetRegionXYZ(pos, out var index))
            {
                if (currentRegion != null && index == currentRegion.listIndices)
                {
                    return currentRegion;
                }
                result = GetInitializedRegion(index);
            }
            return result;
        }

        private void UpdateCameraRegion()
        {
            ProfilingUtils.BeginSample("EcoRegionManager.UpdateCameraRegion");
            if (GetRegionXYZ(MainCamera.camera.transform.position, out var index) && index != cameraRegionIndices)
            {
                ProfilingUtils.BeginSample("EcoRegionManager.Region");
                EcoRegion initializedRegion = GetInitializedRegion(index);
                if (initializedRegion != null)
                {
                    cameraRegion = initializedRegion;
                    cameraRegionIndices = index;
                    cameraRegion.DrawDebug(Color.green, Time.deltaTime, depthTest: false);
                }
                ProfilingUtils.EndSample();
            }
            ProfilingUtils.EndSample();
        }

        private void OnRegionChanged(EcoRegion region)
        {
        }

        public void EcoUpdate()
        {
            ProfilingUtils.BeginSample("EcoRegionManager.EcoUpdate");
            UpdateCameraRegion();
            regionPool.Update();
            ProfilingUtils.EndSample();
        }

        public IEcoTarget FindNearestTargetPhysicsQuery(EcoTargetType type, Vector3 position, float radius, EcoRegion.TargetFilter filter, ref float outTargetDistanceSqr, ref Collider outCollider)
        {
            int num = global::UWE.Utils.OverlapSphereIntoSharedBuffer(position, radius);
            IEcoTarget result = null;
            float num2 = float.MaxValue;
            for (int i = 0; i < num; i++)
            {
                IEcoTarget component = global::UWE.Utils.sharedColliderBuffer[i].gameObject.GetComponent<IEcoTarget>();
                if (component != null && component.GetTargetType() == type && (filter == null || filter(component)))
                {
                    float sqrMagnitude = (global::UWE.Utils.sharedColliderBuffer[i].transform.position - position).sqrMagnitude;
                    if (sqrMagnitude < num2)
                    {
                        result = component;
                        num2 = (outTargetDistanceSqr = sqrMagnitude);
                        outCollider = global::UWE.Utils.sharedColliderBuffer[i];
                    }
                }
            }
            return result;
        }

        public IEcoTarget FindNearestTarget(EcoTargetType type, Vector3 wsPos, EcoRegion.TargetFilter isTargetValid = null, int maxRings = 1)
        {
            ProfilingUtils.BeginSample("EcoRegionManager.FindNearestTarget");
            try
            {
                if (!GetRegionXYZ(wsPos, out var index))
                {
                    return null;
                }
                float num = float.MaxValue;
                IEcoTarget ecoTarget = null;
                int num2 = 0;
                for (int i = 0; i <= maxRings && (ecoTarget == null || i <= num2 + 1); i++)
                {
                    RingWalker3D ringWalker3D = new RingWalker3D(i);
                    while (ringWalker3D.MoveNext())
                    {
                        Int3 current = ringWalker3D.Current;
                        Int3 index2 = index + current;
                        EcoRegion regionIfExists = GetRegionIfExists(index2);
                        if (regionIfExists != null)
                        {
                            float bestDist = float.MaxValue;
                            IEcoTarget best = null;
                            regionIfExists.FindNearestTarget(type, wsPos, isTargetValid, ref bestDist, ref best);
                            if (bestDist < num)
                            {
                                num = bestDist;
                                ecoTarget = best;
                                num2 = i;
                            }
                        }
                    }
                }
                return ecoTarget;
            }
            finally
            {
                ProfilingUtils.EndSample();
            }
        }
    }
}
