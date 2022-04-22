using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_TextGradient : MonoBehaviour, IMeshModifier
    {
        public Color32 color0 = Color.white;

        public Color32 color1 = Color.white;

        public Color32 color2 = Color.black;

        public Color32 color3 = Color.black;

        public bool affectAlpha;

        public void ModifyMesh(Mesh mesh)
        {
        }

        public void ModifyMesh(VertexHelper vh)
        {
            UIVertex vertex = UIVertex.simpleVert;
            int i = 0;
            for (int currentVertCount = vh.currentVertCount; i < currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                Color32 color = (i % 4) switch
                {
                    0 => color0, 
                    1 => color1, 
                    2 => color2, 
                    _ => color3, 
                };
                if (!affectAlpha)
                {
                    color.a = vertex.color.a;
                }
                vertex.color = color;
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
