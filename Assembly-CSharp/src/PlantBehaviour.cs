using UnityEngine;

namespace AssemblyCSharp
{
    public class PlantBehaviour : Living, IOnTakeDamage
    {
        private float kScareDistance = 3f;

        private Animator animator;

        private bool _scared;

        private bool scared
        {
            get
            {
                return _scared;
            }
            set
            {
                if (value != _scared)
                {
                    _scared = value;
                    if (animator != null)
                    {
                        SafeAnimator.SetBool(animator, "scared", value);
                    }
                }
            }
        }

        public virtual void OnDisable()
        {
            if (scared)
            {
                CancelInvoke("StopScared");
            }
        }

        public virtual void Start()
        {
            animator = GetComponentInChildren<Animator>();
        }

        private void Plant_MotionEventEmitter(EcoEvent e)
        {
            ProfilingUtils.BeginSample("PlantBehaviour.Plant_MotionEventEmitter");
            scared = true;
            Invoke("StopScared", 5f);
            ProfilingUtils.EndSample();
        }

        private void StopScared()
        {
            scared = false;
        }

        public void OnTakeDamage(DamageInfo damageInfo)
        {
        }
    }
}
