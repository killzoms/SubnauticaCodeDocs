using System;
using UnityEngine;

namespace AssemblyCSharp
{
    public class DiveReelNode : MonoBehaviour
    {
        [AssertNotNull]
        public Transform arrow;

        [AssertNotNull]
        public Rigidbody rb;

        [AssertNotNull]
        public Transform firstNodeHolder;

        [AssertNotNull]
        public Transform standardNodeHolder;

        [AssertNotNull]
        public FMOD_CustomEmitter blinkSFX;

        [NonSerialized]
        public Transform previousArrowPos;

        public bool firstArrow;

        public float arrowDeployDelay = 2f;

        public float blinkDelay = 3f;

        private Transform useTransform;

        private bool arrowDeploy;

        private float arrowScale;

        private bool destroySelf;

        private float selfScale = 1f;

        private Material arrowMat;

        private Color baseColor;

        private bool blinking;

        private void Start()
        {
            useTransform = (firstArrow ? firstNodeHolder : arrow);
            Invoke("DeployArrow", arrowDeployDelay);
            Vector3 vector = ReturnDirectionToPrevious();
            if (vector != Vector3.zero)
            {
                arrow.rotation = Quaternion.LookRotation(vector);
            }
            useTransform.localScale = Vector3.zero;
            arrowMat = useTransform.GetComponentInChildren<MeshRenderer>().material;
            if ((bool)arrowMat)
            {
                baseColor = arrowMat.GetColor(ShaderPropertyID._Color);
            }
            firstNodeHolder.gameObject.SetActive(firstArrow);
            standardNodeHolder.gameObject.SetActive(!firstArrow);
        }

        public void Blink(float delay)
        {
            Invoke("SetBlink", delay);
            Invoke("UnsetBlink", delay + blinkDelay);
        }

        private void SetBlink()
        {
            blinking = true;
        }

        private void UnsetBlink()
        {
            blinking = false;
        }

        private void DeployArrow()
        {
            arrowDeploy = true;
        }

        private void Update()
        {
            if (!arrowDeploy)
            {
                return;
            }
            float b = (blinking ? 2.5f : 1f);
            arrowScale = Mathf.Lerp(arrowScale, b, Time.deltaTime * 3f);
            useTransform.localScale = new Vector3(arrowScale, arrowScale, arrowScale);
            Quaternion b2 = Quaternion.identity;
            Vector3 vector = ReturnDirectionToPrevious();
            if (vector != Vector3.zero)
            {
                b2 = Quaternion.LookRotation(vector);
            }
            arrow.rotation = Quaternion.Slerp(arrow.rotation, b2, Time.deltaTime * 1.5f);
            if (destroySelf)
            {
                selfScale = Mathf.Lerp(selfScale, 0f, Time.deltaTime * 8f);
                base.transform.localScale = new Vector3(selfScale, selfScale, selfScale);
                if (Mathf.Approximately(selfScale, 0f))
                {
                    global::UnityEngine.Object.Destroy(base.gameObject);
                }
            }
            Color color = arrowMat.GetColor(ShaderPropertyID._Color);
            if (blinking)
            {
                Color value = Color.Lerp(color, Color.green, Time.deltaTime * 3f);
                arrowMat.SetColor(ShaderPropertyID._Color, value);
            }
            else
            {
                Color value2 = Color.Lerp(color, baseColor, Time.deltaTime * 3f);
                arrowMat.SetColor(ShaderPropertyID._Color, value2);
            }
        }

        private Vector3 ReturnDirectionToPrevious()
        {
            if (previousArrowPos == null || firstArrow)
            {
                return Vector3.zero;
            }
            return (previousArrowPos.position - arrow.position).normalized;
        }

        private void SetDestroySelf()
        {
            destroySelf = true;
        }

        public void DestroySelf(float delay)
        {
            Invoke("SetDestroySelf", delay);
        }
    }
}
