using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_SeamothHUD : MonoBehaviour
    {
        public const float temperatureSmoothTime = 1f;

        [AssertNotNull]
        public GameObject root;

        [AssertNotNull]
        public Text textHealth;

        [AssertNotNull]
        public Text textPower;

        [AssertNotNull]
        public Text textTemperature;

        [AssertNotNull]
        public Text textTemperatureSuffix;

        private int lastHealth = int.MinValue;

        private int lastPower = int.MinValue;

        private int lastTemperature = int.MinValue;

        private float temperatureSmoothValue = float.MinValue;

        private float temperatureVelocity;

        private void Update()
        {
            SeaMoth seaMoth = null;
            PDA pDA = null;
            Player main = Player.main;
            if (main != null)
            {
                seaMoth = main.GetVehicle() as SeaMoth;
                pDA = main.GetPDA();
            }
            bool flag = seaMoth != null && (pDA == null || !pDA.isInUse);
            if (root.activeSelf != flag)
            {
                root.SetActive(flag);
            }
            if (flag)
            {
                seaMoth.GetHUDValues(out var health, out var power);
                float temperature = seaMoth.GetTemperature();
                int num = Mathf.CeilToInt(health * 100f);
                if (lastHealth != num)
                {
                    lastHealth = num;
                    textHealth.text = IntStringCache.GetStringForInt(lastHealth);
                }
                int num2 = Mathf.CeilToInt(power * 100f);
                if (lastPower != num2)
                {
                    lastPower = num2;
                    textPower.text = IntStringCache.GetStringForInt(lastPower);
                }
                temperatureSmoothValue = ((temperatureSmoothValue < -10000f) ? temperature : Mathf.SmoothDamp(temperatureSmoothValue, temperature, ref temperatureVelocity, 1f));
                int num3 = Mathf.CeilToInt(temperatureSmoothValue);
                if (lastTemperature != num3)
                {
                    lastTemperature = num3;
                    textTemperature.text = IntStringCache.GetStringForInt(lastTemperature);
                    textTemperatureSuffix.text = Language.main.GetFormat("ThermometerFormat");
                }
            }
        }
    }
}
