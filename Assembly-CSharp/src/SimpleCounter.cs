using UnityEngine;

namespace AssemblyCSharp
{
    public class SimpleCounter : MonoBehaviour
    {
        public static float delay = 0.1f;

        public int calls;

        public void Do()
        {
            calls++;
        }
    }
}
