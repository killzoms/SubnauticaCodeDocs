using UnityEngine;

namespace AssemblyCSharp
{
    public class SolarPanel : HandTarget, IHandTarget
    {
        public PowerSource powerSource;

        public float maxDepth = 200f;

        [AssertNotNull]
        public AnimationCurve depthCurve;

        public override void Awake()
        {
            base.Awake();
        }

        private float GetDepthScalar()
        {
            float time = Mathf.Clamp01((maxDepth - Ocean.main.GetDepthOf(base.gameObject)) / maxDepth);
            return depthCurve.Evaluate(time);
        }

        private float GetSunScalar()
        {
            return DayNightCycle.main.GetLocalLightScalar();
        }

        private float GetRechargeScalar()
        {
            return GetDepthScalar() * GetSunScalar();
        }

        private void Update()
        {
            if (base.gameObject.GetComponent<Constructable>().constructed)
            {
                powerSource.power = Mathf.Clamp(powerSource.power + GetRechargeScalar() * DayNightCycle.main.deltaTime * 0.25f * 5f, 0f, powerSource.maxPower);
            }
        }

        public void OnHandHover(GUIHand hand)
        {
            if (base.gameObject.GetComponent<Constructable>().constructed)
            {
                HandReticle.main.SetInteractText(Language.main.GetFormat("SolarPanelStatus", Mathf.RoundToInt(GetRechargeScalar() * 100f), Mathf.RoundToInt(powerSource.GetPower()), Mathf.RoundToInt(powerSource.GetMaxPower())), translate: false);
                HandReticle.main.SetIcon(HandReticle.IconType.Hand);
            }
        }

        public void OnHandClick(GUIHand hand)
        {
        }
    }
}
