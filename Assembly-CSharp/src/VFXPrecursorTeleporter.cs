using UnityEngine;

namespace AssemblyCSharp
{
    public class VFXPrecursorTeleporter : MonoBehaviour
    {
        [AssertNotNull]
        public Renderer portalRenderer;

        [AssertNotNull]
        public Light portalLight;

        public VFXController fxControl;

        public float fadeInDuration = 1.5f;

        public float fadeOutDuration = 0.5f;

        private float fadefactor = 1f;

        private int fadeState;

        private int radialFadeID;

        private void Start()
        {
            radialFadeID = Shader.PropertyToID("_RadialFade");
        }

        public void Update()
        {
            float num = ((fadeState == 1) ? fadeInDuration : fadeOutDuration);
            fadefactor += (float)fadeState * Time.deltaTime / num;
            if (fadefactor < 0f)
            {
                fadeState = 0;
                portalLight.gameObject.SetActive(value: false);
                portalRenderer.gameObject.SetActive(value: false);
            }
            else if (fadefactor > 1f)
            {
                fadeState = 0;
            }
            fadefactor = Mathf.Clamp01(fadefactor);
            portalLight.intensity *= fadefactor;
            portalRenderer.material.SetFloat(radialFadeID, fadefactor);
        }

        public void FadeIn()
        {
            if (fxControl != null)
            {
                fxControl.Play(0);
            }
            portalLight.gameObject.SetActive(value: true);
            portalRenderer.gameObject.SetActive(value: true);
            fadeState = 1;
        }

        public void FadeOut()
        {
            fadeState = -1;
        }

        public void Toggle(bool open)
        {
            fadeState = 0;
            fadefactor = (open ? 1f : 0f);
            portalLight.gameObject.SetActive(open);
            portalRenderer.gameObject.SetActive(open);
            portalLight.intensity *= fadefactor;
            portalRenderer.material.SetFloat(radialFadeID, fadefactor);
        }

        private void OnDestroy()
        {
            Object.Destroy(portalRenderer.material);
        }
    }
}
