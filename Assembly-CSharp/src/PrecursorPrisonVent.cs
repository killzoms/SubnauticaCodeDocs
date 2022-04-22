using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class PrecursorPrisonVent : PrecursorVentBase
    {
        [AssertNotNull]
        public GameObject prisonPeeperPrefab;

        public float minEmitInterval = 5f;

        public float maxEmitInterval = 30f;

        public float leashDistance = 20f;

        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 1;

        [NonSerialized]
        [ProtoMember(2)]
        public int numStoredPeepers = 20;

        private void Start()
        {
            TryEmitPeeper();
        }

        private void TryEmitPeeper()
        {
            if (numStoredPeepers > 0)
            {
                EmitPeeper();
            }
            Invoke("TryEmitPeeper", global::UnityEngine.Random.Range(minEmitInterval, maxEmitInterval));
        }

        protected override void StorePeeper(Peeper peeper)
        {
            numStoredPeepers++;
            global::UnityEngine.Object.Destroy(peeper.gameObject);
        }

        protected override Peeper RetrievePeeper()
        {
            if (numStoredPeepers < 1)
            {
                Debug.LogWarningFormat(this, "Precursor prison vent can not emit a peeper because none are stored anymore");
                return null;
            }
            numStoredPeepers--;
            Peeper component = global::UnityEngine.Object.Instantiate(prisonPeeperPrefab).GetComponent<Peeper>();
            component.leashPosition = base.transform.TransformPoint(new Vector3(0f, leashDistance, 0f));
            component.isInitialized = true;
            component.InitializeInPrison();
            return component;
        }
    }
}
