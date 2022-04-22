using UnityEngine;

namespace AssemblyCSharp
{
    public class ScreenLock : MonoBehaviour
    {
        private void Start()
        {
            global::UWE.Utils.lockCursor = true;
        }
    }
}
