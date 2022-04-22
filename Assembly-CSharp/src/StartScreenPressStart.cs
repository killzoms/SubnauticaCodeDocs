using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class StartScreenPressStart : MonoBehaviour
    {
        public Text text;

        private bool loading;

        private Vector2 initPos;

        private RectTransform textRT;

        private float animT;

        public bool IsLoading => loading;

        private void Start()
        {
            textRT = text.GetComponent<RectTransform>();
            initPos = textRT.anchoredPosition;
            SetLoading(_loading: false);
        }

        private void Update()
        {
            animT += Time.deltaTime;
            if (loading)
            {
                textRT.anchoredPosition = initPos;
                textRT.localScale = Vector3.one;
                float a = (1f + Mathf.Sin(animT * 10f)) * 0.5f;
                text.color = new Color(text.color.r, text.color.g, text.color.b, a);
            }
            else
            {
                float num = 0.95f + Mathf.Max(0f, (Mathf.Sin(animT * 5f) + 0.1f) * 0.1f);
                textRT.localScale = new Vector3(num, num, 1f);
            }
        }

        public void SetLoading(bool _loading)
        {
            loading = _loading;
            animT = 0f;
            if (loading)
            {
                text.text = Language.main.Get("StartScreenLoading").ToUpper();
            }
            else
            {
                text.text = Language.main.Get("PressStart").ToUpper();
            }
        }
    }
}
