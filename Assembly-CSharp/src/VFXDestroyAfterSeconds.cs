using UnityEngine;

namespace AssemblyCSharp
{
    public class VFXDestroyAfterSeconds : MonoBehaviour
    {
        public float lifeTime;

        public bool destroyMaterials;

        private float spawnTime;

        private void OnEnable()
        {
            spawnTime = Time.realtimeSinceStartup;
        }

        private void LateUpdate()
        {
            if (Time.realtimeSinceStartup - spawnTime >= lifeTime)
            {
                global::UWE.Utils.DestroyWrap(base.gameObject);
            }
        }

        private void OnDestroy()
        {
            if (destroyMaterials)
            {
                Object.DestroyImmediate(GetComponent<Renderer>().material);
            }
        }
    }
}
