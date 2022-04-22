using UnityEngine;

namespace AssemblyCSharp
{
    public class AcidicBrineDamage : MonoBehaviour
    {
        private int numTriggers;

        private void Start()
        {
            InvokeRepeating("ApplyDamage", 0f, 1f);
            SendMessage("OnAcidEnter", null, SendMessageOptions.DontRequireReceiver);
        }

        private void OnDestroy()
        {
            SendMessage("OnAcidExit", null, SendMessageOptions.DontRequireReceiver);
        }

        public void Increment()
        {
            numTriggers++;
        }

        public void Decrement()
        {
            numTriggers--;
            if (numTriggers <= 0)
            {
                Object.Destroy(this);
            }
        }

        private void ApplyDamage()
        {
            GetComponent<LiveMixin>().TakeDamage(10f, base.transform.position, DamageType.Acid);
        }
    }
}
