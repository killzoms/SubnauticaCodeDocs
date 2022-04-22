using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace AssemblyCSharp
{
    public class SwimWaterSplash : MonoBehaviour
    {
        [AssertNotNull]
        public GameObject swimUnderWaterEffect;

        [AssertNotNull]
        public GameObject swimSurfaceEffect;

        [AssertNotNull]
        public Transform leftArmSplashTransform;

        [AssertNotNull]
        public Transform rightArmSplashTransform;

        [AssertNotNull]
        public FMOD_StudioEventEmitter splashUnderwaterSound;

        private int fmodIndexSpeed = -1;

        private int fmodIndexDepth = -1;

        private void SpawnEffect(Transform useTransform)
        {
            GameObject original = swimSurfaceEffect;
            FMOD_StudioEventEmitter fMOD_StudioEventEmitter = null;
            if (Player.main.IsUnderwater())
            {
                original = swimUnderWaterEffect;
                fMOD_StudioEventEmitter = splashUnderwaterSound;
            }
            if (Inventory.main.GetHeldTool() == null)
            {
                GameObject obj = Object.Instantiate(original);
                obj.transform.position = useTransform.position;
                obj.transform.parent = useTransform;
            }
            if (fMOD_StudioEventEmitter != null)
            {
                EventInstance @event = FMODUWE.GetEvent(fMOD_StudioEventEmitter.asset);
                if (fmodIndexSpeed < 0)
                {
                    fmodIndexSpeed = FMODUWE.GetEventInstanceParameterIndex(@event, "speed");
                }
                if (fmodIndexDepth < 0)
                {
                    fmodIndexDepth = FMODUWE.GetEventInstanceParameterIndex(@event, "depth");
                }
                ATTRIBUTES_3D attributes = useTransform.To3DAttributes();
                @event.set3DAttributes(attributes);
                @event.setVolume(1f);
                @event.setParameterValueByIndex(fmodIndexSpeed, Player.main.movementSpeed);
                @event.setParameterValueByIndex(fmodIndexDepth, Player.main.depthLevel);
                @event.start();
                @event.release();
            }
        }

        private void LeftArmSplash()
        {
            SpawnEffect(leftArmSplashTransform);
        }

        private void RightArmSplash()
        {
            SpawnEffect(rightArmSplashTransform);
        }
    }
}
