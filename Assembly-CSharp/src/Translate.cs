using UnityEngine;

namespace AssemblyCSharp
{
    public class Translate : MonoBehaviour
    {
        public Vector3 velocity;

        private void Start()
        {
        }

        private void Update()
        {
            base.transform.position += velocity * Time.deltaTime;
        }
    }
}
