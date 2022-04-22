using System.Collections.Generic;
using Gendarme;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(RectTransform))]
    public class uGUI_CraftingMenu : uGUI_InputGroup, ITreeActionSender, uGUI_INavigableIconGrid
    {
        private const ManagedUpdate.Queue updateQueue = ManagedUpdate.Queue.Canvas;

        [AssertNotNull]
        public uGUI_CanvasScaler canvasScaler;

        [AssertNotNull]
        public CanvasGroup canvasGroup;

        [AssertNotNull]
        public RectTransform iconsCanvas;

        [AssertNotNull]
        public FMODAsset soundOpen;

        [AssertNotNull]
        public FMODAsset soundExpand;

        [AssertNotNull]
        public FMODAsset soundAccept;

        [AssertNotNull]
        public FMODAsset soundDeny;

        private ITreeActionReceiver _client;

        private uGUI_CraftNode craftedNode;

        private uGUI_CraftNode icons;

        private bool interactable = true;

        private bool isDirty;

        public string id { get; private set; }

        public ITreeActionReceiver client => _client;

        [SuppressMessage("Gendarme.Rules.BadPractice", "PreferEmptyInstanceOverNullRule")]
        public uGUI_CraftNode selectedNode
        {
            get
            {
                if (UISelection.selected != null && UISelection.selected.IsValid())
                {
                    return UISelection.selected as uGUI_CraftNode;
                }
                return null;
            }
        }

        public void Open(CraftTree.Type treeType, ITreeActionReceiver receiver)
        {
            if (base.selected)
            {
                return;
            }
            CraftTree tree = CraftTree.GetTree(treeType);
            if (tree != null)
            {
                id = tree.id;
                canvasGroup.alpha = 1f;
                icons = uGUI_CraftNode.CreateFromCraftTree(this, iconsCanvas, tree.nodes, FilterNode);
                icons.Expand();
                _client = receiver;
                if (_client.inProgress)
                {
                    icons.SetLocked(locked: true);
                }
                ItemsContainer container = Inventory.main.container;
                container.onAddItem += InventoryChanged;
                container.onRemoveItem += InventoryChanged;
                Select();
                GamepadInputModule.current.SetCurrentGrid(this);
                ManagedUpdate.Subscribe(ManagedUpdate.Queue.Canvas, OnWillRenderCanvases);
                FMODUWE.PlayOneShot(soundOpen, MainCamera.camera.transform.position);
            }
        }

        public void Close(ITreeActionReceiver receiver)
        {
            ManagedUpdate.Unsubscribe(ManagedUpdate.Queue.Canvas, OnWillRenderCanvases);
            if (_client != null && _client == receiver)
            {
                Deselect();
            }
        }

        public void Lock(ITreeActionReceiver receiver)
        {
            if (_client != null && _client == receiver)
            {
                SetInteractable(value: false);
            }
        }

        public bool ActionAvailable(uGUI_CraftNode sender)
        {
            TreeAction action = sender.action;
            TechType techType = sender.techType0;
            switch (action)
            {
                case TreeAction.Expand:
                    return true;
                case TreeAction.Craft:
                    if (CrafterLogic.IsCraftRecipeUnlocked(techType))
                    {
                        return CrafterLogic.IsCraftRecipeFulfilled(techType);
                    }
                    return false;
                default:
                    return false;
            }
        }

        public override void OnSelect(bool lockMovement)
        {
            base.OnSelect(lockMovement);
            canvasScaler.SetAnchor();
        }

        public override void OnDeselect()
        {
            ((uGUI_INavigableIconGrid)this).DeselectItem();
            SetInteractable(value: true);
            for (int num = iconsCanvas.childCount - 1; num >= 0; num--)
            {
                Object.Destroy(iconsCanvas.GetChild(num).gameObject);
            }
            if (icons != null)
            {
                icons.Remove();
                icons = null;
            }
            _client = null;
            craftedNode = null;
            ItemsContainer container = Inventory.main.container;
            if (container != null)
            {
                container.onAddItem -= InventoryChanged;
                container.onRemoveItem -= InventoryChanged;
            }
            base.OnDeselect();
        }

        public void Action(uGUI_CraftNode sender)
        {
            if (_client != null && interactable && ActionAvailable(sender))
            {
                sender.Punch();
                switch (sender.action)
                {
                    case TreeAction.Expand:
                        sender.Expand();
                        FMODUWE.PlayOneShot(soundExpand, MainCamera.camera.transform.position);
                        break;
                    case TreeAction.Craft:
                        if (_client.PerformAction(this, sender.techType0))
                        {
                            if (icons != null)
                            {
                                icons.SetLocked(locked: true);
                            }
                            craftedNode = sender;
                        }
                        FMODUWE.PlayOneShot(soundAccept, MainCamera.camera.transform.position);
                        break;
                }
            }
            else
            {
                sender.Punch();
                FMODUWE.PlayOneShot(soundDeny, MainCamera.camera.transform.position);
            }
        }

        public void GetTooltip(uGUI_CraftNode node, out string tooltipText, List<TooltipIcon> tooltipIcons)
        {
            if (node != null)
            {
                switch (node.action)
                {
                    case TreeAction.Expand:
                        tooltipText = TooltipFactory.Label($"{id}Menu_{node.id}");
                        return;
                    case TreeAction.Craft:
                    {
                        TechType techType = node.techType0;
                        bool locked = !CrafterLogic.IsCraftRecipeUnlocked(techType);
                        TooltipFactory.Recipe(techType, locked, out tooltipText, tooltipIcons);
                        return;
                    }
                }
            }
            tooltipText = null;
        }

        public void InventoryChanged(InventoryItem item)
        {
            SetDirty();
        }

        public void SetDirty()
        {
            isDirty = true;
        }

        public void OnWillRenderCanvases()
        {
            if (isDirty)
            {
                if (icons != null)
                {
                    int c = 0;
                    int n = 0;
                    icons.UpdateRecursively(ref c, ref n);
                }
                isDirty = false;
            }
        }

        void ITreeActionSender.Progress(float progress)
        {
            if (!interactable)
            {
                SetAlpha(1f - progress / 0.3f);
            }
            if (craftedNode != null)
            {
                craftedNode.SetProgress(progress);
            }
        }

        void ITreeActionSender.Done()
        {
            SetInteractable(value: true);
            if (craftedNode != null)
            {
                craftedNode.SetProgress(1f);
                craftedNode = null;
            }
            if (icons != null)
            {
                icons.SetLocked(locked: false);
            }
        }

        private bool FilterNode(TreeNode sample)
        {
            CraftNode craftNode = (CraftNode)sample;
            TreeAction action = craftNode.action;
            bool flag = GameModeUtils.RequiresSurvival();
            switch (action)
            {
                case TreeAction.Expand:
                {
                    if (craftNode.id == "Survival" && !flag)
                    {
                        return false;
                    }
                    if (craftNode.id == "Submarine" && !Player.main.IsInSubmarine())
                    {
                        return false;
                    }
                    bool flag2 = true;
                    using (IEnumerator<CraftNode> enumerator = craftNode.Traverse(includeSelf: false))
                    {
                        while (enumerator.MoveNext())
                        {
                            CraftNode current = enumerator.Current;
                            TreeAction action2 = current.action;
                            if (action2 != TreeAction.Expand && action2 == TreeAction.Craft && FilterCraftActionNode(current))
                            {
                                flag2 = false;
                                break;
                            }
                        }
                    }
                    if (flag2)
                    {
                        return false;
                    }
                    break;
                }
                case TreeAction.Craft:
                    if (!FilterCraftActionNode(craftNode))
                    {
                        return false;
                    }
                    break;
            }
            return true;
        }

        private bool FilterCraftActionNode(CraftNode node)
        {
            if (node.action != TreeAction.Craft)
            {
                return false;
            }
            if (!GameModeUtils.RequiresBlueprints())
            {
                return true;
            }
            TechType techType = node.techType0;
            if (CraftData.Get(techType) == null)
            {
                Debug.LogError("CraftTree : FilterNode() : CradtData.Get returned null ITechData for '" + techType.AsString() + "'");
                return false;
            }
            TechUnlockState techUnlockState = KnownTech.GetTechUnlockState(techType);
            if (techUnlockState == TechUnlockState.Available || techUnlockState == TechUnlockState.Locked)
            {
                return true;
            }
            return false;
        }

        private void SetAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            canvasGroup.alpha = MathExtensions.EaseInSine(alpha);
        }

        private void SetInteractable(bool value)
        {
            if (interactable != value)
            {
                interactable = value;
                canvasGroup.interactable = interactable;
                canvasGroup.blocksRaycasts = interactable;
                if (interactable)
                {
                    SetAlpha(1f);
                }
            }
        }

        object uGUI_INavigableIconGrid.GetSelectedItem()
        {
            return selectedNode;
        }

        Graphic uGUI_INavigableIconGrid.GetSelectedIcon()
        {
            return selectedNode?.icon;
        }

        void uGUI_INavigableIconGrid.SelectItem(object item)
        {
            (item as uGUI_CraftNode)?.NavigationSelect();
        }

        void uGUI_INavigableIconGrid.DeselectItem()
        {
            selectedNode?.NavigationDeselect();
        }

        bool uGUI_INavigableIconGrid.SelectFirstItem()
        {
            using (IEnumerator<uGUI_CraftNode> enumerator = icons.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    ((uGUI_INavigableIconGrid)this).SelectItem((object)enumerator.Current);
                    return true;
                }
            }
            return false;
        }

        bool uGUI_INavigableIconGrid.SelectItemClosestToPosition(Vector3 screenPos)
        {
            return false;
        }

        bool uGUI_INavigableIconGrid.SelectItemInDirection(int dirX, int dirY)
        {
            if (selectedNode == null)
            {
                return ((uGUI_INavigableIconGrid)this).SelectFirstItem();
            }
            if (dirX == 0 && dirY == 0)
            {
                return false;
            }
            return selectedNode.HandleNavigation(dirX, dirY);
        }

        uGUI_INavigableIconGrid uGUI_INavigableIconGrid.GetNavigableGridInDirection(int dirX, int dirY)
        {
            return null;
        }

        public bool GetIconsLocked()
        {
            return icons.locked;
        }
    }
}
