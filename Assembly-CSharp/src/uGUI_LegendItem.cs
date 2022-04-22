using Gendarme;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_LegendItem : MonoBehaviour
    {
        [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidAlwaysNullFieldRule")]
        [SerializeField]
        private Text descriptionText;

        [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidAlwaysNullFieldRule")]
        [SerializeField]
        private Text buttonText;

        private void Start()
        {
        }

        private void Update()
        {
        }

        public void SetButtonStr(string str)
        {
            if (buttonText != null)
            {
                buttonText.text = str;
            }
        }

        public void SetDescriptionStr(string str)
        {
            if (descriptionText != null)
            {
                descriptionText.text = str;
            }
        }
    }
}
