using System.Collections;
using UnityEngine;

namespace UWE
{
    public class CoroutineHost : MonoBehaviour
    {
        private static CoroutineHost main;

        private static CoroutineHost Initialize()
        {
            if (!main)
            {
                main = new GameObject("CoroutineHost")
                {
                    hideFlags = HideFlags.HideInHierarchy
                }.AddComponent<CoroutineHost>();
            }
            return main;
        }

        public new static Coroutine StartCoroutine(IEnumerator coroutine)
        {
            return ((MonoBehaviour)Initialize()).StartCoroutine(coroutine);
        }
    }
}
