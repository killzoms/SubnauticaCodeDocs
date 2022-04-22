using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class EntitySlotGenerator
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

            private string material;

            private float probability;

            private float minInclination;

            private float maxInclination;

            private float propDistance;

            private float slotDistance;

            private float terrainOffset;

            private string prefabPath;

            private const string pathPrefix = "WorldEntities/Slots";

            public GeneratorRule(string biome, string material, float probability, float minInclination, float maxInclination, float propDistance, float slotDistance, float terrainOffset, string prefabPath)
            {
                this.biome = biome.Replace("*", "");
                biomeWildcard = biome.Contains("*");
                this.material = material;
                this.probability = probability;
                this.minInclination = minInclination;
                this.maxInclination = maxInclination;
                this.propDistance = propDistance;
                this.slotDistance = slotDistance;
                this.terrainOffset = terrainOffset;
                this.prefabPath = prefabPath;
            }

            public bool ShouldSpawn(Vector3 position, Vector3 normal, float inclination, string voxelBiome, string voxelMaterial, Dictionary<Int3, List<EntitySlot>> spatialSlots)
            {
                if (global::UnityEngine.Random.value > probability)
                {
                    return false;
                }
                if (inclination < minInclination || inclination > maxInclination)
                {
                    if (debug)
                    {
                        Debug.Log("failed inclination test");
                    }
                    return false;
                }
                if (!Match(voxelBiome, voxelMaterial))
                {
                    return false;
                }
                Vector3 vector = position + normal * terrainOffset;
                if (terrainOffset > 0f && Physics.Linecast(position, vector, Voxeland.GetTerrainLayerMask()))
                {
                    if (debug)
                    {
                        Debug.Log(string.Concat(position, " popped into terrain"));
                    }
                    return false;
                }
                if (terrainOffset < 0f && Physics.Linecast(vector, position, Voxeland.GetTerrainLayerMask()))
                {
                    if (debug)
                    {
                        Debug.Log(string.Concat(position, " popped out of underground"));
                    }
                    return false;
                }
                if (propDistance >= 0f)
                {
                    int terrainLayerMask = Voxeland.GetTerrainLayerMask();
                    int num = global::UWE.Utils.OverlapSphereIntoSharedBuffer(vector, propDistance, ~terrainLayerMask);
                    for (int i = 0; i < num; i++)
                    {
                        Collider collider = global::UWE.Utils.sharedColliderBuffer[i];
                        if (!collider.isTrigger)
                        {
                            if (debug)
                            {
                                Debug.Log(string.Concat(position, " blocked off by collider"), collider);
                            }
                            return false;
                        }
                    }
                }
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
                    for (int j = 0; j < value.Count; j++)
                    {
                        EntitySlot entitySlot = value[j];
                        if (entitySlot == null || entitySlot.IsCreatureSlot() != flag)
                        {
                            continue;
                        }
                        Vector3 position2 = entitySlot.transform.position;
                        Vector3 vector2 = vector - position2;
                        if (vector2.sqrMagnitude < slotDistance * slotDistance)
                        {
                            if (debug)
                            {
                                Debug.Log(string.Concat(position, " blocked off by nearby slot at ", position2, " (dist ", vector2.magnitude, ")"), entitySlot);
                            }
                            return false;
                        }
                    }
                }
                return true;
            }

            public GameObject Spawn(Vector3 position, Vector3 normal)
            {
                Vector3 position2 = position + normal * terrainOffset;
                Quaternion identity = Quaternion.identity;
                identity.SetFromToRotation(Vector3.up, normal);
                identity *= Quaternion.AngleAxis(global::UnityEngine.Random.Range(0, 360), Vector3.up);
                if (!TryLoadPrefab(out var prefab))
                {
                    Debug.LogErrorFormat("Could not load prefab at '{0}'.", prefabPath);
                    return null;
                }
                return global::UnityEngine.Object.Instantiate(prefab, position2, identity);
            }

            private bool Match(string voxelBiome, string voxelMaterial)
            {
                if (!string.Equals(voxelMaterial, material, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
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

        public const string blockClassificationsPath = "Resources/Balance/BlockTypeClassifications.csv";

        public const string slotDistributionsPath = "Resources/Balance/EntitySlotDistributions.csv";

        public static bool debug = false;

        private const float bucketSize = 5f;

        private static readonly List<Material> emptyList = new List<Material>();

        private AtmosphereVolume[] volumes;

        private Dictionary<Int3, List<EntitySlot>> spatialSlots;

        private Dictionary<int, List<Material>> blockTypeClassification;

        private List<GeneratorRule> generatorRules;

        private bool csvLoaded;

        public static int DeleteAutoGeneratedSlots(Bounds wsBounds)
        {
            int num = 0;
            int num2 = 0;
            EntitySlot[] array = global::UnityEngine.Object.FindObjectsOfType<EntitySlot>();
            foreach (EntitySlot entitySlot in array)
            {
                if (wsBounds.Contains(entitySlot.transform.position))
                {
                    num2++;
                    if (entitySlot.autoGenerated)
                    {
                        global::UWE.Utils.DestroyWrap(entitySlot.gameObject);
                        num++;
                    }
                }
            }
            Debug.Log("Deleted " + num + " of " + num2 + " existing slots.");
            return num;
        }

        public void Initialize(Bounds wsBounds, bool loadCSVs)
        {
            volumes = (from p in global::UnityEngine.Object.FindObjectsOfType<AtmosphereVolume>().Where(DefinesBiome)
                where Overlaps(p, wsBounds)
                orderby p.priority descending
                select p).ToArray();
            blockTypeClassification = new Dictionary<int, List<Material>>();
            generatorRules = new List<GeneratorRule>();
            if (loadCSVs)
            {
                foreach (BlockTypeClassification item in CSVUtils.Load<BlockTypeClassification>(Path.Combine(Application.dataPath, "Resources/Balance/BlockTypeClassifications.csv")))
                {
                    AddBlockClassification(item);
                }
                foreach (EntitySlotDistribution item2 in CSVUtils.Load<EntitySlotDistribution>(Path.Combine(Application.dataPath, "Resources/Balance/EntitySlotDistributions.csv")))
                {
                    AddSlotDistribution(item2);
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

        public void AddBlockClassification(BlockTypeClassification c)
        {
            blockTypeClassification.GetOrAddNew(c.blockType).Add(new Material(c.minInclination, c.maxInclination, c.material));
        }

        public void AddSlotDistribution(EntitySlotDistribution d)
        {
            GeneratorRule item = new GeneratorRule(d.biome, d.material, d.probability, d.minInclination, d.maxInclination, d.propDistance, d.slotDistance, d.terrainOffset, d.slotFilename);
            generatorRules.Add(item);
        }

        public int OnVoxel(Vector3 wsPos, Vector3 wsNormal, int blockType, Transform root = null)
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
            float inclination = Vector3.Angle(wsNormal, Vector3.up);
            List<Material> orDefault = blockTypeClassification.GetOrDefault(blockType, emptyList);
            for (int j = -1; j < orDefault.Count; j++)
            {
                Material material = ((j < 0) ? Material.generic : orDefault[j]);
                if (!material.ContainsInclination(inclination))
                {
                    continue;
                }
                for (int k = 0; k < generatorRules.Count; k++)
                {
                    GeneratorRule generatorRule = generatorRules[k];
                    if (generatorRule.ShouldSpawn(wsPos, wsNormal, inclination, voxelBiome, material.GetMaterial(), spatialSlots))
                    {
                        GameObject gameObject = generatorRule.Spawn(wsPos, wsNormal);
                        if ((bool)gameObject)
                        {
                            EntitySlot component = gameObject.GetComponent<EntitySlot>();
                            component.autoGenerated = true;
                            RegisterSlot(component);
                            gameObject.transform.parent = root;
                            num++;
                        }
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
