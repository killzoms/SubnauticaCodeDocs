using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_BasicColorSwap : MonoBehaviour
    {
        private void Start()
        {
        }

        private void Update()
        {
        }

        public void makeTextBlack()
        {
            Text[] componentsInChildren = GetComponentsInChildren<Text>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].color = Color.black;
            }
        }

        public void makeTextWhite()
        {
            Text[] componentsInChildren = GetComponentsInChildren<Text>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].color = Color.white;
            }
        }
    }
}
