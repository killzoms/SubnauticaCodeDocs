using UnityEngine;

namespace AssemblyCSharp
{
    public class PrecursorDisableGunTerminalArea : MonoBehaviour
    {
        public PrecursorDisableGunTerminal terminal;

        private void OnTriggerEnter(Collider other)
        {
            GameObject entityRoot = global::UWE.Utils.GetEntityRoot(other.gameObject);
            if (!entityRoot)
            {
                entityRoot = other.gameObject;
            }
            if (entityRoot.GetComponent<Player>() != null)
            {
                terminal.OnTerminalAreaEnter();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            GameObject entityRoot = global::UWE.Utils.GetEntityRoot(other.gameObject);
            if (!entityRoot)
            {
                entityRoot = other.gameObject;
            }
            if (entityRoot.GetComponent<Player>() != null)
            {
                terminal.OnTerminalAreaExit();
            }
        }
    }
}
