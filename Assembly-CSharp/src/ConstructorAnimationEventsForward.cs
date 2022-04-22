using UnityEngine;

namespace AssemblyCSharp
{
    public class ConstructorAnimationEventsForward : MonoBehaviour
    {
        [AssertNotNull]
        public Constructor constructor;

        public void OnDeployAnimationEnd()
        {
            constructor.OnDeployAnimationEnd();
        }
    }
}
