using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_Block : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
