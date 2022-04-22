using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class BaseRoot : SubRoot
    {
        [AssertNotNull]
        public BaseFloodSim flood;

        [AssertNotNull]
        public Base baseComp;

        private const float consumePowerInterval = 1f;

        public override void Start()
        {
            base.Start();
        }

        public override bool IsUnderwater(Vector3 wsPos)
        {
            return flood.IsUnderwater(wsPos);
        }

        public override bool IsLeaking()
        {
            return flood.tIsLeaking();
        }

        private void ConsumePower()
        {
            if (!powerRelay)
            {
                return;
            }
            float num = 0f;
            Int3.RangeEnumerator allCells = baseComp.AllCells;
            while (allCells.MoveNext())
            {
                Int3 current = allCells.Current;
                if (baseComp.GetCellMask(current))
                {
                    num += baseComp.GetCellPowerConsumption(current);
                }
            }
            num *= 1f;
            DayNightCycle main = DayNightCycle.main;
            if ((bool)main)
            {
                num *= main.dayNightSpeed;
            }
            powerRelay.ConsumeEnergy(num, out var _);
        }
    }
}
