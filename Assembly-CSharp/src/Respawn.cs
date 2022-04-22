using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class Respawn : MonoBehaviour
    {
        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 1;

        [NonSerialized]
        [ProtoMember(2)]
        public float spawnTime = -1f;

        [NonSerialized]
        [ProtoMember(3)]
        public TechType techType;

        [NonSerialized]
        [ProtoMember(4)]
        public readonly List<string> addComponents = new List<string>();

        private void Start()
        {
            if (DayNightCycle.main.timePassed >= (double)spawnTime && spawnTime >= 0f)
            {
                Spawn();
                global::UnityEngine.Object.Destroy(base.gameObject);
            }
        }

        private void Spawn()
        {
            int num = global::UWE.Utils.OverlapSphereIntoSharedBuffer(base.transform.position, 1.5f);
            for (int i = 0; i < num; i++)
            {
                if (global::UWE.Utils.sharedColliderBuffer[i].GetComponentInParent<Base>() != null)
                {
                    return;
                }
            }
            GameObject gameObject = CraftData.InstantiateFromPrefab(techType);
            gameObject.transform.position = base.transform.position;
            gameObject.transform.rotation = base.transform.rotation;
            for (int j = 0; j < addComponents.Count; j++)
            {
                Type type = Type.GetType(addComponents[j]);
                if (type != null)
                {
                    gameObject.AddComponent(type);
                }
            }
            gameObject.SetActive(value: true);
            if (base.transform.parent == null || base.transform.parent.GetComponentInParent<LargeWorldEntity>() == null)
            {
                if ((bool)LargeWorldStreamer.main)
                {
                    LargeWorldStreamer.main.cellManager.RegisterEntity(gameObject);
                }
                return;
            }
            if ((bool)LargeWorldStreamer.main)
            {
                LargeWorldStreamer.main.cellManager.UnregisterEntity(gameObject);
            }
            gameObject.transform.parent = base.transform.parent;
        }
    }
}
