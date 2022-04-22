using UnityEngine;

namespace AssemblyCSharp
{
    public class AirBladder : PlayerTool, IEquippable
    {
        private float oxygen;

        private const float maxOxygen = 5f;

        public FMOD_CustomEmitter inflate;

        public FMOD_CustomEmitter deflate;

        public FMOD_CustomEmitter deflateAboveWater;

        public FMOD_CustomEmitter loop;

        public GameObject firstPersonBubbleParticlesPrefab;

        public Animator animator;

        public GameObject bubblesExitPoint;

        private float forceConstant = 0.4f;

        private bool firstUse;

        private bool inflating;

        private bool deflating;

        private float inflateStartTime;

        private float lastTimeRMBUp;

        private const float kTransferPerSecond = 2f;

        private void Start()
        {
        }

        public override bool OnRightHandDown()
        {
            if (Player.main.IsBleederAttached())
            {
                return true;
            }
            if (!inflating)
            {
                inflate.Stop();
                inflate.Play();
                inflating = true;
                deflating = false;
                inflateStartTime = Time.time;
            }
            else if (lastTimeRMBUp > inflateStartTime)
            {
                if (Player.main.IsUnderwater())
                {
                    deflate.Play();
                }
                else
                {
                    deflateAboveWater.Play();
                }
                deflating = true;
                inflating = false;
            }
            return true;
        }

        private void Update()
        {
            if (usingPlayer != null)
            {
                UpdateInflateState();
                if (!usingPlayer.GetRightHandDown())
                {
                    lastTimeRMBUp = Time.time;
                }
            }
        }

        private void OnEnable()
        {
            SafeAnimator.SetFloat(animator, "inflate", oxygen / 5f);
        }

        public override void OnDraw(Player p)
        {
            TechType techType = pickupable.GetTechType();
            firstUse = !p.IsToolUsed(techType);
            base.OnDraw(p);
        }

        public override void OnHolster()
        {
            base.OnHolster();
            inflating = false;
            deflating = false;
            oxygen = 0f;
        }

        private void UpdateInflateState()
        {
            OxygenManager component = Player.main.GetComponent<OxygenManager>();
            float a = Time.deltaTime * 2f;
            if (inflating)
            {
                if (oxygen < 5f)
                {
                    float num = Mathf.Min(a, 5f - oxygen);
                    if (num > 0f)
                    {
                        float num2 = component.RemoveOxygen(num);
                        oxygen += num2;
                        SafeAnimator.SetFloat(animator, "inflate", oxygen / 5f);
                    }
                    else
                    {
                        inflating = false;
                    }
                }
            }
            else
            {
                if (!deflating)
                {
                    return;
                }
                float num3 = Mathf.Min(a, oxygen);
                if (num3 > 0f)
                {
                    oxygen -= num3;
                    if (Player.main.IsUnderwater())
                    {
                        Utils.PlayOneShotPS(firstPersonBubbleParticlesPrefab, bubblesExitPoint.transform.position, Quaternion.identity);
                    }
                    SafeAnimator.SetFloat(animator, "inflate", oxygen / 5f);
                }
                else
                {
                    deflating = false;
                }
            }
        }

        public void ApplyBuoyancyForce()
        {
            if (!Mathf.Approximately(oxygen, 0f))
            {
                Pickupable component = base.gameObject.GetComponent<Pickupable>();
                GameObject gameObject = (Inventory.main.Contains(component) ? Player.main.gameObject : base.gameObject);
                if (gameObject.transform.position.y < Ocean.main.GetOceanLevel() - 1f)
                {
                    Rigidbody component2 = gameObject.GetComponent<Rigidbody>();
                    Vector3 force = Vector3.up * (oxygen / 5f) * forceConstant;
                    component2.AddForce(force, ForceMode.VelocityChange);
                }
            }
        }

        private void FixedUpdate()
        {
            ApplyBuoyancyForce();
        }

        public void OnEquip(GameObject sender, string slot)
        {
            if (base.isDrawn && firstUse)
            {
                animator.SetBool("using_tool_first", value: true);
            }
        }

        public void OnUnequip(GameObject sender, string slot)
        {
            if (firstUse)
            {
                animator.SetBool("using_tool_first", value: false);
            }
        }

        public void UpdateEquipped(GameObject sender, string slot)
        {
        }
    }
}
