using UnityEngine;

namespace AssemblyCSharp
{
    public class BasePowerRelay : PowerRelay
    {
        [AssertNotNull]
        public SubRoot subRoot;

        [AssertNotNull]
        public Base baseComp;

        [AssertNotNull]
        public VoiceNotification powerUpInside;

        [AssertNotNull]
        public VoiceNotification powerUpOutside;

        [AssertNotNull]
        public VoiceNotification powerDownInside;

        [AssertNotNull]
        public VoiceNotification powerDownOutside;

        private bool prevHasPower;

        public override void Start()
        {
            base.Start();
            prevHasPower = IsPowered();
        }

        protected override void UpdatePowerState()
        {
            base.UpdatePowerState();
            bool flag = IsPowered();
            if (prevHasPower && !flag)
            {
                Invoke("PlayPowerDownIfUnpowered", 0.5f);
            }
            else if (!prevHasPower && flag)
            {
                Invoke("PlayPowerUpIfPowered", 0.5f);
            }
            prevHasPower = IsPowered();
        }

        public override Vector3 GetConnectPoint(Vector3 fromPosition)
        {
            return baseComp.GetClosestPoint(fromPosition);
        }

        private void PlayPowerDownIfUnpowered()
        {
            if (!IsPowered())
            {
                if (Player.main.GetCurrentSub() == subRoot)
                {
                    powerDownInside.Play();
                }
                else
                {
                    powerDownOutside.Play();
                }
            }
        }

        private void PlayPowerUpIfPowered()
        {
            if (IsPowered())
            {
                if (Player.main.GetCurrentSub() == subRoot)
                {
                    powerUpInside.Play();
                }
                else
                {
                    powerUpOutside.Play();
                }
            }
        }
    }
}
