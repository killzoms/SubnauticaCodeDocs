using UnityEngine;

namespace AssemblyCSharp
{
    public class UpgradeCyclopsPower : MonoBehaviour
    {
        private void Start()
        {
            PowerSource component = GetComponent<PowerSource>();
            if (component != null)
            {
                BatterySource component2 = GetComponent<BatterySource>();
                bool flag = false;
                if (component2 != null && component2.SpawnDefault(component.power / component.maxPower))
                {
                    flag = true;
                }
                Object.Destroy(component);
                if (flag)
                {
                    Object.Destroy(this);
                }
            }
        }
    }
}
