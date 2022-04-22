using UnityEngine;

namespace AssemblyCSharp
{
    public class OpenURL : MonoBehaviour
    {
        public void Open(string URL)
        {
            Application.OpenURL(URL);
        }
    }
}
