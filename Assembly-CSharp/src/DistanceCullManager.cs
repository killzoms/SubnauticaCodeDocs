using UnityEngine;

namespace AssemblyCSharp
{
    public class DistanceCullManager : MonoBehaviour
    {
        private DistanceCull[] distCull;

        public int checksPerFrame = 15;

        private int currentIndex;

        private MainCameraControl mainCameraControl;

        private Vector3 cameraPos = Vector3.zero;

        private void Update()
        {
            if (distCull == null || mainCameraControl == null)
            {
                distCull = base.gameObject.GetComponentsInChildren<DistanceCull>(includeInactive: true);
                mainCameraControl = MainCameraControl.main;
                return;
            }
            cameraPos = mainCameraControl.transform.position;
            int num = currentIndex + checksPerFrame;
            if (num > distCull.Length)
            {
                num = distCull.Length;
            }
            for (int i = currentIndex; i < num; i++)
            {
                bool flag = (cameraPos - distCull[i].transform.position).sqrMagnitude > distCull[i].distanceSqr;
                if (distCull[i].isEnabled && flag)
                {
                    distCull[i].DisableObject();
                }
                else if (!flag)
                {
                    if (distCull[i].isEnabled)
                    {
                        distCull[i].EnableObject();
                    }
                    else
                    {
                        distCull[i].gameObject.SetActive(value: true);
                    }
                }
            }
            currentIndex += checksPerFrame;
            if (currentIndex >= distCull.Length - 1)
            {
                currentIndex = 0;
            }
        }
    }
}
