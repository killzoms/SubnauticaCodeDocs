using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class WaterSlotGenerator
    {
        private class Material
        {
            public static readonly Material generic = new Material(0f, 360f, "*");

            private float minInclination;

            private float maxInclination;

            private string material;

            public Material(float minInclination, float maxInclination, string material)
            {
                this.minInclination = minInclination;
                this.maxInclination = maxInclination;
                this.material = material;
            }

            public bool ContainsInclination(float inclination)
            {
                if (inclination >= minInclination)
                {
                    return inclination <= maxInclination;
                }
                return false;
            }

            public string GetMaterial()
            {
                return material;
            }
        }

        private class GeneratorRule
        {
            private string biome;

            private bool biomeWildcard;

            private float probability;

            private float minDepth;

            private float maxDepth;

            private float terrainDistance;

            private float propDistance;

            private float slotDistance;

            private string prefabPath;

            private const string pathPrefix = "WorldEntities/Slots";

            public GeneratorRule(string biome, float probability, float minDepth, float maxDepth, float terrainDistance, float propDistance, float slotDistance, string prefabPath)
            {
                this.biome = biome.Replace("*", "");
                biomeWildcard = biome.Contains("*");
                this.probability = probability;
                this.minDepth = minDepth;
                this.maxDepth = maxDepth;
                this.terrainDistance = terrainDistance;
                this.propDistance = propDistance;
                this.slotDistance = slotDistance;
                this.prefabPath = prefabPath;
            }

            public bool ShouldSpawn(Vector3 position, float depth, string voxelBiome, Dictionary<Int3, List<EntitySlot>> spatialSlots)
            {
                if (global::UnityEngine.Random.value > probability)
                {
                    return false;
                }
                if (depth < minDepth || depth > maxDepth)
                {
                    if (debug)
                    {
                        Debug.Log("failed depth test");
                    }
                    return false;
                }
                if (!Match(voxelBiome))
                {
                    return false;
                }
                if (terrainDistance >= 0f)
                {
                    int terrainLayerMask = Voxeland.GetTerrainLayerMask();
                    int num = global::UWE.Utils.OverlapSphereIntoSharedBuffer(position, terrainDistance, terrainLayerMask);
                    for (int i = 0; i < num; i++)
                    {
                        Collider collider = global::UWE.Utils.sharedColliderBuffer[i];
                        if (!collider.isTrigger)
                        {
                            if (debug)
                            {
                                Debug.Log(string.Concat(position, " blocked off by terrain"), collider);
                            }
                            return false;
                        }
                    }
                }
                if (propDistance >= 0f)
                {
                    int terrainLayerMask2 = Voxeland.GetTerrainLayerMask();
                    int num2 = global::UWE.Utils.OverlapSphereIntoSharedBuffer(position, propDistance, ~terrainLayerMask2);
                    for (int j = 0; j < num2; j++)
                    {
                        Collider collider2 = global::UWE.Utils.sharedColliderBuffer[j];
                        if (!collider2.isTrigger)
                        {
                            if (debug)
                            {
                                Debug.Log(string.Concat(position, " blocked off by collider"), collider2);
                            }
                            return false;
                        }
                    }
                }
                if (slotDistance >= 0f)
                {
                    bool flag = IsCreatureSlot();
                    Vector3 pos = position - Vector3.one * slotDistance;
                    Vector3 pos2 = position + Vector3.one * slotDistance;
                    Int3 bucketIndex = GetBucketIndex(pos);
                    Int3 bucketIndex2 = GetBucketIndex(pos2);
                    foreach (Int3 item in Int3.Range(bucketIndex, bucketIndex2))
                    {
                        if (!spatialSlots.TryGetValue(item, out var value))
                        {
                            continue;
                        }
                        for (int k = 0; k < value.Count; k++)
                        {
                            EntitySlot entitySlot = value[k];
                            if (entitySlot == null || entitySlot.IsCreatureSlot() != flag)
                            {
                                continue;
                            }
                            Vector3 position2 = entitySlot.transform.position;
                            Vector3 vector = position - position2;
                            if (vector.sqrMagnitude < slotDistance * slotDistance)
                            {
                                if (debug)
                                {
                                    Debug.Log(string.Concat(position, " blocked off by nearby slot at ", position2, " (dist ", vector.magnitude, ")"), entitySlot);
                                }
                                return false;
                            }
                        }
                    }
                }
                return true;
            }

            public GameObject Spawn(Vector3 position, Transform parent)
            {
                Quaternion rotation = Quaternion.Euler(0f, global::UnityEngine.Random.Range(0, 360), 0f);
                if (!TryLoadPrefab(out var prefab))
                {
                    Debug.LogErrorFormat("Could not load prefab at '{0}'.", prefabPath);
                    return null;
                }
                return global::UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
            }

            private bool Match(string voxelBiome)
            {
                if (!biomeWildcard)
                {
                    return string.Equals(voxelBiome, biome, StringComparison.OrdinalIgnoreCase);
                }
                return voxelBiome.IndexOf(biome, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private bool TryLoadPrefab(out GameObject prefab)
            {
                return PrefabDatabase.TryGetPrefabForFilename(Path.Combine("WorldEntities/Slots", prefabPath).Replace('\\', '/'), out prefab);
            }

            private bool IsCreatureSlot()
            {
                if (!TryLoadPrefab(out var prefab))
                {
                    return false;
                }
                EntitySlot component = prefab.GetComponent<EntitySlot>();
                if (!component)
                {
                    return false;
                }
                return component.IsCreatureSlot();
            }
        }

        public const string slotDistributionsPath = "Resources/Balance/WaterSlotDistributions.csv";

        public static bool debug;

        private const float bucketSize = 5f;

        private AtmosphereVolume[] volumes;

        private Dictionary<Int3, List<EntitySlot>> spatialSlots;

        private List<GeneratorRule> generatorRules;

        private bool csvLoaded;

        public void Initialize(Bounds wsBounds, bool loadCSVs)
        {
            volumes = (from p in global::UnityEngine.Object.FindObjectsOfType<AtmosphereVolume>().Where(DefinesBiome)
                where Overlaps(p, wsBounds)
                orderby p.priority descending
                select p).ToArray();
            generatorRules = new List<GeneratorRule>();
            if (loadCSVs)
            {
                foreach (WaterSlotDistribution item in CSVUtils.Load<WaterSlotDistribution>(Path.Combine(Application.dataPath, "Resources/Balance/WaterSlotDistributions.csv")))
                {
                    AddSlotDistribution(item);
                }
                csvLoaded = true;
            }
            EntitySlot[] array = global::UnityEngine.Object.FindObjectsOfType<EntitySlot>();
            spatialSlots = new Dictionary<Int3, List<EntitySlot>>();
            for (int i = 0; i < array.Length; i++)
            {
                RegisterSlot(array[i]);
            }
        }

        public void AddSlotDistribution(WaterSlotDistribution d)
        {
            GeneratorRule item = new GeneratorRule(d.biome, d.probability, d.minDepth, d.maxDepth, d.terrainDistance, d.propDistance, d.slotDistance, d.slotFilename);
            generatorRules.Add(item);
        }

        public int OnVoxel(Vector3 wsPos, Transform root = null)
        {
            int num = 0;
            string voxelBiome = "SafeShallows";
            if ((bool)LargeWorld.main)
            {
                voxelBiome = LargeWorld.main.GetBiome(wsPos);
            }
            for (int i = 0; i < volumes.Length; i++)
            {
                AtmosphereVolume atmosphereVolume = volumes[i];
                if (atmosphereVolume.Contains(wsPos))
                {
                    if (debug)
                    {
                        Debug.Log("hit biome " + atmosphereVolume.overrideBiome, atmosphereVolume);
                    }
                    voxelBiome = atmosphereVolume.overrideBiome;
                    break;
                }
            }
            for (int j = 0; j < generatorRules.Count; j++)
            {
                GeneratorRule generatorRule = generatorRules[j];
                if (generatorRule.ShouldSpawn(wsPos, 0f - wsPos.y, voxelBiome, spatialSlots))
                {
                    GameObject gameObject = generatorRule.Spawn(wsPos, root);
                    if ((bool)gameObject)
                    {
                        EntitySlot component = gameObject.GetComponent<EntitySlot>();
                        component.autoGenerated = true;
                        RegisterSlot(component);
                        num++;
                    }
                }
            }
            return num;
        }

        private void RegisterSlot(EntitySlot slot)
        {
            Int3 bucketIndex = GetBucketIndex(slot.transform.position);
            spatialSlots.GetOrAddNew(bucketIndex).Add(slot);
        }

        private static Int3 GetBucketIndex(Vector3 pos)
        {
            float x = pos.x / 5f;
            float y = pos.y / 5f;
            float z = pos.z / 5f;
            return Int3.Floor(x, y, z);
        }

        private static bool DefinesBiome(AtmosphereVolume volume)
        {
            return !string.IsNullOrEmpty(volume.overrideBiome);
        }

        private static bool Overlaps(AtmosphereVolume volume, Bounds wsBounds)
        {
            Collider component = volume.GetComponent<Collider>();
            if ((bool)component)
            {
                return component.bounds.Intersects(wsBounds);
            }
            return false;
        }
    }
}
