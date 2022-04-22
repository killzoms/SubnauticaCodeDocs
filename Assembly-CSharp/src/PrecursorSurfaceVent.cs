using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class PrecursorSurfaceVent : PrecursorVentBase, IProtoTreeEventListener
    {
        [AssertNotNull]
        public ChildObjectIdentifier storageRoot;

        public float minStoreDuration = 3f;

        public float maxStoreDuration = 30f;

        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 1;

        [NonSerialized]
        [ProtoMember(2)]
        public float timeNextEmit = -1f;

        private readonly List<Peeper> storedPeepers = new List<Peeper>();

        private void Update()
        {
            ProfilingUtils.BeginSample("PrecursorSurfaceVent.Update()");
            if (timeNextEmit < 0f || DayNightUtils.time < timeNextEmit)
            {
                ProfilingUtils.EndSample();
                return;
            }
            EmitPeeper();
            ProfilingUtils.EndSample();
        }

        protected override void StorePeeper(Peeper peeper)
        {
            peeper.gameObject.SetActive(value: false);
            peeper.transform.SetParent(storageRoot.transform, worldPositionStays: false);
            storedPeepers.Add(peeper);
            if (timeNextEmit < 0f)
            {
                timeNextEmit = DayNightUtils.time + global::UnityEngine.Random.Range(minStoreDuration, maxStoreDuration);
            }
        }

        protected override Peeper RetrievePeeper()
        {
            if (storedPeepers.Count < 1)
            {
                Debug.LogWarningFormat(this, "Precursor surface vent can not emit a peeper because none are stored anymore");
                return null;
            }
            timeNextEmit = -1f;
            int index = storedPeepers.Count - 1;
            Peeper peeper = storedPeepers[index];
            storedPeepers.RemoveAt(index);
            if (storedPeepers.Count > 0)
            {
                timeNextEmit = DayNightUtils.time + global::UnityEngine.Random.Range(minStoreDuration, maxStoreDuration);
            }
            peeper.BecomeHero();
            return peeper;
        }

        public void OnProtoSerializeObjectTree(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
            storageRoot.GetComponentsInChildren(includeInactive: true, storedPeepers);
        }
    }
}
