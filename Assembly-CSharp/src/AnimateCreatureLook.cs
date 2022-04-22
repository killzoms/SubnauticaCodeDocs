using UnityEngine;

namespace AssemblyCSharp
{
    public class AnimateCreatureLook : MonoBehaviour
    {
        [AssertNotNull]
        public Animator animator;

        [AssertNotNull]
        public LastTarget lastTarget;

        [AssertNotNull]
        public Transform rootTransform;

        [AssertNotNull]
        public Transform lookDirectionTransform;

        public float animationMaxLookPitch = 90f;

        public float animationMaxLookTilt = 90f;

        public float rotationSpeed = 1f;

        public float rememberTargetTime = 20f;

        public float fov;

        private float prevLookPitch;

        private float prevLookTilt;

        private static readonly int animLookPitch = Animator.StringToHash("look_pitch");

        private static readonly int animLookTilt = Animator.StringToHash("look_tilt");

        public void Update()
        {
            float value = 0f;
            float value2 = 0f;
            if (lastTarget != null && lastTarget.target != null && Time.time < lastTarget.targetTime + rememberTargetTime)
            {
                Vector3 normalized = (lastTarget.target.transform.position - rootTransform.position).normalized;
                if (Vector3.Dot(normalized, rootTransform.forward) > fov)
                {
                    Vector3 eulerAngles = Quaternion.LookRotation(lookDirectionTransform.InverseTransformDirection(normalized)).eulerAngles;
                    value = prevLookPitch - Mathf.DeltaAngle(0f, eulerAngles.x);
                    value2 = prevLookTilt + Mathf.DeltaAngle(0f, eulerAngles.y);
                }
            }
            value = Mathf.Clamp(value, 0f - animationMaxLookPitch, animationMaxLookPitch);
            value2 = Mathf.Clamp(value2, 0f - animationMaxLookTilt, animationMaxLookTilt);
            value = Mathf.Lerp(prevLookPitch, value, rotationSpeed * Time.deltaTime);
            value2 = Mathf.Lerp(prevLookTilt, value2, rotationSpeed * Time.deltaTime);
            if (animationMaxLookPitch > 0f)
            {
                animator.SetFloat(animLookPitch, value / animationMaxLookPitch);
            }
            if (animationMaxLookTilt > 0f)
            {
                animator.SetFloat(animLookTilt, value2 / animationMaxLookTilt);
            }
            prevLookPitch = value;
            prevLookTilt = value2;
        }

        public void OnKill()
        {
            animator.SetFloat(animLookPitch, 0f);
            animator.SetFloat(animLookTilt, 0f);
        }
    }
}
