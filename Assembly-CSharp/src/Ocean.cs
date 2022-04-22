using UnityEngine;

namespace AssemblyCSharp
{
    public class Ocean : MonoBehaviour
    {
        public enum DepthClass
        {
            Surface,
            Safe,
            Unsafe,
            Crush
        }

        public static Ocean main;

        private void Awake()
        {
            main = this;
        }

        public float GetOceanLevel()
        {
            return base.transform.position.y;
        }

        public float GetDepthOf(GameObject obj)
        {
            if (obj == null)
            {
                return 0f;
            }
            return Mathf.Max(0f, GetOceanLevel() - obj.transform.position.y);
        }
    }
}
