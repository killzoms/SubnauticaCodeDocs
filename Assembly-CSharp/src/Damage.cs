using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(GUITexture))]
    public class Damage : MonoBehaviour
    {
        public void Init(float inHoldTime, float fadeTime, Texture texture, Vector3 inDamageSource)
        {
            GUITexture component = base.gameObject.GetComponent<GUITexture>();
            component.texture = texture;
            component.pixelInset = new Rect(-component.texture.width / 2, -component.texture.height / 2, component.texture.width, component.texture.height);
            Vector2 vector = MainCamera.camera.WorldToScreenPoint(inDamageSource);
            float x = Mathf.Clamp(vector.x / (float)Screen.width, 0.2f, 0.8f);
            float y = Mathf.Clamp(vector.y / (float)Screen.height, 0.2f, 0.8f);
            component.transform.position = new Vector3(x, y, component.transform.position.z);
            iTween.FadeTo(base.gameObject, iTween.Hash("alpha", 0, "delay", inHoldTime, "time", fadeTime, "oncomplete", "FadeOut", "oncompletetarget", base.gameObject));
        }

        public void FadeOut()
        {
            Object.Destroy(base.gameObject);
        }
    }
}
