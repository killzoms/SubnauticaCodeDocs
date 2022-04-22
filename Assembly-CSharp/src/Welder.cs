using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(EnergyMixin))]
    public class Welder : PlayerTool
    {
        public FMODASRPlayer weldSound;

        public VFXController fxControl;

        public float weldEnergyCost = 1f;

        private float timeLastWelded;

        private float healthPerWeld = 10f;

        private bool usedThisFrame;

        private LiveMixin activeWeldTarget;

        private bool fxIsPlaying;

        private float timeTillLightbarUpdate;

        public override void OnToolUseAnim(GUIHand hand)
        {
            Weld();
        }

        private void OnDisable()
        {
            activeWeldTarget = null;
        }

        private void Weld()
        {
            if (!(activeWeldTarget != null))
            {
                return;
            }
            EnergyMixin component = base.gameObject.GetComponent<EnergyMixin>();
            if (!component.IsDepleted() && activeWeldTarget.AddHealth(healthPerWeld) > 0f)
            {
                if (fxControl != null && !fxIsPlaying)
                {
                    int i = (Player.main.IsUnderwater() ? 1 : 0);
                    fxControl.Play(i);
                    fxIsPlaying = true;
                }
                timeLastWelded = Time.time;
                component.ConsumeEnergy(weldEnergyCost);
            }
        }

        private void UpdateTarget()
        {
            ProfilingUtils.BeginSample("Welder.UpdateTarget()");
            activeWeldTarget = null;
            if (usingPlayer != null)
            {
                Vector3 position = default(Vector3);
                GameObject closestObj = null;
                global::UWE.Utils.TraceFPSTargetPosition(Player.main.gameObject, 2f, ref closestObj, ref position);
                if (closestObj == null)
                {
                    InteractionVolumeUser component = Player.main.gameObject.GetComponent<InteractionVolumeUser>();
                    if (component != null && component.GetMostRecent() != null)
                    {
                        closestObj = component.GetMostRecent().gameObject;
                    }
                }
                if ((bool)closestObj)
                {
                    LiveMixin liveMixin = closestObj.FindAncestor<LiveMixin>();
                    if ((bool)liveMixin)
                    {
                        if (liveMixin.IsWeldable())
                        {
                            activeWeldTarget = liveMixin;
                        }
                        else
                        {
                            WeldablePoint weldablePoint = closestObj.FindAncestor<WeldablePoint>();
                            if (weldablePoint != null && weldablePoint.transform.IsChildOf(liveMixin.transform))
                            {
                                activeWeldTarget = liveMixin;
                            }
                        }
                    }
                }
            }
            ProfilingUtils.EndSample();
        }

        private void UpdateUI()
        {
            if (activeWeldTarget != null)
            {
                float healthFraction = activeWeldTarget.GetHealthFraction();
                if (healthFraction < 1f)
                {
                    HandReticle main = HandReticle.main;
                    main.SetProgress(healthFraction);
                    main.SetIcon(HandReticle.IconType.Progress, 1.5f);
                    main.SetInteractInfo("Weld");
                }
            }
        }

        public override void OnHolster()
        {
            base.OnHolster();
            StopWeldingFX();
        }

        private void StopWeldingFX()
        {
            weldSound.Stop();
            if (fxControl != null)
            {
                fxControl.StopAndDestroy(0f);
                fxIsPlaying = false;
            }
            if (PlatformUtils.isPS4Platform)
            {
                PlatformUtils.ResetLightbarColor();
            }
        }

        private void Update()
        {
            usedThisFrame = false;
            if (base.isDrawn)
            {
                if (AvatarInputHandler.main.IsEnabled() && Player.main.GetRightHandHeld() && !Player.main.IsBleederAttached())
                {
                    usedThisFrame = true;
                }
                if (usedThisFrame)
                {
                    weldSound.Play();
                }
                else
                {
                    StopWeldingFX();
                }
                UpdateTarget();
                UpdateUI();
                if (PlatformUtils.isPS4Platform)
                {
                    UpdateLightbar();
                }
            }
        }

        public override bool GetUsedToolThisFrame()
        {
            return usedThisFrame;
        }

        private void UpdateLightbar()
        {
            if (fxIsPlaying)
            {
                timeTillLightbarUpdate -= Time.deltaTime;
                if (timeTillLightbarUpdate < 0f)
                {
                    PlatformUtils.SetLightbarColor(global::UnityEngine.Random.ColorHSV(0.05f, 0.175f, 0.95f, 1f, 0.35f, 0.7f));
                    timeTillLightbarUpdate = 71f / (339f * (float)System.Math.PI) + global::UnityEngine.Random.Range(-0.0133333346f, 0.0133333346f);
                }
            }
        }
    }
}
