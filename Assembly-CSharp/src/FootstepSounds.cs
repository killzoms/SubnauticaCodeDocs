using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace AssemblyCSharp
{
    public class FootstepSounds : MonoBehaviour
    {
        [AssertNotNull]
        public Transform leftFootXform;

        [AssertNotNull]
        public Transform rightFootXform;

        [AssertNotNull]
        public FMODAsset metalSound;

        [AssertNotNull]
        public FMODAsset landSound;

        [AssertNotNull]
        public FMODAsset precursorInteriorSound;

        public bool scriptTriggersEvent;

        public float footStepFrequencyMod = 1f;

        public bool debug;

        public float clampedSpeed = -1f;

        private static string crashedShip = "crashedShip";

        private static string precursorGun = "PrecursorGun";

        private IGroundMoveable groundMoveable;

        private float timeLastFootStep;

        private bool triggeredLeft;

        private int fmodIndexSpeed = -1;

        private void Start()
        {
            groundMoveable = GetComponentInChildren<IGroundMoveable>();
            if (groundMoveable == null)
            {
                groundMoveable = GetComponentInParent<IGroundMoveable>();
            }
            if (scriptTriggersEvent)
            {
                InvokeRepeating("TriggerSounds", 0f, 0.05f);
            }
        }

        private void OnDestroy()
        {
            CancelInvoke("TriggerSounds");
        }

        private void TriggerSounds()
        {
            if (!ShouldPlayStepSounds())
            {
                return;
            }
            Vector3 velocity = groundMoveable.GetVelocity();
            float num = Time.time - timeLastFootStep;
            float num2 = velocity.magnitude;
            if (clampedSpeed > 0f)
            {
                num2 = Mathf.Min(clampedSpeed, num2);
            }
            float num3 = 2.5f * footStepFrequencyMod / num2;
            if (num >= num3)
            {
                Transform xform = (triggeredLeft ? rightFootXform : leftFootXform);
                if (debug)
                {
                    global::UnityEngine.Debug.Log(Time.time + "trigger foot step sound, left: " + (!triggeredLeft).ToString() + ", speed: " + velocity.magnitude);
                }
                OnStep(xform);
                timeLastFootStep = Time.time;
                triggeredLeft = !triggeredLeft;
            }
        }

        private bool ShouldPlayStepSounds()
        {
            if (groundMoveable.IsOnGround() && groundMoveable.IsActive())
            {
                return groundMoveable.GetVelocity().magnitude > 0.2f;
            }
            return false;
        }

        private void OnStep(Transform xform)
        {
            if (!ShouldPlayStepSounds())
            {
                return;
            }
            FMODAsset asset = (Player.main.precursorOutOfWater ? precursorInteriorSound : (((groundMoveable.GetGroundSurfaceType() != VFXSurfaceTypes.metal && !Player.main.IsInside() && !(Player.main.GetBiomeString() == crashedShip)) || !(Player.main.currentWaterPark == null)) ? landSound : metalSound));
            EventInstance @event = FMODUWE.GetEvent(asset);
            if (@event.isValid())
            {
                if (fmodIndexSpeed < 0)
                {
                    fmodIndexSpeed = FMODUWE.GetEventInstanceParameterIndex(@event, "speed");
                }
                ATTRIBUTES_3D attributes = xform.To3DAttributes();
                @event.set3DAttributes(attributes);
                @event.setParameterValueByIndex(fmodIndexSpeed, groundMoveable.GetVelocity().magnitude);
                @event.setVolume(1f);
                @event.start();
                @event.release();
            }
        }

        private void footfall_left()
        {
            if (!scriptTriggersEvent)
            {
                OnStep(leftFootXform);
            }
        }

        private void footfall_right()
        {
            if (!scriptTriggersEvent)
            {
                OnStep(rightFootXform);
            }
        }
    }
}
