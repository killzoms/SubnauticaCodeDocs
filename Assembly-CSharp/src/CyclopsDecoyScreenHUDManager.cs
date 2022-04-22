using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class CyclopsDecoyScreenHUDManager : MonoBehaviour
    {
        [AssertNotNull]
        public Text curCountText;

        [AssertNotNull]
        public Text maxCountText;

        [AssertNotNull]
        public CyclopsDecoyManager decoyManager;

        [AssertNotNull]
        public Animator animator;

        public void UpdateDecoyScreen()
        {
            curCountText.text = decoyManager.decoyCount.ToString();
            maxCountText.text = "/" + decoyManager.decoyMax;
        }

        public void SlotNewDecoy()
        {
            animator.SetTrigger("Pulse");
        }
    }
}
