using System.Collections;
using UnityEngine;

namespace AssemblyCSharp
{
    public class RegeneratePowerSource : MonoBehaviour
    {
        [AssertNotNull]
        public PowerSource powerSource;

        public float regenerationThreshhold = 25f;

        public float regenerationInterval = 20f;

        public float regenerationAmount = 1f;

        private IEnumerator Start()
        {
            while (true)
            {
                yield return new WaitForSeconds(regenerationInterval);
                if (powerSource.GetPower() < regenerationThreshhold)
                {
                    powerSource.SetPower(Mathf.Min(regenerationThreshhold, powerSource.GetPower() + regenerationAmount));
                }
            }
        }

        public void OnHover(HandTargetEventData eventData)
        {
            string format = Language.main.GetFormat("PowerCellStatus", Mathf.FloorToInt(powerSource.GetPower()), Mathf.FloorToInt(powerSource.GetMaxPower()));
            HandReticle.main.SetInteractText("RegenPowerCell", format, translate1: true, translate2: false, addInstructions: false);
        }
    }
}
