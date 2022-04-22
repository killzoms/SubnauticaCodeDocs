using UnityEngine;

namespace AssemblyCSharp
{
    public class GenericLoot : MonoBehaviour
    {
        public static bool IsInSub(GameObject go)
        {
            if (go != null && go.GetComponentInParent<SubRoot>() != null)
            {
                return true;
            }
            return Player.main.IsInSub();
        }

        public static void SetInteriorLayer(GameObject go)
        {
            SetLayer(go, isInSub: true);
        }

        public static void SetLayer(GameObject go)
        {
            SetLayer(go, IsInSub(go));
        }

        public static void SetLayer(GameObject go, bool isInSub)
        {
            if (go == null)
            {
                return;
            }
            int uI = LayerID.UI;
            int num;
            int layer;
            if (isInSub)
            {
                num = LayerID.Default;
                layer = LayerID.Interior;
            }
            else
            {
                num = LayerID.Interior;
                layer = LayerID.Default;
            }
            Renderer[] componentsInChildren = go.GetComponentsInChildren<Renderer>(includeInactive: true);
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                GameObject gameObject = componentsInChildren[i].gameObject;
                if (gameObject.layer == num && gameObject.layer != uI)
                {
                    gameObject.layer = layer;
                }
            }
        }
    }
}
