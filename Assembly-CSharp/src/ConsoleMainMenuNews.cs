using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class ConsoleMainMenuNews : MonoBehaviour
    {
        public RawImage image;

        public Text header;

        public Text text;

        public Text date;

        public Text buttonText;

        public string URL;

        public void Start()
        {
            buttonText.text = Language.main.Get(buttonText.text);
        }

        public void Open()
        {
            Application.OpenURL(URL);
        }
    }
}
