using UnityEngine;

namespace AssemblyCSharp
{
    public class DegenTriTest : MonoBehaviour
    {
        public int numTris = 1000;

        private MeshFilter filter;

        private void Start()
        {
            int[] triangles = new int[3 * numTris];
            Vector3[] array = new Vector3[1000];
            filter = base.gameObject.GetComponent<MeshFilter>();
            filter.sharedMesh = new Mesh();
            filter.mesh.vertices = array;
            filter.mesh.normals = array;
            filter.mesh.triangles = triangles;
        }

        private void OnDestroy()
        {
            Object.DestroyImmediate(filter.sharedMesh);
        }
    }
}
