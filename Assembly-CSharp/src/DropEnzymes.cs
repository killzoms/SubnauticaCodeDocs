using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class DropEnzymes : MonoBehaviour, IProtoTreeEventListener
    {
        [AssertNotNull]
        public GameObject enzymePrefab;

        [AssertNotNull]
        public Animator animator;

        [AssertNotNull]
        public Transform cureBallAttach;

        public FMODAsset enzymeDropSound;

        public int spawnLimit = -1;

        [NonSerialized]
        [ProtoMember(1)]
        public float timeNextDrop = -1f;

        [NonSerialized]
        [ProtoMember(2)]
        public bool firstDrop = true;

        private GameObject spawnedCureBall;

        private int spawnCount;

        private void Start()
        {
            SetNextDropTime();
        }

        private void SetNextDropTime()
        {
            timeNextDrop = DayNightCycle.main.timePassedAsFloat + (float)global::UnityEngine.Random.Range(7, 13);
        }

        private void Update()
        {
            if (DayNightCycle.main.timePassed > (double)timeNextDrop)
            {
                StartDropAnimation();
                spawnCount++;
                if (spawnLimit > 0 && spawnCount >= spawnLimit)
                {
                    base.enabled = false;
                }
            }
        }

        private void OnDisable()
        {
            ReleaseSpawnedCureBall();
        }

        private void StartDropAnimation()
        {
            if (spawnedCureBall != null)
            {
                ReleaseSpawnedCureBall();
            }
            SafeAnimator.SetBool(animator, "Burp_ball", value: true);
            SafeAnimator.SetBool(animator, "Burp_first_time", firstDrop);
            firstDrop = false;
            SetNextDropTime();
        }

        public void SpawnCureBall()
        {
            spawnedCureBall = global::UnityEngine.Object.Instantiate(enzymePrefab, cureBallAttach.position, cureBallAttach.rotation);
            spawnedCureBall.transform.SetParent(cureBallAttach, worldPositionStays: true);
            LargeWorldEntity component = spawnedCureBall.GetComponent<LargeWorldEntity>();
            if ((bool)component && (bool)LargeWorldStreamer.main)
            {
                LargeWorldStreamer.main.cellManager.UnregisterEntity(component);
            }
            Rigidbody component2 = spawnedCureBall.GetComponent<Rigidbody>();
            if (component2 != null)
            {
                component2.isKinematic = true;
            }
            if (enzymeDropSound != null)
            {
                Utils.PlayFMODAsset(enzymeDropSound, base.transform);
            }
        }

        public void ReleaseSpawnedCureBall()
        {
            SafeAnimator.SetBool(animator, "Burp_ball", value: false);
            SafeAnimator.SetBool(animator, "Burp_first_time", value: false);
            if ((bool)spawnedCureBall)
            {
                spawnedCureBall.transform.parent = null;
                LargeWorldEntity component = spawnedCureBall.GetComponent<LargeWorldEntity>();
                if ((bool)component && (bool)LargeWorldStreamer.main)
                {
                    LargeWorldStreamer.main.cellManager.RegisterEntity(component);
                }
                Rigidbody component2 = spawnedCureBall.GetComponent<Rigidbody>();
                if (component2 != null)
                {
                    component2.isKinematic = false;
                }
                spawnedCureBall = null;
            }
        }

        public void OnProtoSerializeObjectTree(ProtobufSerializer serializer)
        {
            if (spawnedCureBall != null)
            {
                ReleaseSpawnedCureBall();
            }
        }

        public void OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
        }
    }
}
