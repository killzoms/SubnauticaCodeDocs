using UnityEngine;

namespace AssemblyCSharp
{
    public class CyclopsSubNameScreen : MonoBehaviour
    {
        [AssertNotNull]
        public Animator animator;

        [AssertNotNull]
        public GameObject content;

        private void ContentOn()
        {
            content.SetActive(value: true);
        }

        private void ContentOff()
        {
            content.SetActive(value: false);
        }

        private void OnTriggerEnter(Collider col)
        {
            if (col.gameObject.Equals(Player.main.gameObject))
            {
                animator.SetBool("PanelActive", value: true);
                ContentOn();
            }
        }

        private void OnTriggerExit(Collider col)
        {
            if (col.gameObject.Equals(Player.main.gameObject))
            {
                animator.SetBool("PanelActive", value: false);
                Invoke("ContentOff", 0.5f);
            }
        }
    }
}
