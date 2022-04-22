using System;
using System.Collections;
using System.Collections.Generic;
using AssemblyCSharp.UWE;
using ProtoBuf;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class EntitySlot : MonoBehaviour, IEntitySlot, ICompileTimeCheckable
    {
        public enum Type
        {
            Small,
            Medium,
            Large,
            Tall,
            Creature
        }

        public struct Filler
        {
            public string classId;

            public int count;
        }

        public static bool debugSlots = false;

        public static readonly Int3[] TypeSizes = new Int3[5]
        {
            new Int3(1, 1, 1),
            new Int3(2, 2, 2),
            new Int3(4, 4, 4),
            new Int3(1, 4, 1),
            new Int3(1, 1, 1)
        };

        [ProtoMember(2, OverwriteList = true)]
        [LegacyData]
        public List<Type> allowedTypes = new List<Type>();

        [ProtoMember(4)]
        public BiomeType biomeType;

        [NonSerialized]
        [ProtoMember(6)]
        public bool autoGenerated;

        [NonSerialized]
        [ProtoMember(7)]
        public float density = 1f;

        private bool beingDestroyedAfterUse;

        private bool spawnCalled;

        private static Material spawnedGhostMaterial;

        private static Material emptyGhostMaterial;

        public Int3 GetMaxSize()
        {
            Int3 @int = new Int3(0, 0, 0);
            foreach (Type allowedType in allowedTypes)
            {
                @int = Int3.Max(@int, TypeSizes.GetClamped((int)allowedType));
            }
            return @int;
        }

        public BiomeType GetBiomeType()
        {
            return biomeType;
        }

        public bool IsTypeAllowed(Type t)
        {
            return allowedTypes.Contains(t);
        }

        public float GetDensity()
        {
            return density;
        }

        public bool IsCreatureSlot()
        {
            return IsTypeAllowed(Type.Creature);
        }

        public void CalculateDensity(List<Vector3> slotPositions, float radius)
        {
            Vector3 position = base.transform.position;
            float num = radius * radius;
            density = 0f;
            for (int i = 0; i < slotPositions.Count; i++)
            {
                Vector3 vector = slotPositions[i];
                float num2 = Vector3.SqrMagnitude(position - vector);
                if (!(num2 > num))
                {
                    float num3 = num2 / num;
                    float num4 = 1f - num3;
                    density += 0.9375f * (num4 * num4);
                }
            }
        }

        private void OnDisable()
        {
            if (spawnCalled && !beingDestroyedAfterUse)
            {
                Debug.LogWarning("EntitySlot was disabled but not due to self-destruct!");
            }
        }

        private IEnumerator Start()
        {
            while (LargeWorldStreamer.main.cellManager.IsProcessing())
            {
                yield return new WaitForSeconds(0.5f + global::UnityEngine.Random.value);
            }
            SpawnVirtualEntities();
            beingDestroyedAfterUse = true;
            global::UnityEngine.Object.Destroy(base.gameObject);
        }

        private void SpawnVirtualEntities()
        {
            bool spawnedAny = false;
            spawnCalled = true;
            GameObject virtualEntityPrefab = VirtualEntitiesManager.GetVirtualEntityPrefab();
            virtualEntityPrefab.SetActive(value: false);
            Filler filler = GetFiller(this);
            if (!string.IsNullOrEmpty(filler.classId))
            {
                if (!WorldEntityDatabase.TryGetInfo(filler.classId, out var info))
                {
                    Debug.LogErrorFormat(this, "Missing world entity info for prefab '{0}'", filler.classId);
                }
                else
                {
                    ProfilingUtils.BeginSample("spawn virtual entities");
                    Vector3 localPosition = base.transform.localPosition;
                    Quaternion localRotation = base.transform.localRotation;
                    for (int i = 0; i < filler.count; i++)
                    {
                        if (i > 0)
                        {
                            localPosition += global::UnityEngine.Random.insideUnitSphere * 4f;
                        }
                        if (info.prefabZUp)
                        {
                            localRotation *= Quaternion.Euler(new Vector3(-90f, 0f, 0f));
                        }
                        GameObject obj = global::UnityEngine.Object.Instantiate(virtualEntityPrefab, localPosition, localRotation);
                        obj.transform.SetParent(base.transform.parent, worldPositionStays: false);
                        obj.transform.localScale = info.localScale;
                        obj.GetComponent<VirtualPrefabIdentifier>().ClassId = filler.classId;
                        LargeWorldEntity component = obj.GetComponent<LargeWorldEntity>();
                        component.cellLevel = info.cellLevel;
                        bool active = false;
                        ProfilingUtils.BeginSample("register");
                        if (LargeWorldStreamer.main != null)
                        {
                            active = LargeWorldStreamer.main.cellManager.RegisterEntity(component);
                        }
                        ProfilingUtils.EndSample();
                        obj.SetActive(active);
                        spawnedAny = true;
                    }
                    ProfilingUtils.EndSample();
                }
            }
            if (debugSlots)
            {
                ProfilingUtils.BeginSample("debugSlots");
                bool flag = IsCreatureSlot();
                GameObject obj2 = GameObject.CreatePrimitive((!flag) ? PrimitiveType.Cube : PrimitiveType.Sphere);
                obj2.SetActive(value: false);
                obj2.name = $"{biomeType} Ghost ({density})";
                obj2.transform.position = base.transform.position;
                obj2.transform.rotation = base.transform.rotation;
                obj2.transform.localScale = (flag ? new Vector3(0.5f, 0.5f, 0.5f) : new Vector3(0.2f, 2f, 0.2f));
                obj2.transform.SetParent(null, worldPositionStays: true);
                global::UnityEngine.Object.Destroy(obj2.GetComponent<Collider>());
                obj2.GetComponent<Renderer>().sharedMaterial = GetGhostMaterial(spawnedAny);
                obj2.SetActive(value: true);
                ProfilingUtils.EndSample();
            }
        }

        private static Filler GetFiller(EntitySlot slot)
        {
            LargeWorldStreamer main = LargeWorldStreamer.main;
            if (!main || main.cellManager == null)
            {
                Debug.LogErrorFormat(slot, "Missing cell manager for entity slot '{0}'", slot.name);
                return default(Filler);
            }
            return main.cellManager.GetPrefabForSlot(slot);
        }

        private static Material CreateGhostMaterial(Color color)
        {
            return new Material(Shader.Find("Unlit/Color"))
            {
                color = color.ToAlpha(0.25f)
            };
        }

        public static Material GetGhostMaterial(bool spawnedAny)
        {
            if (!spawnedGhostMaterial)
            {
                spawnedGhostMaterial = CreateGhostMaterial(Color.green);
                emptyGhostMaterial = CreateGhostMaterial(Color.red);
            }
            if (!spawnedAny)
            {
                return emptyGhostMaterial;
            }
            return spawnedGhostMaterial;
        }

        public static void UpdateDensitiesForAllSlots(Bounds wsBounds)
        {
            Timer.Begin("UpdateDensitiesForAllSlots");
            EntitySlot[] array = global::UnityEngine.Object.FindObjectsOfType<EntitySlot>();
            List<EntitySlot> list = new List<EntitySlot>();
            Dictionary<BiomeType, List<Vector3>> dictionary = new Dictionary<BiomeType, List<Vector3>>();
            Dictionary<BiomeType, List<Vector3>> dictionary2 = new Dictionary<BiomeType, List<Vector3>>();
            Bounds bounds = wsBounds;
            bounds.Expand(32f);
            using (Progress progress = new Progress("sorting slots slots", array.Length))
            {
                EntitySlot[] array2 = array;
                foreach (EntitySlot entitySlot in array2)
                {
                    if (progress.Tic())
                    {
                        break;
                    }
                    Vector3 position = entitySlot.transform.position;
                    if (bounds.Contains(position))
                    {
                        (entitySlot.IsCreatureSlot() ? dictionary2 : dictionary).GetOrAddNew(entitySlot.biomeType).Add(position);
                        if (wsBounds.Contains(position))
                        {
                            list.Add(entitySlot);
                        }
                    }
                }
            }
            List<Vector3> defaultValue = new List<Vector3>();
            using (Progress progress2 = new Progress("density comp for slots", list.Count))
            {
                foreach (EntitySlot item in list)
                {
                    if (progress2.Tic())
                    {
                        break;
                    }
                    List<Vector3> orDefault = (item.IsCreatureSlot() ? dictionary2 : dictionary).GetOrDefault(item.biomeType, defaultValue);
                    item.CalculateDensity(orDefault, 32f);
                }
            }
            Timer.End();
        }

        public string CompileTimeCheck()
        {
            if (GetComponentsInParent<Tile>(includeInactive: true).Length != 0)
            {
                return null;
            }
            if (!GetComponent<PrefabIdentifier>())
            {
                return "EntitySlot must be on root of prefab. Use hierarchical prefabs for hierarchical entity slots instead.";
            }
            if (GetComponentsInChildren<EntitySlot>(includeInactive: true).Length != 1)
            {
                return "Only one entity slot allowed per prefab. Use hierarchical prefabs for multiple entity slots instead.";
            }
            return null;
        }
    }
}
