using UnityEngine;

namespace AssemblyCSharp
{
    public class BuiltEffectController : MonoBehaviour
    {
        private const string materialPath = "Materials/builtfade";

        private const string propertyName = "_FadeAmount";

        public float duration = 1f;

        private Renderer[] renderers;

        private Sequence sequence;

        private Material material;

        private int propertyID;

        private void Awake()
        {
            sequence = new Sequence();
            propertyID = Shader.PropertyToID("_FadeAmount");
            Material material = Resources.Load<Material>("Materials/builtfade");
            if (material != null)
            {
                this.material = new Material(material);
            }
            else
            {
                Debug.LogError("Material at path 'Materials/builtfade' is not found!");
            }
        }

        private void Start()
        {
            renderers = MaterialExtensions.AssignMaterial(base.gameObject, material);
            sequence.Set(duration, current: false, target: true, DestroyCallback);
        }

        private void Update()
        {
            sequence.Update();
            if (material != null && renderers != null)
            {
                MaterialExtensions.SetFloat(renderers, propertyID, sequence.t);
            }
        }

        private void DestroyCallback()
        {
            Object.Destroy(base.gameObject);
        }
    }
}
