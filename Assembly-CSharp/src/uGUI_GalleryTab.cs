using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_GalleryTab : uGUI_PDATab, uGUI_IIconGridManager, IScreenshotClient, uGUI_INavigableIconGrid
    {
        public delegate void ImageSelectListener(string image);

        private class IconData
        {
            public string id;

            public string tooltip;

            public DateTime date;

            public IconData(string id, string tooltip, DateTime date)
            {
                this.id = id;
                this.tooltip = tooltip;
                this.date = date;
            }
        }

        private class IconDataComparer : IComparer<IconData>
        {
            public enum SortBy
            {
                DateDescending,
                DateAscending,
                Name
            }

            private SortBy sortBy;

            private int dir = 1;

            public void Initialize(SortBy sortBy)
            {
                this.sortBy = sortBy;
                switch (sortBy)
                {
                    case SortBy.DateDescending:
                        dir = -1;
                        break;
                    case SortBy.DateAscending:
                        dir = 1;
                        break;
                    case SortBy.Name:
                        dir = -1;
                        break;
                }
            }

            public int Compare(IconData icon1, IconData icon2)
            {
                if (sortBy == SortBy.DateAscending || sortBy == SortBy.DateDescending)
                {
                    if (icon1.date < icon2.date)
                    {
                        return -dir;
                    }
                    if (icon1.date > icon2.date)
                    {
                        return dir;
                    }
                    return 0;
                }
                if (sortBy == SortBy.Name)
                {
                    return icon1.id.CompareTo(icon2.id);
                }
                return 0;
            }
        }

        private const string galleryLabelKey = "GalleryLabel";

        [AssertNotNull]
        public GameObject content;

        [AssertNotNull]
        public Text galleryLabel;

        [AssertNotNull]
        public uGUI_IconGrid iconGrid;

        [AssertNotNull]
        public GameObject thumbnailsCanvasGO;

        [AssertNotNull]
        public GameObject fullScreenCanvasGO;

        [AssertNotNull]
        public RectTransform fullScreenCanvas;

        [AssertNotNull]
        public Button buttonFullscreen;

        [AssertNotNull]
        public Button buttonBack;

        [AssertNotNull]
        public Button buttonSelect;

        [AssertNotNull]
        public Text buttonSelectText;

        [AssertNotNull]
        public SimpleTooltip buttonSelectTooltip;

        [AssertNotNull]
        public Button buttonShare;

        [AssertNotNull]
        public Button buttonRemove;

        [AssertNotNull]
        public GameObject buttonSelectGO;

        [AssertNotNull]
        public GameObject buttonShareGO;

        [AssertNotNull]
        public GameObject buttonDeleteGO;

        [AssertNotNull]
        public Text instructions;

        [AssertNotNull]
        public GameObject instructionsGO;

        [AssertNotNull]
        public RawImage fullScreenImage;

        [AssertNotNull]
        public Sprite thumbnailBackground;

        [AssertNotNull]
        public PDANotification screenshotTakenNotification;

        [AssertNotNull]
        public PDANotification screenshotDeniedNotification;

        [AssertNotNull]
        public Text screenshotCountText;

        public Vector2 thumbnailSize = new Vector2(204f, 126f);

        public Vector2 thumbnailSpace = new Vector2(10f, 17f);

        public Vector2 thumbnailBorder = new Vector2(8f, 8f);

        public float thumbnailBackgroundRadius = 4f;

        public Color colorNormal = new Color(0.463f, 0.463f, 0.463f, 0.784f);

        public Color colorHover = new Color(0.443f, 0.443f, 0.443f, 1f);

        public Color colorPress = new Color(0.361f, 0.361f, 0.361f, 1f);

        private static IconDataComparer iconDataComparer = new IconDataComparer();

        private string fullScreenImageID;

        private Dictionary<string, IconData> icons = new Dictionary<string, IconData>();

        private IconDataComparer.SortBy sortBy;

        private bool unsorted = true;

        private ImageSelectListener selectListener;

        private SelectableWrapper selectableFullscreenWrapper;

        private List<ISelectable> navigationFullscreen;

        private readonly List<IconData> sortData = new List<IconData>();

        public override int notificationsCount
        {
            get
            {
                int num = 0;
                Dictionary<string, IconData>.Enumerator enumerator = icons.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    KeyValuePair<string, IconData> current = enumerator.Current;
                    if (NotificationManager.main.Contains(NotificationManager.Group.Gallery, current.Key))
                    {
                        num++;
                    }
                }
                return num;
            }
        }

        private bool isFullScreen => fullScreenImageID != null;

        protected override void Awake()
        {
            base.Awake();
            iconGrid.iconSize = thumbnailSize;
            iconGrid.iconBorder = thumbnailBorder;
            iconGrid.minSpaceX = thumbnailSpace.x;
            iconGrid.minSpaceY = thumbnailSpace.y;
            iconGrid.Initialize(this);
            iconGrid.SetIconBackgroundImage(new Atlas.Sprite(thumbnailBackground, slice9Grid: true));
            iconGrid.SetIconBackgroundRadius(thumbnailBackgroundRadius);
            iconGrid.SetIconBackgroundColors(colorNormal, colorHover, colorPress);
            fullScreenCanvasGO.SetActive(value: false);
            thumbnailsCanvasGO.SetActive(value: true);
            buttonSelect.onClick.AddListener(OnSelect);
            Dictionary<string, ScreenshotManager.Thumbnail>.Enumerator thumbnails = ScreenshotManager.GetThumbnails();
            while (thumbnails.MoveNext())
            {
                KeyValuePair<string, ScreenshotManager.Thumbnail> current = thumbnails.Current;
                string key = current.Key;
                ScreenshotManager.Thumbnail value = current.Value;
                OnThumbnailAdd(key, value);
            }
            ((uGUI_IIconGridManager)this).OnSortRequested();
            ScreenshotManager.onThumbnailAdd += OnThumbnailAdd;
            ScreenshotManager.onThumbnailUpdate += OnThumbnailUpdate;
            ScreenshotManager.onThumbnailRemove += OnThumbnailRemove;
            ScreenshotManager.onScreenshotTaken += OnScreenshotTaken;
            ScreenshotManager.onScreenshotDenied += OnScreenshotDenied;
            UpdateButtonsState();
            Close();
            InitNavigation();
        }

        private void Update()
        {
            instructionsGO.SetActive(iconGrid.GetCount() == 0);
        }

        private void OnDestroy()
        {
            ScreenshotManager.onThumbnailAdd -= OnThumbnailAdd;
            ScreenshotManager.onThumbnailUpdate -= OnThumbnailUpdate;
            ScreenshotManager.onThumbnailRemove -= OnThumbnailRemove;
            ScreenshotManager.onScreenshotTaken -= OnScreenshotTaken;
            ScreenshotManager.onScreenshotDenied -= OnScreenshotDenied;
        }

        public void SetSelectListener(ImageSelectListener listener, string text, string tooltip)
        {
            selectListener = listener;
            buttonSelectText.text = Language.main.Get(text);
            buttonSelectTooltip.text = tooltip;
        }

        public override void Open()
        {
            SetState(state: true);
            UpdateScreenshotText();
        }

        public override void Close()
        {
            ExitFullScreenMode();
            SetState(state: false);
        }

        public override void OnClosePDA()
        {
            base.OnClosePDA();
            selectListener = null;
            ExitFullScreenMode();
        }

        public override uGUI_INavigableIconGrid GetInitialGrid()
        {
            return iconGrid;
        }

        public override void OnLanguageChanged()
        {
            galleryLabel.text = Language.main.Get("GalleryLabel");
            instructions.text = LanguageCache.GetButtonFormat("PDAGalleryTabInstructions", GameInput.Button.TakePicture);
        }

        private void OnThumbnailAdd(string id, ScreenshotManager.Thumbnail thumbnail)
        {
            if (!icons.ContainsKey(id))
            {
                if (iconGrid.AddItem(id, new Atlas.Sprite(thumbnail.texture)))
                {
                    IconData value = new IconData(id, Path.GetFileName(id), thumbnail.lastWriteTimeUtc);
                    icons.Add(id, value);
                    unsorted = true;
                    iconGrid.RegisterNotificationTarget(id, NotificationManager.Group.Gallery, id);
                }
                UpdateScreenshotText();
            }
        }

        private void OnThumbnailRemove(string fileName)
        {
            if (icons.Remove(fileName))
            {
                iconGrid.RemoveItem(fileName);
                unsorted = true;
            }
            UpdateScreenshotText();
        }

        private void OnThumbnailUpdate(string fileName, ScreenshotManager.Thumbnail thumbnail)
        {
            if (icons.TryGetValue(fileName, out var _))
            {
                iconGrid.SetSprite(fileName, new Atlas.Sprite(thumbnail.texture));
                unsorted = true;
            }
        }

        private void OnScreenshotTaken(string fileName)
        {
            screenshotTakenNotification.Play();
        }

        private void OnScreenshotDenied()
        {
            screenshotDeniedNotification.Play();
        }

        public void OnScroll(BaseEventData eventData)
        {
            PointerEventData pointerEventData = eventData as PointerEventData;
            if (pointerEventData != null)
            {
                float y = pointerEventData.scrollDelta.y;
                if (y > 0f)
                {
                    OnPrevious();
                }
                else if (y < 0f)
                {
                    OnNext();
                }
            }
        }

        public void OnPrevious()
        {
            int index = iconGrid.GetIndex(fullScreenImageID);
            if (index < 0)
            {
                ExitFullScreenMode();
            }
            else if (index > 0)
            {
                EnterFullScreenMode(iconGrid.GetIdentifier(index - 1));
            }
        }

        public void OnNext()
        {
            int index = iconGrid.GetIndex(fullScreenImageID);
            if (index < 0)
            {
                ExitFullScreenMode();
            }
            else
            {
                EnterFullScreenMode(iconGrid.GetIdentifier(index + 1));
            }
        }

        public void OnExitFullscreen()
        {
            ExitFullScreenMode();
        }

        public void OnSelect()
        {
            string image = fullScreenImageID;
            ExitFullScreenMode();
            if (selectListener != null)
            {
                selectListener(image);
            }
        }

        public void OnShare()
        {
            if (ScreenshotManager.ShareScreenshot(fullScreenImageID))
            {
                UpdateButtonsState();
            }
        }

        public void OnDelete(BaseEventData eventData)
        {
            PointerEventData pointerEventData = eventData as PointerEventData;
            if (pointerEventData != null && pointerEventData.clickCount == 2)
            {
                OnRemove();
            }
        }

        void uGUI_IIconGridManager.GetTooltip(string id, out string tooltipText, List<TooltipIcon> tooltipIcons)
        {
            IconData value;
            if (Application.platform == RuntimePlatform.PS4)
            {
                tooltipText = "";
            }
            else if (icons.TryGetValue(id, out value))
            {
                tooltipText = TooltipFactory.Label(value.tooltip);
            }
            else
            {
                tooltipText = TooltipFactory.Label(id);
            }
        }

        void uGUI_IIconGridManager.OnPointerEnter(string id)
        {
        }

        void uGUI_IIconGridManager.OnPointerExit(string id)
        {
        }

        void uGUI_IIconGridManager.OnPointerClick(string id, int button)
        {
            if (button == 0)
            {
                EnterFullScreenMode(id);
            }
        }

        void uGUI_IIconGridManager.OnSortRequested()
        {
            if (unsorted)
            {
                iconDataComparer.Initialize(sortBy);
                sortData.Clear();
                Dictionary<string, IconData>.Enumerator enumerator = icons.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    KeyValuePair<string, IconData> current = enumerator.Current;
                    sortData.Add(current.Value);
                }
                sortData.Sort(iconDataComparer);
                int i = 0;
                for (int count = sortData.Count; i < count; i++)
                {
                    IconData iconData = sortData[i];
                    iconGrid.SetIndex(iconData.id, i);
                }
                sortData.Clear();
                unsorted = false;
            }
        }

        void IScreenshotClient.OnProgress(string fileName, float progress)
        {
        }

        void IScreenshotClient.OnDone(string fileName, Texture2D texture)
        {
            if (isFullScreen)
            {
                if (texture == null)
                {
                    ExitFullScreenMode();
                    return;
                }
                SetFullScreenImage(texture);
                UpdateButtonsState();
            }
        }

        void IScreenshotClient.OnRemoved(string fileName)
        {
            if (isFullScreen)
            {
                ExitFullScreenMode();
            }
        }

        private void SetState(bool state)
        {
            content.SetActive(state);
        }

        private void UpdateButtonsState()
        {
            buttonSelectGO.SetActive(selectListener != null);
            bool supportsSharingScreenshots = PlatformUtils.main.GetServices().GetSupportsSharingScreenshots();
            buttonShareGO.SetActive(supportsSharingScreenshots);
            buttonDeleteGO.SetActive(value: true);
        }

        private void SetFullScreenImage(Texture2D texture)
        {
            fullScreenImage.texture = texture;
            if (texture == null)
            {
                fullScreenImage.enabled = false;
                return;
            }
            fullScreenImage.enabled = true;
            Rect rect = fullScreenCanvas.rect;
            MathExtensions.RectFit(texture.width, texture.height, rect.width, rect.height, RectScaleMode.Envelope, out var scale, out var offset);
            fullScreenImage.uvRect = new Rect(offset.x, offset.y, scale.x, scale.y);
        }

        private void EnterFullScreenMode(string id)
        {
            if (id != null && !string.Equals(id, fullScreenImageID))
            {
                Texture2D thumbnail = ScreenshotManager.GetThumbnail(id);
                SetFullScreenImage(thumbnail);
                thumbnailsCanvasGO.SetActive(value: false);
                fullScreenCanvasGO.SetActive(value: true);
                ScreenshotManager.RemoveRequest(fullScreenImageID, this);
                fullScreenImageID = id;
                ScreenshotManager.AddRequest(id, this, highPriority: true);
                UpdateButtonsState();
                if (GamepadInputModule.current.GetCurrentGrid() != this)
                {
                    GamepadInputModule.current.SetCurrentGrid(this);
                }
            }
        }

        private void ExitFullScreenMode()
        {
            if (isFullScreen)
            {
                fullScreenImage.texture = null;
                ScreenshotManager.RemoveRequest(fullScreenImageID, this);
                fullScreenCanvasGO.SetActive(value: false);
                thumbnailsCanvasGO.SetActive(value: true);
                GamepadInputModule.current.SetCurrentGrid(iconGrid);
                if (GamepadInputModule.current.isControlling)
                {
                    iconGrid.SelectItem(iconGrid.GetIcon(fullScreenImageID));
                }
                fullScreenImageID = null;
            }
        }

        private void OnRemove()
        {
            string fileName = fullScreenImageID;
            if (ScreenshotManager.IsScreenshotBeingRequested(fileName))
            {
                return;
            }
            int count = iconGrid.GetCount();
            if (count > 0)
            {
                int index = iconGrid.GetIndex(fullScreenImageID);
                int num = Mathf.Clamp(index + 1, 0, count - 1);
                if (num < 0 || num == index)
                {
                    num = Mathf.Clamp(index - 1, 0, count - 1);
                }
                if (num < 0 || num == index)
                {
                    ExitFullScreenMode();
                }
                else
                {
                    EnterFullScreenMode(iconGrid.GetIdentifier(num));
                }
            }
            ScreenshotManager.Delete(fileName);
            UpdateScreenshotText();
        }

        public object GetSelectedItem()
        {
            return UISelection.selected;
        }

        public Graphic GetSelectedIcon()
        {
            if (UISelection.selected == null)
            {
                return null;
            }
            RectTransform rect = UISelection.selected.GetRect();
            if (rect == null)
            {
                return null;
            }
            return rect.GetComponent<Graphic>();
        }

        public void SelectItem(object item)
        {
            UISelection.selected = item as ISelectable;
        }

        public void DeselectItem()
        {
            UISelection.selected = null;
        }

        public bool SelectFirstItem()
        {
            UISelection.selected = selectableFullscreenWrapper;
            return true;
        }

        public bool SelectItemClosestToPosition(Vector3 worldPos)
        {
            return false;
        }

        public bool SelectItemInDirection(int dirX, int dirY)
        {
            if (UISelection.selected == null)
            {
                return SelectFirstItem();
            }
            if (dirX == 0 && dirY == 0)
            {
                return false;
            }
            if (dirY == 0 && UISelection.selected as SelectableWrapper == selectableFullscreenWrapper)
            {
                if (dirX > 0)
                {
                    OnNext();
                    return true;
                }
                OnPrevious();
                return true;
            }
            ISelectable selectable = UISelection.FindSelectable(fullScreenCanvas, new Vector2(dirX, -dirY), UISelection.selected, navigationFullscreen, fromEdge: false);
            if (selectable != null)
            {
                SelectItem(selectable);
                return true;
            }
            return false;
        }

        public uGUI_INavigableIconGrid GetNavigableGridInDirection(int dirX, int dirY)
        {
            return null;
        }

        private void InitNavigation()
        {
            selectableFullscreenWrapper = new SelectableWrapper(buttonFullscreen, delegate(GameInput.Button button)
            {
                if ((uint)(button - 27) <= 1u)
                {
                    ExitFullScreenMode();
                    return true;
                }
                return false;
            });
            SelectableWrapper item = new SelectableWrapper(buttonBack, delegate(GameInput.Button button)
            {
                if ((uint)(button - 27) <= 1u)
                {
                    ExitFullScreenMode();
                    return true;
                }
                return false;
            });
            SelectableWrapper item2 = new SelectableWrapper(buttonSelect, delegate(GameInput.Button button)
            {
                switch (button)
                {
                    case GameInput.Button.UISubmit:
                        OnSelect();
                        return true;
                    case GameInput.Button.UICancel:
                        ExitFullScreenMode();
                        return true;
                    default:
                        return false;
                }
            });
            SelectableWrapper item3 = new SelectableWrapper(buttonShare, delegate(GameInput.Button button)
            {
                switch (button)
                {
                    case GameInput.Button.UISubmit:
                        OnShare();
                        return true;
                    case GameInput.Button.UICancel:
                        ExitFullScreenMode();
                        return true;
                    default:
                        return false;
                }
            });
            SelectableWrapper item4 = new SelectableWrapper(buttonRemove, delegate(GameInput.Button button)
            {
                switch (button)
                {
                    case GameInput.Button.UISubmit:
                        OnRemove();
                        return true;
                    case GameInput.Button.UICancel:
                        ExitFullScreenMode();
                        return true;
                    default:
                        return false;
                }
            });
            navigationFullscreen = new List<ISelectable> { selectableFullscreenWrapper, item, item2, item3, item4 };
        }

        public override bool OnButtonDown(GameInput.Button button)
        {
            if (selectListener == new ImageSelectListener((pda.GetTab(PDATab.TimeCapsule) as uGUI_TimeCapsuleTab).SelectImage))
            {
                if (button == GameInput.Button.UICancel)
                {
                    pda.OpenTab(PDATab.TimeCapsule);
                    return true;
                }
                return false;
            }
            return base.OnButtonDown(button);
        }

        private void UpdateScreenshotText()
        {
            if (screenshotCountText != null)
            {
                if (ScreenshotManager.IsLimitingScreenhots())
                {
                    screenshotCountText.text = ScreenshotManager.GetNumScreenshots() + " / " + ScreenshotManager.GetMaxNumScreenshots();
                }
                else
                {
                    screenshotCountText.gameObject.SetActive(value: false);
                }
            }
        }
    }
}
