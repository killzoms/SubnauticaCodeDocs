using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class GasoPod : Creature
    {
        public Transform gasPodSpawn;

        public GameObject podPrefab;

        public GameObject gasFXprefab;

        private float podRandomForce = 1f;

        private float podBaseForce = 1f;

        private float timeLastGasPodDrop = -20000f;

        private float podSpawnDist = 0.27f;

        private float scaredTriggerValue = 0.75f;

        private float minTimeBetweenPayloads = 10f;

        private void DropGasPods()
        {
            int num = Random.Range(6, 10);
            for (int i = 1; i <= num; i++)
            {
                GameObject gameObject = Object.Instantiate(podPrefab);
                Vector3 vector = Random.onUnitSphere;
                vector.z = (0f - Mathf.Abs(vector.z)) * 3f;
                gameObject.transform.position = gasPodSpawn.TransformPoint(vector * podSpawnDist);
                vector = gasPodSpawn.TransformDirection(vector);
                gameObject.GetComponent<Rigidbody>().AddForce(vector * (podBaseForce + Random.value * podRandomForce), ForceMode.VelocityChange);
                if ((bool)LargeWorldStreamer.main)
                {
                    LargeWorldStreamer.main.MakeEntityTransient(gameObject);
                }
            }
            if (gasFXprefab != null)
            {
                Utils.SpawnZeroedAt(gasFXprefab, gasPodSpawn);
            }
        }

        public void Update()
        {
            Player main = Player.main;
            if (timeLastGasPodDrop + minTimeBetweenPayloads <= Time.time && Scared.Value >= scaredTriggerValue && (bool)main && main.CanBeAttacked())
            {
                timeLastGasPodDrop = Time.time;
                DropGasPods();
            }
        }
    }
}
