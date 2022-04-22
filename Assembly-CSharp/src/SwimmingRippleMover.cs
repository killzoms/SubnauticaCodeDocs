using UnityEngine;

namespace AssemblyCSharp
{
    public class SwimmingRippleMover : RippleMover
    {
        private float lastSpeedFactor;

        public override float GetRippleAmount()
        {
            float num = (lastSpeedFactor = global::UWE.Utils.Slerp(lastSpeedFactor, Player.main.IsSwimming() ? 1f : 0f, 2f * Time.deltaTime));
            return kRippleAmount * num;
        }
    }
}
