using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class OctNodeMemTest : MonoBehaviour
    {
        public int numNodes = 1000;

        public Stack<VoxelandData.OctNode[]> blocks = new Stack<VoxelandData.OctNode[]>();

        private void Start()
        {
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                blocks.Push(new VoxelandData.OctNode[numNodes]);
                Debug.Log("estimated mem increase = " + (float)(default(VoxelandData.OctNode).EstimateBytes() * numNodes) / 1024f / 1024f + " MB");
            }
        }
    }
}
