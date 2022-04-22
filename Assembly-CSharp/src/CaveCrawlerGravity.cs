using UnityEngine;

namespace AssemblyCSharp
{
    public class CaveCrawlerGravity : MonoBehaviour
    {
        [AssertNotNull]
        public CaveCrawler caveCrawler;

        [AssertNotNull]
        public LiveMixin liveMixin;

        [AssertNotNull]
        public Rigidbody crawlerRigidbody;

        private void FixedUpdate()
        {
            crawlerRigidbody.useGravity = false;
            bool flag = base.transform.position.y >= 0f;
            bool num = caveCrawler.IsOnSurface() && liveMixin.IsAlive();
            if (!num)
            {
                float num2 = (flag ? 9.81f : 2.7f);
                crawlerRigidbody.AddForce(-Vector3.up * Time.deltaTime * num2, ForceMode.VelocityChange);
            }
            else
            {
                float num3 = 10f;
                Vector3 surfaceNormal = caveCrawler.GetSurfaceNormal();
                crawlerRigidbody.AddForce(-surfaceNormal * num3);
            }
            float num4 = (num ? 1.6f : 0.03f);
            if (!flag)
            {
                num4 += 0.3f;
            }
            crawlerRigidbody.drag = num4;
        }
    }
}
