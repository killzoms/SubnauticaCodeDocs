using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(LiveMixin))]
    public class BreakFall : MonoBehaviour, IOnTakeDamage
    {
        public GameObject damageParticlePrefab;

        public FMOD_StudioEventEmitter killSoundEvent;

        private void Awake()
        {
            base.gameObject.GetComponent<Rigidbody>().isKinematic = true;
        }

        public void OnTakeDamage(DamageInfo damageInfo)
        {
            if ((bool)damageParticlePrefab)
            {
                Utils.PlayOneShotPS(damageParticlePrefab, base.transform.position, base.transform.rotation);
            }
        }

        private void OnKill()
        {
            GetComponent<Rigidbody>().isKinematic = false;
            base.transform.parent = null;
            if ((bool)killSoundEvent)
            {
                Utils.PlayEnvSound(killSoundEvent);
            }
        }
    }
}
