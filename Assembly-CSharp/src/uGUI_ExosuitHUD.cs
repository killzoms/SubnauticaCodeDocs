using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_ExosuitHUD : MonoBehaviour
    {
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

        [AssertNotNull]
        public Image imageThrust;

        private int lastHealth = int.MinValue;

        private int lastPower = int.MinValue;

        private float lastThrust = float.MinValue;

        private int lastTemperature = int.MinValue;

        private float temperatureSmoothValue = float.MinValue;

        private float temperatureVelocity;

        private void Awake()
        {
            imageThrust.material = new Material(imageThrust.material);
        }

        private void Update()
        {
            PDA pDA = null;
            Player main = Player.main;
            bool flag = false;
            if (main != null)
            {
                pDA = main.GetPDA();
                flag = main.inExosuit;
            }
            bool flag2 = flag && (pDA == null || !pDA.isInUse);
            if (root.activeSelf != flag2)
            {
                root.SetActive(flag2);
            }
            if (flag2)
            {
                Exosuit obj = main.GetVehicle() as Exosuit;
                obj.GetHUDValues(out var health, out var power, out var thrust);
                float temperature = obj.GetTemperature();
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
                if (lastThrust != thrust)
                {
                    lastThrust = thrust;
                    imageThrust.material.SetFloat(ShaderPropertyID._Amount, lastThrust);
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
