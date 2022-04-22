using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Text))]
    public class uGUI_GraphicsDeviceName : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<Text>().text = SystemInfo.graphicsDeviceName;
        }
    }
}
