using System;
using UnityEngine;

namespace AssemblyCSharp
{
    public class VFXFallingChunk : MonoBehaviour
    {
        public float minSpeed = 1f;

        public float maxSpeed = 10f;

        public float initSpeedDuration = 8f;

        public Vector3 constantSpeed = new Vector3(0f, -9f, 0f);

        public float minDistance;

        public float maxDistance = 1700f;

        public float minScale;

        public float maxScale = 1f;

        public float scaleRandomness = 0.5f;

        public float rotationSpeed;

        private float initSpeed;

        private Vector3 initScale;

        private Vector3 realPos;

        private Vector3 direction;

        private float animTime;

        private bool isFadingOut;

        private float fadingSpeed = 2f;

        private void Awake()
        {
            animTime = initSpeedDuration;
            initSpeed = global::UnityEngine.Random.Range(minSpeed, maxSpeed);
            initScale = Vector3.one * global::UnityEngine.Random.Range(1f - scaleRandomness, 1f + scaleRandomness);
            direction = new Vector3(global::UnityEngine.Random.value - 0.5f, global::UnityEngine.Random.value * 0.2f - 0.1f, global::UnityEngine.Random.value - 0.5f) * 2f;
            realPos = base.transform.position;
        }

        private void FixedUpdate()
        {
            animTime -= Time.fixedDeltaTime;
            if (isFadingOut)
            {
                fadingSpeed -= Time.fixedDeltaTime;
            }
            Vector3 vector = realPos + constantSpeed * Mathf.Max(fadingSpeed, 0f);
            vector += direction * initSpeed * Mathf.Max(0f, animTime / initSpeedDuration);
            if (!isFadingOut)
            {
                Vector3 normalized = (vector - realPos).normalized;
                base.transform.rotation = Quaternion.Slerp(base.transform.rotation, Quaternion.LookRotation(normalized, realPos - MainCamera.camera.transform.position), Time.fixedDeltaTime * rotationSpeed);
            }
            realPos = vector;
        }

        private void Update()
        {
            Vector3 position = MainCamera.camera.transform.position;
            float num = Vector3.Distance(realPos, position);
            float maxDistanceDelta = 0f;
            if (num > maxDistance)
            {
                maxDistanceDelta = num - maxDistance;
            }
            if (num < minDistance || realPos.y < 100f)
            {
                base.gameObject.GetComponent<ParticleSystem>().Stop();
                global::UnityEngine.Object.Destroy(base.gameObject, 12f);
                isFadingOut = true;
            }
            base.transform.position = Vector3.MoveTowards(realPos, position, maxDistanceDelta);
            float num2 = Vector3.Distance(base.transform.position, position);
            float num3 = 2f * Mathf.Tan(MainCamera.camera.fieldOfView * 0.5f * ((float)Math.PI / 180f));
            float num4 = num3 * num;
            float num5 = num3 * num2;
            float num6 = (num4 - num5) / num4;
            base.transform.localScale = initScale * Mathf.Clamp(1f - num6, minScale, maxScale);
        }
    }
}
