using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(RectTransform))]
    public class uGUI_NotificationLabel : MonoBehaviour, ILayoutIgnorer
    {
        private const string prefabPath = "NotificationLabel";

        private RectTransform _rectTransform;

        [AssertNotNull]
        public Image background;

        [AssertNotNull]
        public Text text;

        public RectTransform rectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }
                return _rectTransform;
            }
        }

        public bool ignoreLayout => true;

        public static uGUI_NotificationLabel CreateInstance(RectTransform parent)
        {
            uGUI_NotificationLabel result = null;
            GameObject gameObject = Resources.Load<GameObject>("NotificationLabel");
            if (gameObject != null)
            {
                result = Object.Instantiate(gameObject).GetComponent<uGUI_NotificationLabel>();
                result.rectTransform.SetParent(parent, worldPositionStays: false);
                return result;
            }
            Debug.LogError("Error: Prefab at path 'NotificationLabel' is not found in Resources folder!");
            return result;
        }

        public void SetAnchor(UIAnchor anchor)
        {
            Vector2 vector2 = (rectTransform.anchorMin = (rectTransform.anchorMax = RectTransformExtensions.GetAnchorPoint(anchor)));
        }

        public void SetOffset(Vector2 offset)
        {
            rectTransform.anchoredPosition = offset;
        }

        public void SetText(string text)
        {
            this.text.text = text;
        }

        public void SetBackgroundColor(Color color)
        {
            background.color = color;
        }

        public void SetAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            Color color = background.color;
            color.a = alpha;
            background.color = color;
            color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }
}
