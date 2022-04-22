using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Collider))]
    public class ObservatoryAmbientSound : MonoBehaviour
    {
        public SphereCollider trigger;

        private static bool inObservatory;

        public static bool IsPlayerInObservatory()
        {
            return inObservatory;
        }

        private void Start()
        {
            InvokeRepeating("CheckTriggerExit", Random.value, 10f);
        }

        private void OnTriggerEnter(Collider c)
        {
            if (c.gameObject == Player.mainObject)
            {
                inObservatory = true;
            }
        }

        private void OnTriggerExit(Collider c)
        {
            if (c.gameObject == Player.mainObject)
            {
                inObservatory = false;
            }
        }

        private void CheckTriggerExit()
        {
            if (!inObservatory)
            {
                return;
            }
            GameObject mainObject = Player.mainObject;
            if ((bool)mainObject)
            {
                Vector3 position = mainObject.transform.position;
                if (!global::UWE.Utils.IsInsideCollider(trigger, position))
                {
                    Debug.LogWarning("OnTriggerExit failed. Fixing.", this);
                    inObservatory = false;
                }
            }
        }
    }
}
