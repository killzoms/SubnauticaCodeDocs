using System;
using UnityEngine;

namespace AssemblyCSharp
{
    public class OnSurfaceTracker : MonoBehaviour
    {
        protected bool _onSurface;

        protected Vector3 _surfaceNormal;

        [Range(0f, 180f)]
        public float maxSurfaceAngle = 60f;

        protected float minSurfaceCos;

        public bool onSurface => _onSurface;

        public virtual Vector3 surfaceNormal
        {
            get
            {
                if (!_onSurface)
                {
                    return Vector3.up;
                }
                return _surfaceNormal;
            }
        }

        private void Awake()
        {
            minSurfaceCos = Mathf.Cos((float)Math.PI / 180f * maxSurfaceAngle);
        }

        protected virtual void OnCollisionStay(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Creature") && !collision.gameObject.CompareTag("Player") && collision.contacts[0].normal.y >= minSurfaceCos)
            {
                _surfaceNormal = collision.contacts[0].normal;
                _onSurface = true;
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            _onSurface = false;
        }
    }
}
