using UnityEngine;

namespace AssemblyCSharp
{
    public class ExploderOption : MonoBehaviour
    {
        public bool Plane2D;

        public Color CrossSectionVertexColor = Color.white;

        public Vector4 CrossSectionUV = new Vector4(0f, 0f, 1f, 1f);

        public bool SplitMeshIslands;

        public bool UseLocalForce;

        public float Force = 30f;

        public void DuplicateSettings(ExploderOption options)
        {
            options.Plane2D = Plane2D;
            options.CrossSectionVertexColor = CrossSectionVertexColor;
            options.CrossSectionUV = CrossSectionUV;
            options.SplitMeshIslands = SplitMeshIslands;
            options.UseLocalForce = UseLocalForce;
            options.Force = Force;
        }
    }
}
