using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class CyclopsUpgradeConsoleHUDManager : MonoBehaviour
    {
        [AssertNotNull]
        public Transform icons;

        [AssertNotNull]
        public SubRoot subRoot;

        [AssertNotNull]
        public LiveMixin liveMixin;

        [AssertNotNull]
        public Text healthCur;

        [AssertNotNull]
        public Text healthMax;

        [AssertNotNull]
        public Text energyCur;

        [AssertNotNull]
        public Text energyMax;

        private int lastMaxSubPowerDisplayed = -1;

        private int lastHealthMaxDisplayed = -1;

        public void RefreshUpgradeConsoleIcons(TechType[] slottedTypes)
        {
            ToggleAllIconsOff();
            foreach (TechType type in slottedTypes)
            {
                ToggleIcon(type, state: true);
            }
        }

        private void OnEnable()
        {
            lastHealthMaxDisplayed = -1;
            lastMaxSubPowerDisplayed = -1;
            InvokeRepeating("RefreshScreen", 0f, 2f);
        }

        private void OnDisable()
        {
            CancelInvoke();
        }

        private void RefreshScreen()
        {
            healthCur.text = IntStringCache.GetStringForInt((int)liveMixin.health);
            int num = (int)liveMixin.maxHealth;
            if (lastHealthMaxDisplayed != num)
            {
                healthMax.text = "/" + IntStringCache.GetStringForInt(num);
                lastHealthMaxDisplayed = num;
            }
            energyCur.text = IntStringCache.GetStringForInt((int)subRoot.powerRelay.GetPower());
            int num2 = (int)subRoot.powerRelay.GetMaxPower();
            if (lastMaxSubPowerDisplayed != num2)
            {
                energyMax.text = "/" + IntStringCache.GetStringForInt(num2);
                lastMaxSubPowerDisplayed = num2;
            }
        }

        private void ToggleIcon(TechType type, bool state)
        {
            foreach (Transform icon in icons)
            {
                CyclopsUpgradeConsoleIcon component = icon.GetComponent<CyclopsUpgradeConsoleIcon>();
                if (component != null && component.upgradeType == type)
                {
                    icon.gameObject.SetActive(state);
                    break;
                }
            }
        }

        private void ToggleAllIconsOff()
        {
            foreach (Transform icon in icons)
            {
                icon.gameObject.SetActive(value: false);
            }
        }
    }
}
