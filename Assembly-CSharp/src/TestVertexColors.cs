using UnityEngine;

namespace AssemblyCSharp
{
    public class TestVertexColors : MonoBehaviour
    {
        private void Start()
        {
            Mesh mesh = GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            Color32[] array = new Color32[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                array[i] = Color.blue;
            }
            mesh.colors32 = array;
        }
    }
}
