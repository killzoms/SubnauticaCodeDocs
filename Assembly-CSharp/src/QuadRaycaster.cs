using UnityEngine;

namespace AssemblyCSharp
{
    public class QuadRaycaster : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            float radiusScale = global::UWE.Utils.GetRadiusScale(base.transform.lossyScale);
            float radius = 0.04f * radiusScale;
            Vector3 direction = base.transform.forward * radiusScale;
            for (int i = -5; i <= 5; i++)
            {
                for (int j = -5; j <= 5; j++)
                {
                    Vector3 vector = base.transform.TransformPoint(new Vector3((float)j * 0.1f, (float)i * 0.1f, 0f));
                    Vector3 forward = base.transform.forward;
                    if (Physics.Raycast(vector, forward, out var hitInfo))
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(vector, hitInfo.point);
                        Gizmos.DrawWireSphere(hitInfo.point, radius);
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawRay(vector, direction);
                    }
                }
            }
        }
    }
}
