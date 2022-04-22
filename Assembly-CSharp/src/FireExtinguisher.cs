using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class FireExtinguisher : PlayerTool
    {
        public FMODASRPlayer useSound;

        public FMOD_CustomLoopingEmitter soundEmitter;

        public VFXController fxControl;

        [SerializeField]
        private float expendFuelPerSecond = 3.5f;

        [SerializeField]
        private float fireDousePerSecond = 20f;

        [ProtoMember(1)]
        public float fuel = 100f;

        public float maxFuel = 100f;

        public LayerMask impactLayerFX;

        private bool usedThisFrame;

        private Fire fireTarget;

        private bool fxIsPlaying;

        private bool impactFXisPlaying;

        private int fmodIndexInWater = -1;

        private int lastUnderwaterValue = -1;

        private int lastFuelStringValue = -1;

        private string cachedFuelString = "";

        private void UseExtinguisher(float douseAmount, float expendAmount)
        {
            if ((bool)fireTarget)
            {
                fireTarget.Douse(douseAmount);
            }
            if (fxControl != null && !fxIsPlaying)
            {
                fxControl.Play(0);
                fxIsPlaying = true;
            }
            if (!IntroVignette.isIntroActive)
            {
                fuel = Mathf.Max(fuel - expendAmount, 0f);
            }
        }

        private void UpdateTarget()
        {
            fireTarget = null;
            if (!(usingPlayer != null))
            {
                return;
            }
            Vector3 position = default(Vector3);
            GameObject closestObj = null;
            global::UWE.Utils.TraceFPSTargetPosition(Player.main.gameObject, 8f, ref closestObj, ref position);
            if ((bool)closestObj)
            {
                Fire componentInHierarchy = global::UWE.Utils.GetComponentInHierarchy<Fire>(closestObj);
                if ((bool)componentInHierarchy)
                {
                    fireTarget = componentInHierarchy;
                }
            }
        }

        private void UpdateImpactFX()
        {
            if (!(fxControl != null) || fxControl.emitters[1] == null)
            {
                return;
            }
            if (fxIsPlaying && fireTarget != null && (float)fireTarget.GetExtinguishPercent() > 0f)
            {
                GameObject instanceGO = fxControl.emitters[1].instanceGO;
                Transform transform = fxControl.gameObject.transform;
                bool flag = false;
                if (Physics.Raycast(transform.position, transform.forward, out var hitInfo, 3f, impactLayerFX, QueryTriggerInteraction.Ignore))
                {
                    instanceGO.transform.position = hitInfo.point;
                    instanceGO.transform.eulerAngles = hitInfo.normal * 360f;
                    flag = true;
                }
                if (flag && !impactFXisPlaying)
                {
                    instanceGO.transform.parent = null;
                    fxControl.Play(1);
                    impactFXisPlaying = true;
                }
                else if (!flag && impactFXisPlaying)
                {
                    fxControl.Stop(1);
                    impactFXisPlaying = false;
                }
            }
            else if (impactFXisPlaying)
            {
                fxControl.Stop(1);
                impactFXisPlaying = false;
            }
        }

        private void Update()
        {
            if (AvatarInputHandler.main.IsEnabled() && Player.main.GetRightHandHeld() && base.isDrawn)
            {
                usedThisFrame = true;
            }
            else
            {
                usedThisFrame = false;
            }
            int num = (Player.main.isUnderwater.value ? 1 : 0);
            if (num != lastUnderwaterValue)
            {
                lastUnderwaterValue = num;
                if (fmodIndexInWater < 0)
                {
                    fmodIndexInWater = soundEmitter.GetParameterIndex("in_water");
                }
                soundEmitter.SetParameterValue(fmodIndexInWater, num);
            }
            ProfilingUtils.BeginSample("FireExtinguisher.UpdateTarget");
            UpdateTarget();
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("FireExtinguisher.SoundUpdate");
            if (usedThisFrame && fuel > 0f)
            {
                if (Player.main.IsUnderwater())
                {
                    Player.main.GetComponent<UnderwaterMotor>().SetVelocity(-MainCamera.camera.transform.forward * 5f);
                }
                float douseAmount = fireDousePerSecond * Time.deltaTime;
                float expendAmount = expendFuelPerSecond * Time.deltaTime;
                UseExtinguisher(douseAmount, expendAmount);
                soundEmitter.Play();
            }
            else
            {
                soundEmitter.Stop();
                if (fxControl != null)
                {
                    fxControl.Stop(0);
                    fxIsPlaying = false;
                }
            }
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("FireExtinguisher.UpdateImpactFX");
            UpdateImpactFX();
            ProfilingUtils.EndSample();
        }

        private void StopExtinguisherFX()
        {
            fxControl.Stop(0);
        }

        private void FireExSpray()
        {
            if (fxControl != null)
            {
                fxControl.Play(0);
                Invoke("StopExtinguisherFX", 1.5f);
            }
        }

        public string GetFuelValueText()
        {
            int num = Mathf.FloorToInt(fuel);
            if (lastFuelStringValue != num)
            {
                float arg = fuel / maxFuel;
                cachedFuelString = Language.main.GetFormat("FuelPercent", arg);
                lastFuelStringValue = num;
            }
            return cachedFuelString;
        }

        public override string GetCustomUseText()
        {
            if (IntroVignette.isIntroActive)
            {
                return string.Empty;
            }
            return GetFuelValueText();
        }
    }
}
