using UnityEngine;

namespace AssemblyCSharp
{
    public class PrecursorTeleporterCollider : MonoBehaviour
    {
        private void OnTriggerEnter(Collider col)
        {
            if (!col.isTrigger)
            {
                GameObject entityRoot = global::UWE.Utils.GetEntityRoot(col.gameObject);
                if (!entityRoot)
                {
                    entityRoot = col.gameObject;
                }
                GameObject gameObject = null;
                if (entityRoot.Equals(Player.main.gameObject))
                {
                    gameObject = Player.main.gameObject;
                }
                if ((bool)entityRoot.GetComponent<Vehicle>())
                {
                    gameObject = entityRoot;
                }
                if ((bool)gameObject)
                {
                    SendMessageUpwards("BeginTeleportPlayer", gameObject, SendMessageOptions.RequireReceiver);
                }
            }
        }
    }
}
