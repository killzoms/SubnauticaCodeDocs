using UnityEngine;

namespace AssemblyCSharp
{
    public class VolumeCullManager : MonoBehaviour
    {
        public bool cullOutsideVolume;

        public int checkEveryXFrame = 15;

        [AssertNotNull]
        public Collider[] colliders;

        [AssertNotNull]
        public GameObject[] gameObjectsToCull;

        public bool cullSun;

        private int currentFrameIndex;

        private bool isInVolume = true;

        private bool wasInVolume;

        private Transform camTransform;

        private Vector3 prevCamPos = Vector3.zero;

        private bool disabledLight;

        private bool GetCameraHasMoved()
        {
            return (prevCamPos - camTransform.position).sqrMagnitude >= 1f;
        }

        private void Start()
        {
            camTransform = MainCamera.camera.transform;
            isInVolume = IsInVolume();
            ToggleGameObjects(!isInVolume);
            wasInVolume = isInVolume;
        }

        private void OnDestroy()
        {
            if (disabledLight && (bool)DayNightCycle.main)
            {
                DayNightCycle.main.SetLightEnabled(lightEnabled: true);
            }
        }

        private void ToggleGameObjects(bool toEnable)
        {
            GameObject[] array = gameObjectsToCull;
            for (int i = 0; i < array.Length; i++)
            {
                array[i].SetActive(toEnable);
            }
            if (cullSun && DayNightCycle.main != null)
            {
                disabledLight = !toEnable;
                DayNightCycle.main.SetLightEnabled(toEnable);
            }
        }

        private bool IsInVolume()
        {
            bool flag = false;
            Collider[] array = colliders;
            for (int i = 0; i < array.Length; i++)
            {
                flag = array[i].bounds.Contains(camTransform.position);
                if (flag)
                {
                    break;
                }
            }
            if (cullOutsideVolume)
            {
                flag = !flag;
            }
            return flag;
        }

        private void Update()
        {
            currentFrameIndex++;
            if (camTransform == null)
            {
                camTransform = MainCamera.camera.transform;
            }
            else if (checkEveryXFrame <= currentFrameIndex && GetCameraHasMoved())
            {
                prevCamPos = camTransform.position;
                currentFrameIndex = 0;
                isInVolume = IsInVolume();
                if (isInVolume != wasInVolume)
                {
                    ToggleGameObjects(!isInVolume);
                    wasInVolume = isInVolume;
                }
            }
        }
    }
}
