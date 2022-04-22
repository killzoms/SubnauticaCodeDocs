using UnityEngine;

namespace AssemblyCSharp
{
    public class OnGroundTracker : OnSurfaceTracker
    {
        private Vector3 _groundPoint;

        public override Vector3 surfaceNormal => _surfaceNormal;

        public Vector3 groundPoint => _groundPoint;

        private bool CheckIsGround(Collision collisionInfo)
        {
            bool result = false;
            ContactPoint[] contacts = collisionInfo.contacts;
            if (collisionInfo.collider.GetComponentInParent<Voxeland>() != null)
            {
                for (int i = 0; i < contacts.Length; i++)
                {
                    if (Vector3.Dot(Vector3.up, contacts[i].normal) >= minSurfaceCos)
                    {
                        result = true;
                        _surfaceNormal = contacts[i].normal;
                        _groundPoint = contacts[i].point;
                        if (Application.isEditor)
                        {
                            Debug.DrawLine(contacts[i].point, contacts[i].point + 3f * contacts[i].normal, Color.white);
                        }
                        break;
                    }
                }
            }
            return result;
        }

        private void OnCollisionEnter(Collision collision)
        {
            _onSurface = CheckIsGround(collision);
        }

        protected override void OnCollisionStay(Collision collision)
        {
            _onSurface = CheckIsGround(collision);
        }
    }
}
