using UnityEngine;

namespace AssemblyCSharp
{
    public class LastTarget : MonoBehaviour
    {
        private float _targetTime;

        private GameObject _target;

        private bool _targetLocked;

        public float targetTime => _targetTime;

        public GameObject target
        {
            get
            {
                return _target;
            }
            set
            {
                if (!_targetLocked || !(value != _target))
                {
                    SetTarget(value);
                }
            }
        }

        public bool targetLocked => _targetLocked;

        protected virtual void SetTarget(GameObject target)
        {
            _target = target;
            _targetTime = Time.time;
        }

        public void SetLockedTarget(GameObject target)
        {
            SetTarget(target);
            _targetLocked = true;
        }

        public void UnlockTarget()
        {
            _targetLocked = false;
        }
    }
}
