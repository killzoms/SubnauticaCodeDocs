using System;
using Gendarme;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class GUIHand : MonoBehaviour
    {
        public enum Mode
        {
            Free,
            Tool,
            Placing
        }

        public enum GrabMode
        {
            None,
            World,
            Screen
        }

        public const float kUseDistance = 2f;

        [AssertNotNull]
        public Player player;

        private GameObject activeTarget;

        private float activeHitDistance;

        private bool suppressTooltip;

        private bool usedToolThisFrame;

        private bool usedAltAttackThisFrame;

        private GrabMode grabMode;

        private bool targetDebug;

        private const ManagedUpdate.Queue updateQueue = ManagedUpdate.Queue.UpdateInput;

        private int cachedTextEnergyScalar = -1;

        private string cachedEnergyHudText = "";

        private float timeOfLastToolUseAnim = -1f;

        private void Start()
        {
            DevConsole.RegisterConsoleCommand(this, "target");
        }

        private void OnEnable()
        {
            ManagedUpdate.Subscribe(ManagedUpdate.Queue.UpdateInput, OnUpdate);
        }

        private void OnDisable()
        {
            ManagedUpdate.Unsubscribe(ManagedUpdate.Queue.UpdateInput, OnUpdate);
        }

        private void UpdateActiveTarget()
        {
            PlayerTool tool = GetTool();
            if (tool != null && tool.GetComponent<PropulsionCannon>() != null && tool.GetComponent<PropulsionCannon>().IsGrabbingObject())
            {
                activeTarget = tool.GetComponent<PropulsionCannon>().GetNearbyGrabbedObject();
                suppressTooltip = true;
            }
            else if ((tool != null && tool.DoesOverrideHand()) || !Targeting.GetTarget(Player.main.gameObject, 2f, out activeTarget, out activeHitDistance))
            {
                activeTarget = null;
                activeHitDistance = 0f;
            }
            else if (activeTarget.layer == LayerID.NotUseable)
            {
                activeTarget = null;
            }
            else
            {
                IHandTarget handTarget = null;
                Transform parent = activeTarget.transform;
                while (parent != null)
                {
                    handTarget = parent.GetComponent<IHandTarget>();
                    if (handTarget == null)
                    {
                        parent = parent.parent;
                        continue;
                    }
                    activeTarget = parent.gameObject;
                    break;
                }
                if (handTarget == null)
                {
                    switch (CraftData.GetHarvestTypeFromTech(CraftData.GetTechType(activeTarget)))
                    {
                        case HarvestType.Pick:
                            if (Utils.FindAncestorWithComponent<Pickupable>(activeTarget) == null)
                            {
                                LargeWorldEntity largeWorldEntity = Utils.FindAncestorWithComponent<LargeWorldEntity>(activeTarget);
                                largeWorldEntity.gameObject.AddComponent<Pickupable>();
                                largeWorldEntity.gameObject.AddComponent<WorldForces>().useRigidbody = largeWorldEntity.GetComponent<Rigidbody>();
                            }
                            break;
                        case HarvestType.None:
                            activeTarget = null;
                            break;
                    }
                }
            }
            if (IntroVignette.isIntroActive)
            {
                activeTarget = FilterIntroTarget(activeTarget);
            }
        }

        public void DestroyActiveTarget()
        {
            if (activeTarget != null)
            {
                global::UnityEngine.Object.Destroy(activeTarget);
            }
        }

        private void OnConsoleCommand_target(NotificationCenter.Notification n)
        {
            targetDebug = !targetDebug;
            ErrorMessage.AddDebug("target debug now " + targetDebug);
        }

        [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidComplexMethodsRule")]
        private void OnUpdate()
        {
            usedToolThisFrame = false;
            usedAltAttackThisFrame = false;
            suppressTooltip = false;
            if (player.IsFreeToInteract() && AvatarInputHandler.main.IsEnabled())
            {
                string text = string.Empty;
                PlayerTool tool = GetTool();
                EnergyMixin energyMixin = null;
                if (tool != null)
                {
                    ProfilingUtils.BeginSample("GUIHandUpdate-GetCustomUseText");
                    text = tool.GetCustomUseText();
                    ProfilingUtils.EndSample();
                    energyMixin = tool.GetComponent<EnergyMixin>();
                }
                if (energyMixin != null && energyMixin.allowBatteryReplacement)
                {
                    ProfilingUtils.BeginSample("GUIHandUpdate-EnergyMixinAllowBattery");
                    int num = Mathf.FloorToInt(energyMixin.GetEnergyScalar() * 100f);
                    if (cachedTextEnergyScalar != num)
                    {
                        if (num <= 0)
                        {
                            cachedEnergyHudText = LanguageCache.GetButtonFormat("ExchangePowerSource", GameInput.Button.Reload);
                        }
                        else
                        {
                            cachedEnergyHudText = Language.main.GetFormat("PowerPercent", energyMixin.GetEnergyScalar());
                        }
                        cachedTextEnergyScalar = num;
                    }
                    HandReticle.main.SetUseTextRaw(text, cachedEnergyHudText);
                    ProfilingUtils.EndSample();
                }
                else if (!string.IsNullOrEmpty(text))
                {
                    HandReticle.main.SetUseTextRaw(text, string.Empty);
                }
                ProfilingUtils.BeginSample("GUIHandUpdate-UpdateActiveTarget");
                if (grabMode == GrabMode.None)
                {
                    UpdateActiveTarget();
                }
                ProfilingUtils.EndSample();
                HandReticle.main.SetTargetDistance(activeHitDistance);
                ProfilingUtils.BeginSample("GUIHandUpdate-TargetToolTips");
                if (activeTarget != null && !suppressTooltip)
                {
                    TechType techType = CraftData.GetTechType(activeTarget);
                    if (techType != 0)
                    {
                        HandReticle.main.SetInteractInfo(techType.AsString());
                    }
                    ProfilingUtils.BeginSample("GUIHandUpdate-SendToActiveTarget");
                    Send(activeTarget, HandTargetEventType.Hover, this);
                    ProfilingUtils.EndSample();
                }
                ProfilingUtils.EndSample();
                bool flag = GameInput.GetButtonDown(GameInput.Button.LeftHand);
                bool buttonHeld = GameInput.GetButtonHeld(GameInput.Button.LeftHand);
                bool buttonUp = GameInput.GetButtonUp(GameInput.Button.LeftHand);
                bool flag2 = GameInput.GetButtonDown(GameInput.Button.RightHand);
                bool buttonHeld2 = GameInput.GetButtonHeld(GameInput.Button.RightHand);
                bool buttonUp2 = GameInput.GetButtonUp(GameInput.Button.RightHand);
                bool buttonDown = GameInput.GetButtonDown(GameInput.Button.Reload);
                bool buttonDown2 = GameInput.GetButtonDown(GameInput.Button.Exit);
                bool buttonDown3 = GameInput.GetButtonDown(GameInput.Button.AltTool);
                bool buttonHeld3 = GameInput.GetButtonHeld(GameInput.Button.AltTool);
                bool buttonUp3 = GameInput.GetButtonUp(GameInput.Button.AltTool);
                ProfilingUtils.BeginSample("GUIHandUpdate-PDAScannerGetTarget");
                PDAScanner.UpdateTarget(8f, buttonDown3 || buttonHeld3);
                ProfilingUtils.EndSample();
                ProfilingUtils.BeginSample("GUIHandUpdate-PDAScanner");
                PDAScanner.ScanTarget scanTarget = PDAScanner.scanTarget;
                if (scanTarget.isValid && Inventory.main.container.Contains(TechType.Scanner) && PDAScanner.CanScan() == PDAScanner.Result.Scan && !PDAScanner.scanTarget.isPlayer)
                {
                    uGUI_ScannerIcon.main.Show();
                }
                ProfilingUtils.EndSample();
                if (tool != null)
                {
                    if (flag2)
                    {
                        if (tool.OnRightHandDown())
                        {
                            usedToolThisFrame = true;
                            tool.OnToolActionStart();
                            flag2 = false;
                            buttonHeld2 = false;
                            buttonUp2 = false;
                        }
                    }
                    else if (buttonHeld2)
                    {
                        if (tool.OnRightHandHeld())
                        {
                            flag2 = false;
                            buttonHeld2 = false;
                        }
                    }
                    else if (buttonUp2 && tool.OnRightHandUp())
                    {
                        buttonUp2 = false;
                    }
                    if (flag)
                    {
                        if (tool.OnLeftHandDown())
                        {
                            tool.OnToolActionStart();
                            flag = false;
                            buttonHeld = false;
                            buttonUp = false;
                        }
                    }
                    else if (buttonHeld)
                    {
                        if (tool.OnLeftHandHeld())
                        {
                            flag = false;
                            buttonHeld = false;
                        }
                    }
                    else if (buttonUp && tool.OnLeftHandUp())
                    {
                        buttonUp = false;
                    }
                    if (buttonDown3)
                    {
                        if (tool.OnAltDown())
                        {
                            usedAltAttackThisFrame = true;
                            tool.OnToolActionStart();
                            buttonDown3 = false;
                            buttonHeld3 = false;
                            buttonUp3 = false;
                        }
                    }
                    else if (buttonHeld3)
                    {
                        if (tool.OnAltHeld())
                        {
                            buttonDown3 = false;
                            buttonHeld3 = false;
                        }
                    }
                    else if (buttonUp3 && tool.OnAltUp())
                    {
                        buttonUp3 = false;
                    }
                    if (buttonDown && tool.OnReloadDown())
                    {
                        buttonDown = false;
                    }
                    if (buttonDown2 && tool.OnExitDown())
                    {
                        buttonDown2 = false;
                    }
                }
                if (tool == null && flag2)
                {
                    Inventory.main.DropHeldItem(applyForce: true);
                }
                if (activeTarget != null && flag)
                {
                    Send(activeTarget, HandTargetEventType.Click, this);
                }
            }
            ProfilingUtils.BeginSample("GUIHandUpdate-OpenPDA");
            if (AvatarInputHandler.main.IsEnabled() && GameInput.GetButtonDown(GameInput.Button.PDA) && !IntroVignette.isIntroActive)
            {
                player.GetPDA().Open();
            }
            ProfilingUtils.EndSample();
            if (targetDebug && (bool)activeTarget)
            {
                HandReticle.main.SetInteractTextRaw($"activeTarget: {activeTarget.name}", string.Empty);
            }
        }

        public static void Send(GameObject target, HandTargetEventType e, GUIHand hand)
        {
            if (target == null || !target.activeInHierarchy || e == HandTargetEventType.None)
            {
                return;
            }
            IHandTarget component = target.GetComponent<IHandTarget>();
            if (component == null)
            {
                return;
            }
            try
            {
                switch (e)
                {
                    case HandTargetEventType.Hover:
                        ProfilingUtils.BeginSample("OnHandHover");
                        component.OnHandHover(hand);
                        ProfilingUtils.EndSample();
                        break;
                    case HandTargetEventType.Click:
                        ProfilingUtils.BeginSample("OnHandClick");
                        component.OnHandClick(hand);
                        ProfilingUtils.EndSample();
                        break;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private GameObject FilterIntroTarget(GameObject target)
        {
            if (target == null)
            {
                return null;
            }
            if (target.GetComponent<UnusableInLifepodIntro>() != null)
            {
                return null;
            }
            if (target.GetComponent<Fabricator>() != null)
            {
                return null;
            }
            if (target.GetComponent<Radio>() != null)
            {
                return null;
            }
            if (target.GetComponent<MedicalCabinet>() != null)
            {
                return null;
            }
            return target;
        }

        public PlayerTool GetTool()
        {
            return Inventory.main.GetHeldTool();
        }

        public T GetToolOfType<T>() where T : Behaviour
        {
            T result = null;
            PlayerTool heldTool = Inventory.main.GetHeldTool();
            if (heldTool != null)
            {
                return heldTool.gameObject.GetComponent<T>();
            }
            return result;
        }

        public bool GetUsingTool()
        {
            if (AvatarInputHandler.main.IsEnabled() && GetTool() != null)
            {
                if (!usedToolThisFrame)
                {
                    return GetTool().GetUsedToolThisFrame();
                }
                return true;
            }
            return false;
        }

        public bool GetAltAttacking()
        {
            if (AvatarInputHandler.main.IsEnabled() && GetTool() != null)
            {
                return GetTool().GetAltUsedToolThisFrame();
            }
            return false;
        }

        public void OnToolBleederHitAnim()
        {
            if (GetTool() != null)
            {
                GetTool().OnToolBleederHitAnim(this);
            }
        }

        public void OnToolUseAnim()
        {
            if (timeOfLastToolUseAnim == -1f || Time.time > timeOfLastToolUseAnim + 0.5f)
            {
                if (GetTool() != null)
                {
                    GetTool().OnToolUseAnim(this);
                }
                timeOfLastToolUseAnim = Time.time;
            }
        }

        public void BashHit()
        {
            if (GetTool() != null)
            {
                GetTool().SendMessage("BashHit", this, SendMessageOptions.DontRequireReceiver);
            }
            if (activeTarget != null)
            {
                activeTarget.SendMessage("BashHit", this, SendMessageOptions.DontRequireReceiver);
            }
        }

        public void FireExSpray()
        {
            if (GetTool() != null)
            {
                GetTool().SendMessage("FireExSpray", this, SendMessageOptions.DontRequireReceiver);
            }
        }

        public Mode GetMode()
        {
            if (GetTool() != null)
            {
                return Mode.Tool;
            }
            return Mode.Free;
        }

        public bool IsTool()
        {
            return GetMode() == Mode.Tool;
        }

        public bool IsFreeToInteract()
        {
            if (GetMode() != 0)
            {
                return GetMode() == Mode.Tool;
            }
            return true;
        }

        public void SetGrabMode(GrabMode val)
        {
            this.grabMode = val;
            GrabMode grabMode = this.grabMode;
            if ((uint)grabMode > 1u && grabMode == GrabMode.Screen)
            {
                GetComponent<MouseLook>().SetEnabled(val: false);
            }
            else
            {
                GetComponent<MouseLook>().SetEnabled(val: true);
            }
        }

        public Camera GetPlayerCamera()
        {
            return MainCamera.camera;
        }

        public Vector3 GetGrabbingHandPosition()
        {
            Camera playerCamera = GetPlayerCamera();
            return playerCamera.transform.position + playerCamera.transform.forward * activeHitDistance;
        }

        public Vector3 GetActiveHitPosition()
        {
            return GetGrabbingHandPosition();
        }

        public Facing GetFacingInSub()
        {
            Vector3 vector = player.GetCurrentSub().transform.InverseTransformDirection(player.transform.forward);
            float x = vector.x;
            float z = vector.z;
            if (Mathf.Abs(x) > Mathf.Abs(z))
            {
                if (!(x > 0f))
                {
                    return Facing.West;
                }
                return Facing.East;
            }
            if (!(z > 0f))
            {
                return Facing.South;
            }
            return Facing.North;
        }

        public Vector3 GetPlayerEyePos()
        {
            return GetPlayerCamera().transform.position;
        }

        public GameObject GetActiveTarget()
        {
            return activeTarget;
        }
    }
}
