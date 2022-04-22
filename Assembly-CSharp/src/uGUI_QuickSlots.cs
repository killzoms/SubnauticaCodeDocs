using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(RectTransform))]
    public class uGUI_QuickSlots : MonoBehaviour, uGUI_IIconManager
    {
        private static readonly Vector2 iconStep = new Vector2(58f, 58f);

        private static readonly Vector2 foregroundSize = new Vector2(58f, 58f);

        private static readonly Vector2 backgroundSize = new Vector2(60f, 60f);

        private static readonly Vector2 activeSize = new Vector2(66f, 66f);

        private const float iconSpace = 8f;

        [AssertNotNull]
        public Material materialBackground;

        [AssertNotNull]
        public Sprite spriteLeft;

        [AssertNotNull]
        public Sprite spriteCenter;

        [AssertNotNull]
        public Sprite spriteRight;

        [AssertNotNull]
        public Sprite spriteNormal;

        [AssertNotNull]
        public Sprite spriteHighlighted;

        [AssertNotNull]
        public Sprite spriteExosuitArm;

        [AssertNotNull]
        public Sprite spriteSelected;

        private RectTransform _rectTransform;

        private IQuickSlots overrideTarget;

        private IQuickSlots target;

        private uGUI_ItemIcon[] icons;

        private Image[] backgrounds;

        private bool unbindOnEndDrag;

        private Atlas.Sprite _atlasSpriteNormal;

        private Atlas.Sprite _atlasSpriteHighlighted;

        private Atlas.Sprite _atlasSpriteExosuitArm;

        private Image _selector;

        private RectTransform rectTransform
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

        private Atlas.Sprite atlasSpriteNormal
        {
            get
            {
                if (_atlasSpriteNormal == null)
                {
                    _atlasSpriteNormal = new Atlas.Sprite(spriteNormal);
                }
                return _atlasSpriteNormal;
            }
        }

        private Atlas.Sprite atlasSpriteHighlighted
        {
            get
            {
                if (_atlasSpriteHighlighted == null)
                {
                    _atlasSpriteHighlighted = new Atlas.Sprite(spriteHighlighted);
                }
                return _atlasSpriteHighlighted;
            }
        }

        private Atlas.Sprite atlasSpriteExosuitArm
        {
            get
            {
                if (_atlasSpriteExosuitArm == null)
                {
                    _atlasSpriteExosuitArm = new Atlas.Sprite(spriteExosuitArm);
                }
                return _atlasSpriteExosuitArm;
            }
        }

        private Image selector
        {
            get
            {
                if (_selector == null)
                {
                    GameObject gameObject = new GameObject("QuickSlot Selector");
                    gameObject.layer = base.gameObject.layer;
                    _selector = gameObject.AddComponent<Image>();
                    _selector.sprite = spriteSelected;
                    RectTransform obj = _selector.rectTransform;
                    RectTransformExtensions.SetParams(obj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rectTransform);
                    obj.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, spriteSelected.rect.width);
                    obj.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, spriteSelected.rect.height);
                    obj.SetAsLastSibling();
                    _selector.enabled = false;
                }
                return _selector;
            }
        }

        private void Update()
        {
            IQuickSlots quickSlots = GetTarget();
            if (target != quickSlots)
            {
                target = quickSlots;
                Init(target);
            }
            if (target == null)
            {
                return;
            }
            HandleInput();
            int i = 0;
            for (int num = icons.Length; i < num; i++)
            {
                uGUI_ItemIcon uGUI_ItemIcon2 = icons[i];
                float slotProgress = target.GetSlotProgress(i);
                float slotCharge = target.GetSlotCharge(i);
                if (slotProgress < 1f)
                {
                    uGUI_ItemIcon2.SetProgress(slotProgress);
                }
                else if (slotCharge > 0f)
                {
                    uGUI_ItemIcon2.SetProgress(slotCharge, FillMethod.Vertical);
                }
                else
                {
                    uGUI_ItemIcon2.SetProgress(1f, FillMethod.None);
                }
            }
        }

        public void SetTarget(IQuickSlots newTarget)
        {
            overrideTarget = newTarget;
        }

        private void Init(IQuickSlots newTarget)
        {
            Uninit();
            if (newTarget != null)
            {
                target = newTarget;
                TechType[] slotBinding = target.GetSlotBinding();
                int num = slotBinding.Length;
                backgrounds = new Image[num];
                icons = new uGUI_ItemIcon[num];
                for (int i = 0; i < num; i++)
                {
                    TechType techType = slotBinding[i];
                    Vector2 position = GetPosition(i);
                    Image image = new GameObject("QuickSlot Background")
                    {
                        layer = base.gameObject.layer
                    }.AddComponent<Image>();
                    Sprite sprite = null;
                    sprite = ((num != 1) ? ((i != 0) ? ((i != num - 1) ? spriteCenter : spriteRight) : spriteLeft) : spriteCenter);
                    image.material = materialBackground;
                    image.sprite = sprite;
                    RectTransform obj = image.rectTransform;
                    RectTransformExtensions.SetParams(obj, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rectTransform);
                    obj.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sprite.rect.width);
                    obj.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sprite.rect.height);
                    obj.anchoredPosition = position;
                    backgrounds[i] = image;
                    uGUI_ItemIcon uGUI_ItemIcon2 = new GameObject("QuickSlot Icon")
                    {
                        layer = base.gameObject.layer
                    }.AddComponent<uGUI_ItemIcon>();
                    uGUI_ItemIcon2.Init(this, rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                    SetForeground(uGUI_ItemIcon2, techType);
                    SetBackground(uGUI_ItemIcon2, techType, target.IsToggled(i));
                    uGUI_ItemIcon2.SetBackgroundBlending(Blending.Additive);
                    uGUI_ItemIcon2.SetActiveSize(activeSize.x, activeSize.y);
                    uGUI_ItemIcon2.SetForegroundSize(foregroundSize);
                    uGUI_ItemIcon2.SetBackgroundSize(-1f, -1f);
                    uGUI_ItemIcon2.SetPosition(position.x, position.y);
                    uGUI_ItemIcon2.SetAsLastSibling();
                    icons[i] = uGUI_ItemIcon2;
                }
                selector.rectTransform.SetAsLastSibling();
                OnSelect(target.GetActiveSlotID());
                target.onBind += OnBind;
                target.onToggle += OnToggle;
                target.onSelect += OnSelect;
            }
        }

        private void Uninit()
        {
            if (target != null)
            {
                target.onBind -= OnBind;
                target.onToggle -= OnToggle;
                target.onSelect -= OnSelect;
                target = null;
            }
            if (backgrounds != null)
            {
                for (int num = backgrounds.Length - 1; num >= 0; num--)
                {
                    Image image = backgrounds[num];
                    if (!(image == null))
                    {
                        Object.Destroy(image.gameObject);
                    }
                }
                backgrounds = null;
            }
            if (icons == null)
            {
                return;
            }
            for (int num2 = icons.Length - 1; num2 >= 0; num2--)
            {
                uGUI_ItemIcon uGUI_ItemIcon2 = icons[num2];
                if (!(uGUI_ItemIcon2 == null))
                {
                    Object.Destroy(uGUI_ItemIcon2.gameObject);
                }
            }
            icons = null;
        }

        private void HandleInput()
        {
            Player main = Player.main;
            if (!main.GetCanItemBeUsed())
            {
                return;
            }
            bool isIntroActive = IntroVignette.isIntroActive;
            if (!isIntroActive)
            {
                int i = 0;
                for (int quickSlotButtonsCount = Player.quickSlotButtonsCount; i < quickSlotButtonsCount; i++)
                {
                    if (main.GetQuickSlotKeyDown(i))
                    {
                        target.SlotKeyDown(i);
                    }
                    else if (main.GetQuickSlotKeyHeld(i))
                    {
                        target.SlotKeyHeld(i);
                    }
                    if (main.GetQuickSlotKeyUp(i))
                    {
                        target.SlotKeyUp(i);
                    }
                }
                if (GameInput.GetButtonDown(GameInput.Button.CycleNext))
                {
                    target.SlotNext();
                }
                else if (GameInput.GetButtonDown(GameInput.Button.CyclePrev))
                {
                    target.SlotPrevious();
                }
            }
            if (AvatarInputHandler.main != null && AvatarInputHandler.main.IsEnabled())
            {
                if (main.GetLeftHandDown())
                {
                    target.SlotLeftDown();
                }
                else if (main.GetLeftHandHeld())
                {
                    target.SlotLeftHeld();
                }
                if (main.GetLeftHandUp())
                {
                    target.SlotLeftUp();
                }
                if (main.GetRightHandDown())
                {
                    target.SlotRightDown();
                }
                else if (main.GetRightHandHeld())
                {
                    target.SlotRightHeld();
                }
                if (main.GetRightHandUp())
                {
                    target.SlotRightUp();
                }
                if (!isIntroActive && GameInput.GetButtonDown(GameInput.Button.Exit))
                {
                    target.DeselectSlots();
                }
            }
        }

        private void OnBind(int slotID, TechType techType, bool state)
        {
            if (target != null)
            {
                uGUI_ItemIcon icon = GetIcon(slotID);
                if (state)
                {
                    SetForeground(icon, techType);
                    SetBackground(icon, techType, target.IsToggled(slotID));
                }
                else
                {
                    SetForeground(icon, TechType.None);
                    SetBackground(icon, TechType.None, highlighted: false);
                }
            }
        }

        private void OnToggle(int slotID, bool state)
        {
            if (target != null)
            {
                SetBackground(GetIcon(slotID), target.GetSlotBinding(slotID), state);
            }
        }

        private void OnSelect(int slotID)
        {
            if (target != null)
            {
                if (slotID < 0)
                {
                    selector.enabled = false;
                    return;
                }
                selector.rectTransform.anchoredPosition = GetPosition(slotID);
                selector.enabled = true;
            }
        }

        private void SetForeground(uGUI_ItemIcon icon, TechType techType)
        {
            if (!(icon == null))
            {
                bool flag = techType != TechType.None;
                icon.SetForegroundSprite(flag ? SpriteManager.Get(techType) : null);
            }
        }

        private void SetBackground(uGUI_ItemIcon icon, TechType techType, bool highlighted)
        {
            if (!(icon == null))
            {
                Atlas.Sprite backgroundSprite = (highlighted ? atlasSpriteHighlighted : atlasSpriteNormal);
                if (techType != 0 && CraftData.GetEquipmentType(techType) == EquipmentType.ExosuitArm)
                {
                    backgroundSprite = atlasSpriteExosuitArm;
                }
                icon.SetBackgroundSprite(backgroundSprite);
            }
        }

        private uGUI_ItemIcon GetIcon(int slotID)
        {
            if (icons == null || slotID < 0 || slotID >= icons.Length)
            {
                return null;
            }
            return icons[slotID];
        }

        private int GetSlot(uGUI_ItemIcon icon)
        {
            if (icons != null)
            {
                for (int i = 0; i < icons.Length; i++)
                {
                    if (icons[i] == icon)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public void GetTooltip(uGUI_ItemIcon icon, out string tooltipText, List<TooltipIcon> tooltipIcons)
        {
            if (target != null)
            {
                int slot = GetSlot(icon);
                TechType slotBinding = target.GetSlotBinding(slot);
                if (slotBinding != 0)
                {
                    InventoryItem slotItem = target.GetSlotItem(slot);
                    if (slotItem != null && slotItem.item != null)
                    {
                        TooltipFactory.QuickSlot(slotBinding, slotItem.item.gameObject, out tooltipText);
                        return;
                    }
                }
            }
            tooltipText = null;
        }

        public void OnPointerEnter(uGUI_ItemIcon icon)
        {
        }

        public void OnPointerExit(uGUI_ItemIcon icon)
        {
        }

        public bool OnPointerClick(uGUI_ItemIcon icon, int button)
        {
            unbindOnEndDrag = false;
            return true;
        }

        public bool OnBeginDrag(uGUI_ItemIcon icon)
        {
            if (target == null)
            {
                return true;
            }
            int slot = GetSlot(icon);
            InventoryItem slotItem = target.GetSlotItem(slot);
            int instanceID = icon.GetInstanceID();
            RectTransform rectTransform = icon.rectTransform;
            if (ItemDragManager.DragStart(backgroundRadius: Mathf.Min(backgroundSize.x, backgroundSize.y) * 0.5f, item: slotItem, icon: icon, instanceId: instanceID, worldPosition: rectTransform.position, worldRotation: rectTransform.rotation, worldScale: rectTransform.lossyScale, foregroundSize: foregroundSize, backgroundSize: backgroundSize))
            {
                unbindOnEndDrag = true;
            }
            return true;
        }

        public void OnEndDrag(uGUI_ItemIcon icon)
        {
            ItemDragManager.DragStop();
            if (unbindOnEndDrag)
            {
                unbindOnEndDrag = false;
                if (target != null)
                {
                    int slot = GetSlot(icon);
                    target.Unbind(slot);
                }
            }
        }

        public void OnDrop(uGUI_ItemIcon icon)
        {
            if (target == null || !ItemDragManager.isDragging)
            {
                return;
            }
            unbindOnEndDrag = false;
            InventoryItem draggedItem = ItemDragManager.draggedItem;
            ItemDragManager.DragStop();
            if (draggedItem == null || !Inventory.main.GetCanBindItem(draggedItem))
            {
                return;
            }
            int slot = GetSlot(icon);
            InventoryItem slotItem = target.GetSlotItem(slot);
            if (draggedItem == slotItem)
            {
                return;
            }
            if (slotItem == null)
            {
                target.Bind(slot, draggedItem);
                return;
            }
            int slotByItem = target.GetSlotByItem(draggedItem);
            if (slotByItem == -1)
            {
                target.Bind(slot, draggedItem);
            }
            else if (slotByItem != slot)
            {
                target.Bind(slot, draggedItem);
                target.Bind(slotByItem, slotItem);
            }
        }

        public void OnDragHoverEnter(uGUI_ItemIcon icon)
        {
        }

        public void OnDragHoverStay(uGUI_ItemIcon icon)
        {
            if (target == null)
            {
                return;
            }
            bool flag = false;
            if (ItemDragManager.isDragging)
            {
                InventoryItem draggedItem = ItemDragManager.draggedItem;
                if (draggedItem != null && Inventory.main.GetCanBindItem(draggedItem) && Inventory.main.quickSlots.GetSlotByItem(draggedItem) != GetSlot(icon))
                {
                    flag = true;
                }
            }
            if (flag)
            {
                RectTransform rectTransform = icon.rectTransform;
                float backgroundRadius = Mathf.Min(backgroundSize.x, backgroundSize.y) * 0.5f;
                ItemDragManager.DragSnap(icon.GetInstanceID(), rectTransform.position, rectTransform.rotation, rectTransform.lossyScale, foregroundSize, backgroundSize, backgroundRadius);
            }
        }

        public void OnDragHoverExit(uGUI_ItemIcon icon)
        {
        }

        private Vector2 GetPosition(int slotID)
        {
            if (icons == null)
            {
                return new Vector2(0f, 0f);
            }
            float num = iconStep.x + 8f;
            return new Vector2(-0.5f * (float)(icons.Length - 1) * num + (float)slotID * num, 0f);
        }

        private IQuickSlots GetTarget()
        {
            if (!uGUI.isMainLevel)
            {
                return null;
            }
            if (uGUI.isIntro)
            {
                return null;
            }
            if (LaunchRocket.isLaunching)
            {
                return null;
            }
            Player main = Player.main;
            if (main == null)
            {
                return null;
            }
            if (main.GetMode() == Player.Mode.Piloting)
            {
                return null;
            }
            uGUI_CameraDrone main2 = uGUI_CameraDrone.main;
            if (main2 != null && main2.GetCamera() != null)
            {
                return null;
            }
            if (overrideTarget != null)
            {
                return overrideTarget;
            }
            Inventory main3 = Inventory.main;
            if (main3 != null)
            {
                return main3.quickSlots;
            }
            return null;
        }
    }
}
