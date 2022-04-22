using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AssemblyCSharp
{
    public class BuilderTool : PlayerTool
    {
        private const float hitRange = 30f;

        public float powerConsumptionConstruct = 0.5f;

        public float powerConsumptionDeconstruct = 0.5f;

        public Renderer bar;

        public int barMaterialID = 1;

        public Transform nozzleLeft;

        public Transform nozzleRight;

        public Transform beamLeft;

        public Transform beamRight;

        public float nozzleRotationSpeed = 10f;

        [Range(0.01f, 5f)]
        public float pointSwitchTimeMin = 0.1f;

        [Range(0.01f, 5f)]
        public float pointSwitchTimeMax = 1f;

        public Animator animator;

        public FMOD_CustomLoopingEmitter buildSound;

        public FMODAsset completeSound;

        private bool isConstructing;

        private Constructable constructable;

        private int handleInputFrame = -1;

        private Vector3 leftPoint = Vector3.zero;

        private Vector3 rightPoint = Vector3.zero;

        private float leftConstructionTime;

        private float rightConstructionTime;

        private float leftConstructionInterval;

        private float rightConstructionInterval;

        private Vector3 leftConstructionPoint;

        private Vector3 rightConstructionPoint;

        private string deconstructText;

        private string constructText;

        private string noPowerText;

        private Material barMaterial;

        private void Start()
        {
            if (barMaterial == null)
            {
                barMaterial = bar.materials[barMaterialID];
            }
            SetBeamActive(state: false);
            UpdateText();
        }

        private void OnDisable()
        {
            buildSound.Stop();
        }

        private void Update()
        {
            HandleInput();
        }

        private void LateUpdate()
        {
            Quaternion b = Quaternion.identity;
            Quaternion b2 = Quaternion.identity;
            bool flag = constructable != null;
            if (isConstructing != flag)
            {
                isConstructing = flag;
                if (isConstructing)
                {
                    leftConstructionInterval = Random.Range(pointSwitchTimeMin, pointSwitchTimeMax);
                    rightConstructionInterval = Random.Range(pointSwitchTimeMin, pointSwitchTimeMax);
                    leftConstructionPoint = constructable.GetRandomConstructionPoint();
                    rightConstructionPoint = constructable.GetRandomConstructionPoint();
                }
                else
                {
                    leftConstructionTime = 0f;
                    rightConstructionTime = 0f;
                }
            }
            else if (isConstructing)
            {
                leftConstructionTime += Time.deltaTime;
                rightConstructionTime += Time.deltaTime;
                if (leftConstructionTime >= leftConstructionInterval)
                {
                    leftConstructionTime %= leftConstructionInterval;
                    leftConstructionInterval = Random.Range(pointSwitchTimeMin, pointSwitchTimeMax);
                    leftConstructionPoint = constructable.GetRandomConstructionPoint();
                }
                if (rightConstructionTime >= rightConstructionInterval)
                {
                    rightConstructionTime %= rightConstructionInterval;
                    rightConstructionInterval = Random.Range(pointSwitchTimeMin, pointSwitchTimeMax);
                    rightConstructionPoint = constructable.GetRandomConstructionPoint();
                }
                leftPoint = nozzleLeft.parent.InverseTransformPoint(leftConstructionPoint);
                rightPoint = nozzleRight.parent.InverseTransformPoint(rightConstructionPoint);
                Debug.DrawLine(nozzleLeft.position, leftConstructionPoint, Color.white);
                Debug.DrawLine(nozzleRight.position, rightConstructionPoint, Color.white);
            }
            if (isConstructing)
            {
                b = Quaternion.LookRotation(leftPoint, Vector3.up);
                b2 = Quaternion.LookRotation(rightPoint, Vector3.up);
                Vector3 localScale = beamLeft.localScale;
                localScale.z = leftPoint.magnitude;
                beamLeft.localScale = localScale;
                localScale = beamRight.localScale;
                localScale.z = rightPoint.magnitude;
                beamRight.localScale = localScale;
                Debug.DrawLine(nozzleLeft.position, leftConstructionPoint, Color.white);
                Debug.DrawLine(nozzleRight.position, rightConstructionPoint, Color.white);
            }
            float t = nozzleRotationSpeed * Time.deltaTime;
            nozzleLeft.localRotation = Quaternion.Slerp(nozzleLeft.localRotation, b, t);
            nozzleRight.localRotation = Quaternion.Slerp(nozzleRight.localRotation, b2, t);
            SetBeamActive(isConstructing);
            SetUsingAnimation(isConstructing);
            if (isConstructing)
            {
                buildSound.Play();
            }
            else
            {
                buildSound.Stop();
            }
            UpdateBar();
            constructable = null;
        }

        private void HandleInput()
        {
            if (handleInputFrame == Time.frameCount)
            {
                return;
            }
            handleInputFrame = Time.frameCount;
            if (!base.isDrawn || Builder.isPlacing || !AvatarInputHandler.main.IsEnabled() || TryDisplayNoPowerTooltip())
            {
                return;
            }
            Targeting.AddToIgnoreList(Player.main.gameObject);
            Targeting.GetTarget(30f, out var result, out var distance);
            if (result == null)
            {
                return;
            }
            bool buttonHeld = GameInput.GetButtonHeld(GameInput.Button.LeftHand);
            bool buttonDown = GameInput.GetButtonDown(GameInput.Button.Deconstruct);
            bool buttonHeld2 = GameInput.GetButtonHeld(GameInput.Button.Deconstruct);
            Constructable constructable = result.GetComponentInParent<Constructable>();
            if (constructable != null && distance > constructable.placeMaxDistance)
            {
                constructable = null;
            }
            string reason;
            if (constructable != null)
            {
                OnHover(constructable);
                if (buttonHeld)
                {
                    Construct(constructable, state: true);
                }
                else if (constructable.DeconstructionAllowed(out reason))
                {
                    if (buttonHeld2)
                    {
                        if (constructable.constructed)
                        {
                            constructable.SetState(value: false, setAmount: false);
                        }
                        else
                        {
                            Construct(constructable, state: false);
                        }
                    }
                }
                else if (buttonDown && !string.IsNullOrEmpty(reason))
                {
                    ErrorMessage.AddMessage(reason);
                }
                return;
            }
            BaseDeconstructable baseDeconstructable = result.GetComponentInParent<BaseDeconstructable>();
            if (baseDeconstructable == null)
            {
                BaseExplicitFace componentInParent = result.GetComponentInParent<BaseExplicitFace>();
                if (componentInParent != null)
                {
                    baseDeconstructable = componentInParent.parent;
                }
            }
            if (!(baseDeconstructable != null))
            {
                return;
            }
            if (baseDeconstructable.DeconstructionAllowed(out reason))
            {
                OnHover(baseDeconstructable);
                if (buttonDown)
                {
                    baseDeconstructable.Deconstruct();
                }
            }
            else if (buttonDown && !string.IsNullOrEmpty(reason))
            {
                ErrorMessage.AddMessage(reason);
            }
        }

        private bool TryDisplayNoPowerTooltip()
        {
            if (energyMixin.charge <= 0f)
            {
                HandReticle main = HandReticle.main;
                main.SetInteractText(noPowerText, translate: false);
                main.SetIcon(HandReticle.IconType.Default);
                return true;
            }
            return false;
        }

        public override bool OnRightHandDown()
        {
            if (Player.main.IsBleederAttached())
            {
                return true;
            }
            if (energyMixin.charge <= 0f)
            {
                return false;
            }
            uGUI_BuilderMenu.Show();
            return true;
        }

        public override bool OnLeftHandDown()
        {
            HandleInput();
            return isConstructing;
        }

        public override bool OnLeftHandHeld()
        {
            HandleInput();
            return isConstructing;
        }

        public override bool OnLeftHandUp()
        {
            HandleInput();
            return isConstructing;
        }

        private void UpdateText()
        {
            string buttonFormat = LanguageCache.GetButtonFormat("ConstructFormat", GameInput.Button.LeftHand);
            string buttonFormat2 = LanguageCache.GetButtonFormat("DeconstructFormat", GameInput.Button.Deconstruct);
            constructText = Language.main.GetFormat("ConstructDeconstructFormat", buttonFormat, buttonFormat2);
            deconstructText = buttonFormat2;
            noPowerText = Language.main.Get("NoPower");
        }

        private bool Construct(Constructable c, bool state)
        {
            if (c != null && !c.constructed && energyMixin.charge > 0f)
            {
                float amount = (state ? powerConsumptionConstruct : powerConsumptionDeconstruct) * Time.deltaTime;
                energyMixin.ConsumeEnergy(amount);
                bool constructed = c.constructed;
                if (state ? c.Construct() : c.Deconstruct())
                {
                    constructable = c;
                }
                else if (state && !constructed)
                {
                    Utils.PlayFMODAsset(completeSound, c.transform);
                }
                return true;
            }
            return false;
        }

        private void OnHover(Constructable constructable)
        {
            HandReticle main = HandReticle.main;
            if (constructable.constructed)
            {
                main.SetInteractText(Language.main.Get(constructable.techType), deconstructText, translate1: false, translate2: false, addInstructions: false);
                return;
            }
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(constructText);
            foreach (KeyValuePair<TechType, int> remainingResource in constructable.GetRemainingResources())
            {
                TechType key = remainingResource.Key;
                string text = Language.main.Get(key);
                int value = remainingResource.Value;
                if (value > 1)
                {
                    stringBuilder.AppendLine(Language.main.GetFormat("RequireMultipleFormat", text, value));
                }
                else
                {
                    stringBuilder.AppendLine(text);
                }
            }
            main.SetInteractText(Language.main.Get(constructable.techType), stringBuilder.ToString(), translate1: false, translate2: false, addInstructions: false);
            main.SetProgress(constructable.amount);
            main.SetIcon(HandReticle.IconType.Progress, 1.5f);
        }

        private void OnHover(BaseDeconstructable deconstructable)
        {
            HandReticle.main.SetInteractInfo(deconstructable.Name, deconstructText);
        }

        public override bool GetUsedToolThisFrame()
        {
            return isConstructing;
        }

        public override void OnHolster()
        {
            base.OnHolster();
            uGUI_BuilderMenu.Hide();
            Builder.End();
            SetBeamActive(state: false);
        }

        private void UpdateBar()
        {
            if (!(bar == null))
            {
                float value = ((energyMixin.capacity > 0f) ? (energyMixin.charge / energyMixin.capacity) : 0f);
                barMaterial.SetFloat(ShaderPropertyID._Amount, value);
            }
        }

        private void SetBeamActive(bool state)
        {
            if (beamLeft != null)
            {
                beamLeft.gameObject.SetActive(state);
            }
            if (beamRight != null)
            {
                beamRight.gameObject.SetActive(state);
            }
        }

        private void SetUsingAnimation(bool state)
        {
            if (!(animator == null) && animator.isActiveAndEnabled)
            {
                SafeAnimator.SetBool(animator, "using_tool", state);
            }
        }
    }
}
