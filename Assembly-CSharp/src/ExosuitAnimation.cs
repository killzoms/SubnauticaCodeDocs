using UnityEngine;

namespace AssemblyCSharp
{
    public class ExosuitAnimation : MonoBehaviour
    {
        public Exosuit exosuit;

        public void OnStep(int side)
        {
        }

        public void OnPlayerEntered()
        {
            exosuit.OnPlayerEntered();
        }
    }
}
