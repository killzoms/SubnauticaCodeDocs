using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AssemblyCSharp
{
    public class QuickSlots : IQuickSlots
    {
        private enum ArmsState
        {
            None,
            Draw,
            Hold,
            Holster,
            Drop
        }

        public delegate void OnBind(int slotID, TechType id, bool state);

        public delegate void OnToggle(int slotID, bool state);

        public delegate void OnSelect(int slotID);

        private static readonly string[] slotNames = new string[6] { "QuickSlot0", "QuickSlot1", "QuickSlot2", "QuickSlot3", "QuickSlot4", "QuickSlot5" };

        private GameObject owner;

        private ItemsContainer container;

        private Transform slotTransform;

        private Transform toolSocket;

        private Transform cameraSocket;

        private InventoryItem[] binding;

        private InventoryItem _heldItem;

        private int defaultLayer;

        private int viewModelLayer;

        private ArmsState state;

        private string activeToolName = string.Empty;

        private int desiredSlot = -1;

        private Sequence sequence = new Sequence();

        private TechType refillTechType;

        private int refillSlot = -1;

        private bool ignoreHotkeyInput;

        public InventoryItem heldItem => _heldItem;

        public int activeSlot => GetSlotByItem(_heldItem);

        public int slotCount { get; private set; }

        public event OnBind onBind;

        public event OnToggle onToggle;

        public event OnSelect onSelect;

        public QuickSlots(GameObject owner, Transform toolSocket, Transform cameraSocket, Inventory inv, Transform slotTr, int slotCount)
        {
            this.owner = owner;
            this.toolSocket = toolSocket;
            this.cameraSocket = cameraSocket;
            defaultLayer = LayerMask.NameToLayer("Default");
            viewModelLayer = LayerMask.NameToLayer("Viewmodel");
            this.slotCount = slotCount;
            binding = new InventoryItem[slotCount];
            container = inv.container;
            container.onAddItem += OnAddItem;
            container.onRemoveItem += OnRemoveItem;
            slotTransform = slotTr;
        }

        public TechType[] GetSlotBinding()
        {
            TechType[] array = new TechType[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                array[i] = binding[i]?.item.GetTechType() ?? TechType.None;
            }
            return array;
        }

        public void SetIgnoreHotkeyInput(bool ignore)
        {
            ignoreHotkeyInput = ignore;
        }

        public TechType GetSlotBinding(int slotID)
        {
            if (slotID < 0 || slotID >= slotCount)
            {
                return TechType.None;
            }
            return binding[slotID]?.item.GetTechType() ?? TechType.None;
        }

        public InventoryItem GetSlotItem(int slotID)
        {
            if (slotID < 0 || slotID >= slotCount)
            {
                return null;
            }
            return binding[slotID];
        }

        public int GetActiveSlotID()
        {
            return desiredSlot;
        }

        public bool IsToggled(int slotID)
        {
            if (slotID < 0 || slotID >= slotCount)
            {
                return false;
            }
            if (CraftData.GetQuickSlotType(GetSlotBinding(slotID)) == QuickSlotType.Passive)
            {
                return true;
            }
            return activeSlot == slotID;
        }

        public int GetSlotCount()
        {
            return slotCount;
        }

        public void Assign(InventoryItem item)
        {
            if (item == null || !item.isBindable)
            {
                return;
            }
            int slotByItem = GetSlotByItem(item);
            if (slotByItem == 0)
            {
                return;
            }
            if (slotByItem > 0)
            {
                InventoryItem slotItem = GetSlotItem(0);
                if (slotItem != null)
                {
                    Bind(slotByItem, slotItem);
                }
                Bind(0, item);
                return;
            }
            slotByItem = BindToEmpty(item);
            if (slotByItem >= 0)
            {
                return;
            }
            for (int num = slotCount; num >= 1; num--)
            {
                InventoryItem slotItem2 = GetSlotItem(num - 1);
                if (slotItem2 != null)
                {
                    Bind(num, slotItem2);
                }
            }
            Bind(0, item);
        }

        public void Bind(int slotID, InventoryItem item)
        {
            if (slotID < 0 || slotID >= slotCount || item == null || !item.isBindable)
            {
                return;
            }
            InventoryItem inventoryItem = binding[slotID];
            if (inventoryItem != null && inventoryItem == item)
            {
                Unbind(slotID);
                return;
            }
            for (int i = 0; i < slotCount; i++)
            {
                if (binding[i] == item)
                {
                    Unbind(i);
                    break;
                }
            }
            binding[slotID] = item;
            NotifyBind(slotID, state: true);
        }

        public void Unbind(int slotID)
        {
            if (slotID >= 0 && slotID < slotCount && binding[slotID] != null)
            {
                binding[slotID] = null;
                NotifyBind(slotID, state: false);
            }
        }

        public void SlotKeyDown(int slotID)
        {
            if (Player.main.GetPDA().isInUse)
            {
                InventoryItem hoveredItem = ItemDragManager.hoveredItem;
                if (hoveredItem != null && Inventory.main.GetCanBindItem(hoveredItem))
                {
                    if (slotID < 0)
                    {
                        Assign(hoveredItem);
                    }
                    else
                    {
                        Bind(slotID, hoveredItem);
                    }
                }
            }
            else if (AvatarInputHandler.main.IsEnabled() && Player.main.GetMode() == Player.Mode.Normal && !ignoreHotkeyInput)
            {
                Select(slotID);
            }
        }

        public void SlotKeyHeld(int slotID)
        {
        }

        public void SlotKeyUp(int slotID)
        {
        }

        public void SlotNext()
        {
            if (!AvatarInputHandler.main.IsEnabled() || Player.main.GetMode() != 0 || ignoreHotkeyInput)
            {
                return;
            }
            int activeSlotID = GetActiveSlotID();
            int num = GetSlotCount();
            int num2 = ((activeSlotID < 0) ? (-1) : activeSlotID);
            for (int i = 0; i < num; i++)
            {
                num2++;
                if (num2 >= num)
                {
                    num2 = 0;
                }
                TechType slotBinding = GetSlotBinding(num2);
                if (slotBinding != 0)
                {
                    QuickSlotType quickSlotType = CraftData.GetQuickSlotType(slotBinding);
                    if (quickSlotType == QuickSlotType.Selectable || quickSlotType == QuickSlotType.SelectableChargeable)
                    {
                        Select(num2);
                        break;
                    }
                }
            }
        }

        public void SlotPrevious()
        {
            if (!AvatarInputHandler.main.IsEnabled() || Player.main.GetMode() != 0 || ignoreHotkeyInput)
            {
                return;
            }
            int activeSlotID = GetActiveSlotID();
            int num = GetSlotCount();
            int num2 = ((activeSlotID < 0) ? num : activeSlotID);
            for (int i = 0; i < num; i++)
            {
                num2--;
                if (num2 < 0)
                {
                    num2 = num - 1;
                }
                TechType slotBinding = GetSlotBinding(num2);
                if (slotBinding != 0)
                {
                    QuickSlotType quickSlotType = CraftData.GetQuickSlotType(slotBinding);
                    if (quickSlotType == QuickSlotType.Selectable || quickSlotType == QuickSlotType.SelectableChargeable)
                    {
                        Select(num2);
                        break;
                    }
                }
            }
        }

        public void SlotLeftDown()
        {
        }

        public void SlotLeftHeld()
        {
        }

        public void SlotLeftUp()
        {
        }

        public void SlotRightDown()
        {
        }

        public void SlotRightHeld()
        {
        }

        public void SlotRightUp()
        {
        }

        public void DeselectSlots()
        {
            Deselect();
        }

        public float GetSlotProgress(int slotID)
        {
            return 1f;
        }

        public float GetSlotCharge(int slotID)
        {
            return 1f;
        }

        public void Select(int slotID)
        {
            if (slotID >= 0 && slotID < slotCount)
            {
                if (desiredSlot != slotID)
                {
                    desiredSlot = slotID;
                }
                else
                {
                    desiredSlot = -1;
                }
            }
        }

        public void Deselect()
        {
            desiredSlot = -1;
        }

        public void SelectImmediate(int slotID)
        {
            if (slotID < 0 || slotID >= slotCount)
            {
                return;
            }
            if (activeSlot != slotID)
            {
                DisposeAnimationState();
                sequence.Reset();
                sequence.ForceState(state: true);
                desiredSlot = slotID;
                SelectInternal(desiredSlot);
                if (_heldItem != null)
                {
                    PlayerTool component = _heldItem.item.GetComponent<PlayerTool>();
                    if (component != null && component.hasAnimations)
                    {
                        string animToolName = component.animToolName;
                        if (!string.IsNullOrEmpty(animToolName))
                        {
                            SetAnimationState(animToolName);
                            state = ArmsState.Draw;
                        }
                    }
                    else
                    {
                        state = ArmsState.Hold;
                    }
                }
                else
                {
                    desiredSlot = -1;
                    state = ArmsState.None;
                }
            }
            else
            {
                DeselectImmediate();
            }
        }

        public void DeselectImmediate()
        {
            if (_heldItem != null)
            {
                desiredSlot = -1;
                DisposeAnimationState();
                sequence.Reset();
                sequence.ForceState(state: false);
                state = ArmsState.None;
                DeselectInternal();
            }
        }

        public int GetSlotByItem(InventoryItem item)
        {
            if (item != null)
            {
                for (int i = 0; i < slotCount; i++)
                {
                    if (binding[i] == item)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public void Update()
        {
            sequence.Update();
            UpdateState();
            if (_heldItem != null)
            {
                int slotByItem = GetSlotByItem(_heldItem);
                if (slotByItem == -1)
                {
                    DeselectImmediate();
                }
                else
                {
                    Equipment.SendEquipmentEvent(_heldItem.item, 2, owner, slotNames[slotByItem]);
                }
            }
        }

        private void UpdateState()
        {
            int slotByItem = GetSlotByItem(_heldItem);
            if (refillTechType != 0 && refillSlot != -1)
            {
                TryRefill(refillTechType, refillSlot);
                refillTechType = TechType.None;
                refillSlot = -1;
            }
            switch (state)
            {
                case ArmsState.None:
                    if (desiredSlot == -1)
                    {
                        break;
                    }
                    SelectInternal(desiredSlot);
                    if (_heldItem != null)
                    {
                        PlayerTool component2 = _heldItem.item.GetComponent<PlayerTool>();
                        if (component2 != null && component2.hasAnimations)
                        {
                            string animToolName2 = component2.animToolName;
                            if (!string.IsNullOrEmpty(animToolName2))
                            {
                                SetAnimationState(animToolName2);
                                state = ArmsState.Draw;
                                sequence.Set(GetTransitionTime(), current: false, target: true, TransitionEnd);
                            }
                        }
                        else
                        {
                            state = ArmsState.Hold;
                        }
                    }
                    else
                    {
                        desiredSlot = -1;
                    }
                    break;
                case ArmsState.Draw:
                    if (slotByItem != desiredSlot)
                    {
                        DisposeAnimationState();
                        state = ArmsState.Holster;
                        float transitionTime = GetTransitionTime();
                        sequence.Set(transitionTime, current: true, target: false, TransitionEnd);
                    }
                    break;
                case ArmsState.Hold:
                    if (slotByItem != desiredSlot && !HeldToolIsInUse())
                    {
                        DisposeAnimationState();
                        state = ArmsState.Holster;
                        sequence.Set(GetTransitionTime(), current: true, target: false, TransitionEnd);
                    }
                    break;
                case ArmsState.Holster:
                    if (slotByItem != desiredSlot)
                    {
                        break;
                    }
                    if (_heldItem != null)
                    {
                        PlayerTool component = _heldItem.item.GetComponent<PlayerTool>();
                        if (component != null && component.hasAnimations)
                        {
                            string animToolName = component.animToolName;
                            if (!string.IsNullOrEmpty(animToolName))
                            {
                                SetAnimationState(animToolName);
                                state = ArmsState.Draw;
                                sequence.Set(GetTransitionTime(), current: false, target: true, TransitionEnd);
                            }
                        }
                        else
                        {
                            state = ArmsState.Hold;
                        }
                    }
                    else
                    {
                        desiredSlot = -1;
                    }
                    break;
            }
        }

        private float GetTransitionTime()
        {
            float value = 0.5f;
            float value2 = 0.35f;
            float value3 = 1f;
            if (_heldItem != null)
            {
                PlayerTool component = _heldItem.item.GetComponent<PlayerTool>();
                if (component != null)
                {
                    value = component.drawTime;
                    value2 = component.holsterTime;
                    value3 = component.dropTime;
                }
            }
            value = Mathf.Clamp(value, 0f, 10f);
            value2 = Mathf.Clamp(value2, 0f, 10f);
            value3 = Mathf.Clamp(value3, 0f, 10f);
            return state switch
            {
                ArmsState.Draw => value, 
                ArmsState.Holster => value2, 
                ArmsState.Drop => value3, 
                _ => 0f, 
            };
        }

        private void TransitionEnd()
        {
            switch (state)
            {
                case ArmsState.Draw:
                    state = ArmsState.Hold;
                    break;
                case ArmsState.Holster:
                    state = ArmsState.None;
                    DeselectInternal();
                    break;
                case ArmsState.Drop:
                    state = ArmsState.None;
                    break;
                case ArmsState.Hold:
                    break;
            }
        }

        private void SetAnimationState(string toolName)
        {
            DisposeAnimationState();
            if (!string.IsNullOrEmpty(toolName))
            {
                activeToolName = toolName;
                SafeAnimator.SetBool(Player.main.armsController.GetComponent<Animator>(), "holding_" + activeToolName, value: true);
            }
        }

        private void DisposeAnimationState()
        {
            if (!string.IsNullOrEmpty(activeToolName))
            {
                SafeAnimator.SetBool(Player.main.armsController.GetComponent<Animator>(), "holding_" + activeToolName, value: false);
                activeToolName = null;
            }
        }

        public void Drop(Vector3 force)
        {
            if (_heldItem != null && Inventory.CanDropItemHere(_heldItem.item, notify: true))
            {
                Pickupable item = _heldItem.item;
                refillTechType = item.GetTechType();
                refillSlot = GetSlotByItem(_heldItem);
                desiredSlot = refillSlot;
                item.Drop(item.transform.position, force);
            }
        }

        private bool HeldToolIsInUse()
        {
            if (_heldItem == null)
            {
                return false;
            }
            PlayerTool component = _heldItem.item.GetComponent<PlayerTool>();
            if (component == null)
            {
                return false;
            }
            return component.isInUse;
        }

        private void SelectInternal(int slotID)
        {
            DeselectInternal();
            InventoryItem inventoryItem = binding[slotID];
            if (inventoryItem != null)
            {
                Pickupable item = inventoryItem.item;
                PlayerTool component = item.GetComponent<PlayerTool>();
                if (component != null)
                {
                    DrawAsTool(component);
                }
                else
                {
                    DrawAsItem(inventoryItem);
                }
                _heldItem = inventoryItem;
                NotifyToggle(slotID, state: true);
                NotifySelect(slotID);
                Equipment.SendEquipmentEvent(item, 0, owner, slotNames[slotID]);
            }
        }

        private void DeselectInternal()
        {
            if (_heldItem != null)
            {
                int slotByItem = GetSlotByItem(_heldItem);
                Pickupable item = _heldItem.item;
                PlayerTool component = item.GetComponent<PlayerTool>();
                if (component != null)
                {
                    HolsterAsTool(component);
                }
                else
                {
                    HolsterAsItem(_heldItem);
                }
                _heldItem = null;
                NotifyToggle(slotByItem, state: false);
                NotifySelect(-1);
                Equipment.SendEquipmentEvent(slot: (slotByItem == -1) ? string.Empty : slotNames[slotByItem], pickupable: item, eventType: 1, owner: owner);
            }
        }

        public void SetViewModelVis(bool state)
        {
            if (_heldItem == null)
            {
                return;
            }
            PlayerTool component = _heldItem.item.GetComponent<PlayerTool>();
            if (component != null)
            {
                Player main = Player.main;
                if (state)
                {
                    component.OnDraw(main);
                }
                else
                {
                    component.OnHolster();
                }
                component.gameObject.SetActive(state);
            }
        }

        private void HolsterAsTool(PlayerTool tool)
        {
            tool.gameObject.SetActive(value: false);
            Utils.SetLayerRecursively(tool.gameObject, defaultLayer);
            if (tool.mainCollider != null)
            {
                tool.mainCollider.enabled = true;
            }
            tool.GetComponent<Rigidbody>().isKinematic = false;
            ItemsContainer itemsContainer = (ItemsContainer)_heldItem.container;
            if (itemsContainer != null)
            {
                _heldItem.item.Reparent(itemsContainer.tr);
            }
            Animator[] componentsInChildren = tool.GetComponentsInChildren<Animator>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
            tool.OnHolster();
            TechType techType = _heldItem.item.GetTechType();
            GoalManager.main.OnCustomGoalEvent("Equip_" + techType.AsString());
        }

        private void DrawAsTool(PlayerTool tool)
        {
            Transform socket = ((tool.socket == PlayerTool.Socket.Camera) ? cameraSocket : toolSocket);
            ModelPlug.PlugIntoSocket(tool, socket);
            Utils.SetLayerRecursively(tool.gameObject, viewModelLayer);
            Animator[] componentsInChildren = tool.GetComponentsInChildren<Animator>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            if (tool.mainCollider != null)
            {
                tool.mainCollider.enabled = false;
            }
            tool.GetComponent<Rigidbody>().isKinematic = true;
            tool.gameObject.SetActive(value: true);
            Player localPlayerComp = Utils.GetLocalPlayerComp();
            tool.OnDraw(localPlayerComp);
        }

        private void HolsterAsItem(InventoryItem item)
        {
            ItemsContainer itemsContainer = (ItemsContainer)item.container;
            if (itemsContainer != null)
            {
                item.item.Reparent(itemsContainer.tr);
            }
            item.item.SetVisible(visible: false);
            Utils.SetLayerRecursively(item.item.gameObject, defaultLayer);
        }

        private void DrawAsItem(InventoryItem item)
        {
            item.item.Reparent(slotTransform);
            item.item.SetVisible(visible: true);
            Utils.SetLayerRecursively(item.item.gameObject, viewModelLayer);
        }

        private int BindToEmpty(InventoryItem item)
        {
            int num = -1;
            for (int i = 0; i < binding.Length; i++)
            {
                if (binding[i] == null)
                {
                    num = i;
                    break;
                }
            }
            if (num == -1)
            {
                return -1;
            }
            Bind(num, item);
            return num;
        }

        private bool TryRefill(TechType techType, int slotID)
        {
            IList<InventoryItem> items = container.GetItems(techType);
            if (items == null)
            {
                return false;
            }
            for (int i = 0; i < items.Count; i++)
            {
                InventoryItem item = items[i];
                if (GetSlotByItem(item) == -1)
                {
                    Bind(slotID, item);
                    return true;
                }
            }
            return false;
        }

        public string[] SaveBinding()
        {
            string[] array = new string[binding.Length];
            for (int i = 0; i < binding.Length; i++)
            {
                InventoryItem inventoryItem = binding[i];
                if (inventoryItem != null && !(inventoryItem.item == null))
                {
                    UniqueIdentifier component = inventoryItem.item.GetComponent<UniqueIdentifier>();
                    if (!(component == null))
                    {
                        array[i] = component.Id;
                    }
                }
            }
            return array;
        }

        public void RestoreBinding(string[] uids)
        {
            for (int i = 0; i < binding.Length; i++)
            {
                Unbind(i);
            }
            foreach (InventoryItem item in (IEnumerable<InventoryItem>)container)
            {
                UniqueIdentifier component = item.item.GetComponent<UniqueIdentifier>();
                if (component == null)
                {
                    continue;
                }
                int num = Mathf.Min(uids.Length, binding.Length);
                for (int j = 0; j < num; j++)
                {
                    if (uids[j] == component.Id)
                    {
                        Bind(j, item);
                        break;
                    }
                }
            }
        }

        private void OnAddItem(InventoryItem item)
        {
            if (item == null || item.item == null || !item.isBindable)
            {
                return;
            }
            TechType techType = item.item.GetTechType();
            if (techType == TechType.ScrapMetal)
            {
                return;
            }
            for (int i = 0; i < binding.Length; i++)
            {
                InventoryItem inventoryItem = binding[i];
                if (inventoryItem != null && techType == inventoryItem.item.GetTechType())
                {
                    return;
                }
            }
            int num = BindToEmpty(item);
            if (_heldItem == null && num >= 0 && AvatarInputHandler.main.IsEnabled())
            {
                Select(num);
            }
        }

        private void OnRemoveItem(InventoryItem item)
        {
            int slotByItem = GetSlotByItem(item);
            if (item == _heldItem)
            {
                state = ArmsState.Drop;
                float transitionTime = GetTransitionTime();
                DeselectInternal();
                DisposeAnimationState();
                sequence.Set(transitionTime, current: true, target: false, TransitionEnd);
                desiredSlot = slotByItem;
            }
            if (slotByItem != -1)
            {
                Unbind(slotByItem);
                refillTechType = item.item.GetTechType();
                refillSlot = slotByItem;
            }
        }

        private void NotifyBind(int slotID, bool state)
        {
            if (this.onBind != null)
            {
                TechType id = (state ? binding[slotID].item.GetTechType() : TechType.None);
                this.onBind(slotID, id, state);
            }
        }

        private void NotifyToggle(int slotID, bool state)
        {
            if (this.onToggle != null)
            {
                this.onToggle(slotID, state);
            }
        }

        private void NotifySelect(int slotID)
        {
            if (this.onSelect != null)
            {
                this.onSelect(slotID);
            }
        }

        public void OnGUI()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("HeldItem: {0}\n", (_heldItem == null) ? "None" : _heldItem.item.name);
            stringBuilder.AppendFormat("activeSlot: {0}\ndesiredSlot: {1}\naction: {2}\nsequence.t: {3:f1}\nanimation: {4}\nrefillTechType: {5}\nrefillSlot: {6}\n", activeSlot, desiredSlot, state, sequence.t, string.IsNullOrEmpty(activeToolName) ? string.Empty : ("holding_" + activeToolName), refillTechType, refillSlot);
            GUI.Label(new Rect(10f, 10f, 500f, 500f), stringBuilder.ToString());
        }
    }
}
