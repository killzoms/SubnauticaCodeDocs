using UnityEngine;

namespace AssemblyCSharp
{
    public class TransfuserUI : MonoBehaviour
    {
        public GUIText heldSerumText;

        public GUIText playerEffectsText;

        private void Awake()
        {
        }

        private void Update()
        {
            string text = "";
            GUIHand component = Player.main.gameObject.GetComponent<GUIHand>();
            if ((bool)component)
            {
                PlayerTool tool = component.GetTool();
                if (tool is Transfuser)
                {
                    text = ((Transfuser)tool).GetHUDText();
                }
            }
            heldSerumText.text = text;
            playerEffectsText.text = "";
        }
    }
}
