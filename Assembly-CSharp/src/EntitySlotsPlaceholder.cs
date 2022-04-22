using System;
using AssemblyCSharp.UWE;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [AddComponentMenu("")]
    [ProtoContract]
    public class EntitySlotsPlaceholder : MonoBehaviour, ISerializeInEditMode
    {
        private const int currentVersion = 1;

        [AssertNotNull]
        public GameObject virtualEntityPrefab;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 1;

        [NonSerialized]
        [ProtoMember(2, OverwriteList = true)]
        public EntitySlotData[] slotsData;

        private void Start()
        {
            Spawn();
            global::UWE.Utils.DestroyWrap(base.gameObject);
        }

        private void Spawn()
        {
            if (slotsData == null)
            {
                return;
            }
            virtualEntityPrefab.SetActive(value: false);
            for (int i = 0; i < slotsData.Length; i++)
            {
                EntitySlotData entitySlotData = slotsData[i];
                bool spawnedAny = false;
                EntitySlot.Filler prefabForSlot = LargeWorld.main.streamer.cellManager.GetPrefabForSlot(entitySlotData);
                if (!string.IsNullOrEmpty(prefabForSlot.classId))
                {
                    if (!WorldEntityDatabase.TryGetInfo(prefabForSlot.classId, out var info))
                    {
                        Debug.LogErrorFormat(this, "Missing world entity info for prefab '{0}'", prefabForSlot.classId);
                        continue;
                    }
                    ProfilingUtils.BeginSample("spawn virtual entities");
                    for (int j = 0; j < prefabForSlot.count; j++)
                    {
                        Vector3 localPosition = entitySlotData.localPosition;
                        if (j > 0)
                        {
                            localPosition += global::UnityEngine.Random.insideUnitSphere * 4f;
                        }
                        Quaternion localRotation = entitySlotData.localRotation;
                        if (info.prefabZUp)
                        {
                            localRotation *= Quaternion.Euler(new Vector3(-90f, 0f, 0f));
                        }
                        GameObject obj = global::UWE.Utils.InstantiateDeactivated(virtualEntityPrefab, base.transform, localPosition, localRotation, info.localScale);
                        obj.GetComponent<VirtualPrefabIdentifier>().ClassId = prefabForSlot.classId;
                        LargeWorldEntity component = obj.GetComponent<LargeWorldEntity>();
                        component.cellLevel = info.cellLevel;
                        bool active = false;
                        ProfilingUtils.BeginSample("register");
                        if (LargeWorld.main != null)
                        {
                            active = LargeWorld.main.streamer.cellManager.RegisterEntity(component);
                        }
                        ProfilingUtils.EndSample();
                        obj.SetActive(active);
                        spawnedAny = true;
                    }
                    ProfilingUtils.EndSample();
                }
                if (EntitySlot.debugSlots)
                {
                    ProfilingUtils.BeginSample("debugSlots");
                    bool flag = entitySlotData.IsCreatureSlot();
                    GameObject gameObject = GameObject.CreatePrimitive((!flag) ? PrimitiveType.Cube : PrimitiveType.Sphere);
                    gameObject.SetActive(value: false);
                    gameObject.name = $"{entitySlotData.biomeType} ghost (density {entitySlotData.density}, count {prefabForSlot.count}, id {prefabForSlot.classId})";
                    gameObject.transform.parent = base.transform.parent;
                    gameObject.transform.localPosition = entitySlotData.localPosition;
                    gameObject.transform.localRotation = entitySlotData.localRotation;
                    gameObject.transform.localScale = (flag ? new Vector3(0.5f, 0.5f, 0.5f) : new Vector3(0.2f, 2f, 0.2f));
                    gameObject.transform.SetParent(null, worldPositionStays: true);
                    global::UnityEngine.Object.Destroy(gameObject.GetComponent<Collider>());
                    gameObject.GetComponent<Renderer>().sharedMaterial = EntitySlot.GetGhostMaterial(spawnedAny);
                    gameObject.SetActive(value: true);
                    ProfilingUtils.EndSample();
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = base.transform.localToWorldMatrix;
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(Vector3.zero, 0.5f);
            if (slotsData == null)
            {
                return;
            }
            for (int i = 0; i < slotsData.Length; i++)
            {
                EntitySlotData entitySlotData = slotsData[i];
                Gizmos.color = Color.white;
                Gizmos.DrawLine(Vector3.zero, entitySlotData.localPosition);
                Gizmos.color = Color.blue;
                if (entitySlotData.IsCreatureSlot())
                {
                    Gizmos.DrawSphere(entitySlotData.localPosition, 1f);
                }
                else
                {
                    Gizmos.DrawCube(entitySlotData.localPosition + new Vector3(0f, 0.5f, 0f), Vector3.one);
                }
            }
        }
    }
}
