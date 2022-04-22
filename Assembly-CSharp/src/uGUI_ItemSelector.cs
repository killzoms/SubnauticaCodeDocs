using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_ItemSelector : MonoBehaviour, IInputHandler
    {
        private const float spaceBetweenIcons = 0.5f;

        private const float timeout = 5f;

        private const float fadeTime = 1f;

        private static readonly GameInput.Button[] buttonsNext = new GameInput.Button[3]
        {
            GameInput.Button.CycleNext,
            GameInput.Button.MoveForward,
            GameInput.Button.MoveRight
        };

        private static readonly GameInput.Button[] buttonsPrevious = new GameInput.Button[3]
        {
            GameInput.Button.CyclePrev,
            GameInput.Button.MoveBackward,
            GameInput.Button.MoveLeft
        };

        private static readonly GameInput.Button[] buttonsSelect = new GameInput.Button[1] { GameInput.Button.LeftHand };

        private static readonly GameInput.Button[] buttonsCancel = new GameInput.Button[3]
        {
            GameInput.Button.RightHand,
            GameInput.Button.Exit,
            GameInput.Button.UICancel
        };

        public Text text;

        public RectTransform canvas;

        public CanvasGroup canvasGroup;

        public AnimationCurve alphaCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

        private List<IItemsContainer> srcContainers;

        private IItemSelectorManager manager;

        private Atlas.Sprite defaultSprite;

        private List<uGUI_ItemIcon> icons = new List<uGUI_ItemIcon>();

        private int used = -1;

        private int selected = -1;

        private List<InventoryItem> items = new List<InventoryItem>();

        private Sequence sequence = new Sequence();

        private StringBuilder stringBuilder = new StringBuilder();

        private static readonly List<Graphic> sGraphic = new List<Graphic>();

        private void Awake()
        {
            text.text = "";
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            SetState(active: false, immediately: true);
        }

        private void Update()
        {
            Vector2 size = canvas.rect.size;
            int count = icons.Count;
            for (int i = 0; i < count; i++)
            {
                uGUI_ItemIcon obj = icons[i];
                RectTransform rectTransform = obj.rectTransform;
                GetIconPositionScale(i, out var x, out var s);
                float t = 7f * Time.deltaTime;
                x = Mathf.Lerp(rectTransform.anchoredPosition.x, x, t);
                s = Mathf.Lerp(rectTransform.localScale.x, s, t);
                obj.SetPosition(x, 0f);
                obj.SetScale(s, s);
                float num = alphaCurve.Evaluate(x / (0.5f * size.x));
                obj.SetAlpha(num, num, num);
            }
            if (sequence.active)
            {
                sequence.Update(Time.deltaTime);
                float time = sequence.time;
                float num2 = sequence.t * time;
                float alpha = Mathf.Clamp01((time - num2) / 1f);
                canvasGroup.alpha = alpha;
                if (!sequence.active)
                {
                    Reset();
                }
            }
            if (manager != null)
            {
                stringBuilder.Length = 0;
                stringBuilder.AppendFormat("{0} ", Language.main.Get("ItemSelectorPrevious"));
                FillBindingNames(buttonsPrevious);
                stringBuilder.AppendFormat(", {0} ", Language.main.Get("ItemSelectorNext"));
                FillBindingNames(buttonsNext);
                stringBuilder.AppendFormat("\n{0} ", Language.main.Get("ItelSelectorSelect"));
                FillBindingNames(buttonsSelect);
                stringBuilder.AppendFormat(", {0} ", Language.main.Get("ItemSelectorCancel"));
                FillBindingNames(buttonsCancel);
                HandReticle.main.SetUseTextRaw("", stringBuilder.ToString());
            }
        }

        public void Initialize(IItemSelectorManager manager, Atlas.Sprite defaultSprite, List<IItemsContainer> containers)
        {
            Reset();
            if (manager == null || containers == null || containers.Count == 0)
            {
                return;
            }
            this.manager = manager;
            this.defaultSprite = defaultSprite;
            srcContainers = containers;
            for (int i = 0; i < srcContainers.Count; i++)
            {
                IItemsContainer itemsContainer = srcContainers[i];
                foreach (InventoryItem item in itemsContainer)
                {
                    if (manager.Filter(item))
                    {
                        items.Add(item);
                    }
                }
                itemsContainer.onAddItem += OnAddItem;
                itemsContainer.onRemoveItem += OnRemoveItem;
            }
            used = 1 + manager.Sort(items);
            selected = used;
            CreateIcons();
            UpdateInfoText();
            SetState(active: true, immediately: true);
            InputHandlerStack.main.Push(this);
            FPSInputModule.current.lockMovement = true;
            sequence.Set(5f, current: false, target: true);
        }

        public static bool HasCompatibleItems(IItemSelectorManager manager, List<IItemsContainer> containers)
        {
            for (int i = 0; i < containers.Count; i++)
            {
                foreach (InventoryItem item in containers[i])
                {
                    if (manager.Filter(item))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void Reset()
        {
            sequence.Reset();
            ClearIcons();
            items.Clear();
            selected = -1;
            if (srcContainers != null)
            {
                for (int i = 0; i < srcContainers.Count; i++)
                {
                    IItemsContainer itemsContainer = srcContainers[i];
                    itemsContainer.onAddItem -= OnAddItem;
                    itemsContainer.onRemoveItem -= OnRemoveItem;
                }
                srcContainers = null;
            }
            manager = null;
            defaultSprite = null;
            FPSInputModule.current.lockMovement = false;
        }

        private void ClearIcons()
        {
            int i = 0;
            for (int count = icons.Count; i < count; i++)
            {
                Object.Destroy(icons[i].gameObject);
            }
            icons.Clear();
        }

        private void OnAddItem(InventoryItem item)
        {
            if (manager.Filter(item))
            {
                ClearIcons();
                items.Add(item);
                used = 1 + manager.Sort(items);
                selected = used;
                CreateIcons();
            }
        }

        private void OnRemoveItem(InventoryItem item)
        {
            int num = items.IndexOf(item);
            if (num != -1)
            {
                ClearIcons();
                items.RemoveAt(num);
                used = 1 + manager.Sort(items);
                selected = used;
                CreateIcons();
            }
        }

        private void CreateIcons()
        {
            uGUI_ItemIcon item = CreateIcon((defaultSprite != null) ? defaultSprite : SpriteManager.defaultSprite, SpriteManager.GetBackground(CraftData.BackgroundType.Normal));
            icons.Add(item);
            int i = 0;
            for (int count = items.Count; i < count; i++)
            {
                TechType techType = items[i].item.GetTechType();
                item = CreateIcon(SpriteManager.Get(techType), SpriteManager.GetBackground(techType));
                icons.Add(item);
            }
            int j = 0;
            for (int count2 = icons.Count; j < count2; j++)
            {
                item = icons[j];
                GetIconPositionScale(j, out var x, out var s);
                item.SetPosition(x, 0f);
                item.SetScale(s, s);
            }
            if (used >= 0)
            {
                icons[used].SetForegroundColors(Color.green, Color.green, Color.green);
            }
        }

        private void GetIconPositionScale(int i, out float x, out float s)
        {
            float height = canvas.rect.height;
            float num = 1.5f * height;
            x = (float)(i - selected) * num;
            s = Mathf.Lerp(1f, 0.3f, Mathf.Abs(x / (3f * num)));
        }

        private uGUI_ItemIcon CreateIcon(Atlas.Sprite foreground, Atlas.Sprite background)
        {
            uGUI_ItemIcon obj = new GameObject("ItemIcon").AddComponent<uGUI_ItemIcon>();
            obj.Init(null, canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            obj.SetForegroundSprite(foreground);
            obj.SetBackgroundSprite(background);
            float height = canvas.rect.height;
            obj.SetSize(height, height);
            obj.SetBackgroundRadius(height * 0.5f);
            return obj;
        }

        private InventoryItem GetSelectedItem()
        {
            if (selected >= 1)
            {
                return items[selected - 1];
            }
            return null;
        }

        private void UpdateInfoText()
        {
            text.text = manager.GetText(GetSelectedItem());
        }

        private void SetState(bool active, bool immediately)
        {
            if (active)
            {
                if (immediately)
                {
                    canvasGroup.alpha = 1f;
                }
            }
            else if (immediately)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private bool GetButtonDown(GameInput.Button[] buttons)
        {
            int i = 0;
            for (int num = buttons.Length; i < num; i++)
            {
                if (GameInput.GetButtonDown(buttons[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private void FillBindingNames(GameInput.Button[] buttons)
        {
            int i = 0;
            for (int num = buttons.Length; i < num; i++)
            {
                string arg = uGUI.FormatButton(buttons[i]);
                stringBuilder.AppendFormat((i == 0) ? "({0})" : " ({0})", arg);
            }
        }

        public bool HandleInput()
        {
            if (manager == null || GameInput.GetButtonDown(GameInput.Button.Reload))
            {
                Reset();
                SetState(active: false, immediately: true);
                return false;
            }
            if (GetButtonDown(buttonsNext))
            {
                selected++;
                if (selected >= icons.Count)
                {
                    selected = 0;
                }
                UpdateInfoText();
                sequence.Set(5f, current: false, target: true);
            }
            else if (GetButtonDown(buttonsPrevious))
            {
                selected--;
                if (selected < 0)
                {
                    selected = icons.Count - 1;
                }
                UpdateInfoText();
                sequence.Set(5f, current: false, target: true);
            }
            else
            {
                if (GetButtonDown(buttonsSelect))
                {
                    manager.Select(GetSelectedItem());
                    Reset();
                    return false;
                }
                if (GetButtonDown(buttonsCancel))
                {
                    Reset();
                    return false;
                }
            }
            return true;
        }

        public bool HandleLateInput()
        {
            return true;
        }

        public void OnFocusChanged(InputFocusMode mode)
        {
            switch (mode)
            {
                case InputFocusMode.Remove:
                    Reset();
                    SetState(active: false, immediately: true);
                    break;
                case InputFocusMode.Add:
                case InputFocusMode.Suspend:
                case InputFocusMode.Restore:
                    break;
            }
        }
    }
}
