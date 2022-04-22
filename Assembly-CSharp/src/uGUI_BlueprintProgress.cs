using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(RectTransform))]
    public class uGUI_BlueprintProgress : MonoBehaviour, ILayoutIgnorer
    {
        [AssertNotNull]
        public Graphic bar;

        [AssertNotNull]
        public Text amount;

        private int unlocked = -1;

        private int total = -1;

        public bool ignoreLayout => true;

        private void Awake()
        {
            bar.material = new Material(bar.material);
        }

        public void SetValue(int unlocked, int total)
        {
            Material material = bar.material;
            if (this.total != total)
            {
                this.total = total;
                material.SetFloat(ShaderPropertyID._Subdivisions, total);
            }
            material.SetFloat(ShaderPropertyID._Amount, (float)unlocked / (float)total);
            if (this.unlocked != unlocked || this.total != total)
            {
                this.unlocked = unlocked;
                amount.text = Language.main.GetFormat("BlueprintFragmentProgressFormat", unlocked, total);
            }
        }
    }
}
