using UnityEngine;

namespace AssemblyCSharp
{
    public class LastScarePosition : MonoBehaviour
    {
        private float _lastScareTime;

        private Vector3 _lastScarePosition;

        public float lastScareTime => _lastScareTime;

        public Vector3 lastScarePosition
        {
            get
            {
                return _lastScarePosition;
            }
            set
            {
                _lastScarePosition = value;
                _lastScareTime = Time.time;
            }
        }
    }
}
