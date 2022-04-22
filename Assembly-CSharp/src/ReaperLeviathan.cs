using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class ReaperLeviathan : Creature
    {
        private enum VehicleType
        {
            None,
            Seamoth,
            Exosuit
        }

        private Vehicle holdingVehicle;

        private VehicleType holdingVehicleType;

        private float timeVehicleGrabbed;

        private float timeVehicleReleased;

        private Quaternion vehicleInitialRotation;

        private Vector3 vehicleInitialPosition;

        public Transform seamothAttachPoint;

        public float seamothDamagePerSecond = 15f;

        public FMOD_CustomLoopingEmitter seamothGrabSound;

        public bool IsHoldingVehicle()
        {
            return holdingVehicle != null;
        }

        public bool IsHoldingSeamoth()
        {
            if (holdingVehicleType == VehicleType.Seamoth)
            {
                return holdingVehicle != null;
            }
            return false;
        }

        public bool IsHoldingExosuit()
        {
            if (holdingVehicleType == VehicleType.Exosuit)
            {
                return holdingVehicle != null;
            }
            return false;
        }

        public void GrabSeamoth(SeaMoth seamoth)
        {
            GrabVehicle(seamoth, VehicleType.Seamoth);
        }

        public void GrabExosuit(Exosuit exosuit)
        {
            GrabVehicle(exosuit, VehicleType.Exosuit);
        }

        private void GrabVehicle(Vehicle vehicle, VehicleType type)
        {
            vehicle.GetComponent<Rigidbody>().isKinematic = true;
            vehicle.collisionModel.SetActive(value: false);
            holdingVehicle = vehicle;
            holdingVehicleType = type;
            if (holdingVehicleType == VehicleType.Exosuit)
            {
                SafeAnimator.SetBool(holdingVehicle.mainAnimator, "reaper_attack", value: true);
                Exosuit component = holdingVehicle.GetComponent<Exosuit>();
                if (component != null)
                {
                    component.cinematicMode = true;
                }
            }
            Aggression.Value = 0f;
            timeVehicleGrabbed = Time.time;
            vehicleInitialRotation = vehicle.transform.rotation;
            vehicleInitialPosition = vehicle.transform.position;
            seamothGrabSound.Play();
            InvokeRepeating("DamageVehicle", 1f, 1f);
            Invoke("ReleaseVehicle", 8f + Random.value * 5f);
        }

        public bool GetCanGrabVehicle()
        {
            if (timeVehicleReleased + 10f < Time.time)
            {
                return !IsHoldingVehicle();
            }
            return false;
        }

        public void ReleaseVehicle()
        {
            if (holdingVehicle != null)
            {
                if (holdingVehicleType == VehicleType.Exosuit)
                {
                    SafeAnimator.SetBool(holdingVehicle.mainAnimator, "reaper_attack", value: false);
                    Exosuit component = holdingVehicle.GetComponent<Exosuit>();
                    if (component != null)
                    {
                        component.cinematicMode = false;
                    }
                }
                holdingVehicle.GetComponent<Rigidbody>().isKinematic = false;
                holdingVehicle.collisionModel.SetActive(value: true);
                holdingVehicle = null;
                timeVehicleReleased = Time.time;
            }
            holdingVehicleType = VehicleType.None;
            CancelInvoke("DamageVehicle");
            seamothGrabSound.Stop();
        }

        private void DamageVehicle()
        {
            if (holdingVehicle != null)
            {
                holdingVehicle.liveMixin.TakeDamage(seamothDamagePerSecond);
            }
        }

        public override void OnTakeDamage(DamageInfo damageInfo)
        {
            base.OnTakeDamage(damageInfo);
            if ((damageInfo.type == DamageType.Electrical || damageInfo.type == DamageType.Poison) && holdingVehicle != null)
            {
                ReleaseVehicle();
            }
        }

        public void Update()
        {
            if (holdingVehicleType != 0 && holdingVehicle == null)
            {
                ReleaseVehicle();
            }
            SafeAnimator.SetBool(GetAnimator(), "seamoth_attack", IsHoldingSeamoth());
            SafeAnimator.SetBool(GetAnimator(), "exo_attack", IsHoldingExosuit());
            if (holdingVehicle != null)
            {
                float num = Mathf.Clamp01(Time.time - timeVehicleGrabbed);
                if (num >= 1f)
                {
                    holdingVehicle.transform.position = seamothAttachPoint.position;
                    holdingVehicle.transform.rotation = seamothAttachPoint.transform.rotation;
                }
                else
                {
                    holdingVehicle.transform.position = (seamothAttachPoint.position - vehicleInitialPosition) * num + vehicleInitialPosition;
                    holdingVehicle.transform.rotation = Quaternion.Lerp(vehicleInitialRotation, seamothAttachPoint.transform.rotation, num);
                }
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();
            if (holdingVehicle != null)
            {
                ReleaseVehicle();
            }
        }
    }
}
