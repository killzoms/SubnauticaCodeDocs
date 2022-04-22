using UnityEngine;

namespace AssemblyCSharp
{
    public class MainMenuSubscriptionFeedback : MonoBehaviour
    {
        private void OnEnable()
        {
            Invoke("Dismiss", 3f);
        }

        private void Dismiss()
        {
            base.gameObject.SetActive(value: false);
        }
    }
}
