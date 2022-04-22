using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(RectTransform))]
    public class uGUI_BlueprintEntry : MonoBehaviour, ILayoutElement, ICompileTimeCheckable, INotificationTarget, ISelectable
    {
        [AssertNotNull]
        public uGUI_ItemIcon icon;

        [AssertNotNull]
        public Text title;

        [AssertNotNull]
        public GameObject progressPlaceholder;

        [AssertNotNull]
        public GameObject prefabProgress;

        public Vector2 iconSize = new Vector2(106f, 106f);

        [SerializeField]
        private float _minWidth = -1f;

        private RectTransform _rectTransform;

        private uGUI_BlueprintProgress _progress;

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

        public float minWidth => _minWidth;

        public float minHeight => -1f;

        public float preferredWidth => -1f;

        public float preferredHeight => -1f;

        public float flexibleWidth => -1f;

        public float flexibleHeight => -1f;

        public int layoutPriority => 1;

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        private void Awake()
        {
            icon.SetSize(iconSize);
            progressPlaceholder.SetActive(value: false);
        }

        private void OnEnable()
        {
            SetDirty();
        }

        private void OnDisable()
        {
            SetDirty();
        }

        private void OnTransformParentChanged()
        {
            SetDirty();
        }

        private void OnDidApplyAnimationProperties()
        {
            SetDirty();
        }

        private void OnBeforeTransformParentChanged()
        {
            SetDirty();
        }

        public void Initialize(uGUI_IIconManager manager)
        {
            icon.manager = manager;
        }

        public void SetIcon(TechType techType)
        {
            Atlas.Sprite foregroundSprite = SpriteManager.Get(techType);
            Atlas.Sprite background = SpriteManager.GetBackground(techType);
            icon.SetForegroundSprite(foregroundSprite);
            icon.SetBackgroundSprite(background);
            float num = Mathf.Min(iconSize.x, iconSize.y);
            icon.SetBackgroundRadius(num * 0.5f);
        }

        public void SetText(string text)
        {
            title.text = text;
        }

        public void SetValue(int unlocked, int total)
        {
            if (_progress == null)
            {
                if (total > 0)
                {
                    _progress = Object.Instantiate(prefabProgress).GetComponent<uGUI_BlueprintProgress>();
                    RectTransform component = _progress.GetComponent<RectTransform>();
                    component.SetParent(rectTransform, worldPositionStays: false);
                    component.SetAsLastSibling();
                    progressPlaceholder.SetActive(value: true);
                }
            }
            else
            {
                if (total > 0)
                {
                    _progress.gameObject.SetActive(value: true);
                }
                if (total <= 0)
                {
                    _progress.gameObject.SetActive(value: false);
                    progressPlaceholder.SetActive(value: false);
                }
            }
            if (_progress != null)
            {
                _progress.SetValue(unlocked, total);
            }
            icon.SetChroma((unlocked < total) ? 0f : 1f);
        }

        private void SetDirty()
        {
            if (base.isActiveAndEnabled)
            {
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            }
        }

        bool ISelectable.IsValid()
        {
            if (this != null)
            {
                return base.isActiveAndEnabled;
            }
            return false;
        }

        RectTransform ISelectable.GetRect()
        {
            return rectTransform;
        }

        bool ISelectable.OnButtonDown(GameInput.Button button)
        {
            return false;
        }

        bool INotificationTarget.IsVisible()
        {
            return ((INotificationTarget)icon).IsVisible();
        }

        bool INotificationTarget.IsDestroyed()
        {
            return this == null;
        }

        void INotificationTarget.Progress(float progress)
        {
            if (icon.SetNotificationProgress(progress))
            {
                icon.SetNotificationBackgroundColor(NotificationManager.notificationColor);
                icon.SetNotificationOffset(iconSize * -0.1464466f);
            }
        }

        public string CompileTimeCheck()
        {
            if (prefabProgress.GetComponent<uGUI_BlueprintProgress>() == null)
            {
                return $"uGUI_BlueprintProgress component is expected on {prefabProgress.name} prefab assigned to prefabProgress field\n";
            }
            return null;
        }
    }
}
