using System;
using UnityEngine;

namespace AssemblyCSharp
{
    public class BaseFoundationPiece : MonoBehaviour
    {
        [Serializable]
        public struct Pillar
        {
            public GameObject root;

            public Transform adjustable;

            public Transform bottom;
        }

        public Pillar[] pillars;

        public float maxPillarHeight = 8f;

        public float extraHeight = 0.1f;

        public float minHeight;

        public void OnGenerate()
        {
            for (int i = 0; i < pillars.Length; i++)
            {
                Pillar pillar = pillars[i];
                pillar.root.SetActive(value: false);
                Transform adjustable = pillar.adjustable;
                if (!adjustable)
                {
                    continue;
                }
                Vector3 position = adjustable.position;
                Vector3 forward = adjustable.forward;
                int num = global::UWE.Utils.RaycastIntoSharedBuffer(position, forward, maxPillarHeight, -5, QueryTriggerInteraction.Ignore);
                float num2 = float.MaxValue;
                float num3 = float.MaxValue;
                for (int j = 0; j < num; j++)
                {
                    RaycastHit raycastHit = global::UWE.Utils.sharedHitBuffer[j];
                    if (!global::UWE.Utils.IsAncestorOf(base.gameObject, raycastHit.collider.gameObject))
                    {
                        Base componentInParent = raycastHit.collider.GetComponentInParent<Base>();
                        if ((bool)componentInParent && !componentInParent.isGhost)
                        {
                            num2 = Mathf.Min(num2, raycastHit.distance);
                        }
                        if (raycastHit.collider.gameObject.layer == 30)
                        {
                            num3 = Mathf.Min(num3, raycastHit.distance);
                        }
                    }
                }
                if (num3 < num2)
                {
                    Debug.DrawRay(position, forward * num3, Color.green, 10f, depthTest: true);
                    float num4 = num3 + 0.01f + extraHeight;
                    if (num4 >= minHeight)
                    {
                        adjustable.localScale = new Vector3(1f, 1f, num4);
                        if ((bool)pillar.bottom)
                        {
                            pillar.bottom.position = adjustable.position + forward * num4;
                        }
                        pillar.root.SetActive(value: true);
                    }
                }
                else if (num2 < num3)
                {
                    Debug.DrawRay(position, forward * num2, Color.red, 10f, depthTest: true);
                }
                else
                {
                    Debug.DrawRay(position, forward * maxPillarHeight, Color.black, 10f, depthTest: true);
                }
            }
        }
    }
}
