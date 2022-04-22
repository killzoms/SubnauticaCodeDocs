using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(GUIText))]
    public class HeldSampleName : MonoBehaviour
    {
        private void Update()
        {
            Player localPlayerComp = Utils.GetLocalPlayerComp();
            if (!localPlayerComp)
            {
                return;
            }
            Transfuser transfuser = (Transfuser)localPlayerComp.gameObject.GetComponent<GUIHand>().GetTool();
            if ((bool)transfuser)
            {
                string text = transfuser.heldSampleName;
                if (text != "")
                {
                    text += " (LMB to inject)";
                }
                base.gameObject.GetComponent<GUIText>();
                GetComponent<GUIText>().text = text;
            }
        }
    }
}
