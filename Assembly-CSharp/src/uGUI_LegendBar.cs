using Gendarme;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(HorizontalLayoutGroup))]
    public class uGUI_LegendBar : MonoBehaviour
    {
        private struct legendButtonData
        {
            public string buttonStr;

            public string descriptionStr;
        }

        [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidAlwaysNullFieldRule")]
        [SerializeField]
        private uGUI_LegendItem[] _legendButtons;

        private static legendButtonData[] _legendDataList = new legendButtonData[4];

        private void Start()
        {
        }

        private void Update()
        {
            if (!GameInput.IsPrimaryDeviceGamepad())
            {
                for (int i = 0; i < _legendButtons.Length; i++)
                {
                    _legendButtons[i].gameObject.SetActive(value: false);
                }
                return;
            }
            for (int j = 0; j < _legendButtons.Length; j++)
            {
                _legendButtons[j].gameObject.SetActive(value: true);
                _legendButtons[j].SetButtonStr(_legendDataList[j].buttonStr);
                _legendButtons[j].SetDescriptionStr(_legendDataList[j].descriptionStr);
            }
        }

        public static void ClearButtons()
        {
            for (int i = 0; i < _legendDataList.Length; i++)
            {
                _legendDataList[i].buttonStr = string.Empty;
                _legendDataList[i].descriptionStr = string.Empty;
            }
        }

        public static void ChangeButton(int index, string buttonStr, string descriptionStr)
        {
            if (index < _legendDataList.Length)
            {
                _legendDataList[index].buttonStr = buttonStr;
                _legendDataList[index].descriptionStr = descriptionStr;
            }
        }
    }
}
