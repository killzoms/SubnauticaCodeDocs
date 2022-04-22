using System.Collections;
using System.Collections.Generic;
using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    [SuppressMessage("Subnautica.Rules", "ValueTypeEnumeratorRule")]
    public class uGUI_CraftNode : TreeNode, IEnumerable<uGUI_CraftNode>, IEnumerable, uGUI_IIconManager, ISelectable
    {
        private const float expandPunchAmplitude = 1f;

        private const float clickPunchFrequency = 5f;

        private const float clickPunchAmplitude = 0.5f;

        private const float punchDuration = 1f;

        private const float punchDurationScatter = 0.2f;

        private static Atlas.Sprite _backgroundNormal;

        private static Atlas.Sprite _backgroundHovered;

        public TechType techType0;

        protected uGUI_CraftingMenu view;

        private static Atlas.Sprite backgroundNormal
        {
            get
            {
                if (_backgroundNormal == null)
                {
                    _backgroundNormal = SpriteManager.Get(SpriteManager.Group.Background, "categoryNormal");
                }
                return _backgroundNormal;
            }
        }

        private static Atlas.Sprite backgroundHovered
        {
            get
            {
                if (_backgroundHovered == null)
                {
                    _backgroundHovered = SpriteManager.Get(SpriteManager.Group.Background, "categoryHovered");
                }
                return _backgroundHovered;
            }
        }

        protected int index { get; private set; }

        public TreeAction action { get; private set; }

        public uGUI_ItemIcon icon { get; private set; }

        public bool locked { get; private set; }

        protected bool visible { get; private set; }

        protected bool expanded { get; private set; }

        protected int notifications { get; private set; }

        public uGUI_CraftNode(uGUI_CraftingMenu view, string id, int index, TreeAction action = TreeAction.None, TechType techType = TechType.None)
            : base(id)
        {
            this.view = view;
            this.index = index;
            this.action = action;
            techType0 = techType;
            locked = false;
            visible = false;
            expanded = false;
        }

        public new IEnumerator<uGUI_CraftNode> GetEnumerator()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                yield return (uGUI_CraftNode)nodes[i];
            }
        }

        private void PerformAction()
        {
            view.Action(this);
        }

        void uGUI_IIconManager.GetTooltip(uGUI_ItemIcon icon, out string tooltipText, List<TooltipIcon> tooltipIcons)
        {
            view.GetTooltip(this, out tooltipText, tooltipIcons);
        }

        void uGUI_IIconManager.OnPointerEnter(uGUI_ItemIcon icon)
        {
            if (icon != null)
            {
                icon.SetBackgroundSprite(backgroundHovered);
            }
        }

        void uGUI_IIconManager.OnPointerExit(uGUI_ItemIcon icon)
        {
            if (icon != null)
            {
                icon.SetBackgroundSprite(backgroundNormal);
            }
        }

        bool uGUI_IIconManager.OnPointerClick(uGUI_ItemIcon icon, int button)
        {
            if (button == 0)
            {
                PerformAction();
            }
            else
            {
                _ = 1;
            }
            return true;
        }

        bool uGUI_IIconManager.OnBeginDrag(uGUI_ItemIcon icon)
        {
            return true;
        }

        void uGUI_IIconManager.OnEndDrag(uGUI_ItemIcon icon)
        {
        }

        void uGUI_IIconManager.OnDrop(uGUI_ItemIcon icon)
        {
        }

        void uGUI_IIconManager.OnDragHoverEnter(uGUI_ItemIcon icon)
        {
        }

        void uGUI_IIconManager.OnDragHoverStay(uGUI_ItemIcon icon)
        {
        }

        void uGUI_IIconManager.OnDragHoverExit(uGUI_ItemIcon icon)
        {
        }

        protected void OnSiblingClicked()
        {
            List<TreeNode>.Enumerator enumerator = nodes.GetEnumerator();
            while (enumerator.MoveNext())
            {
                uGUI_CraftNode obj = (uGUI_CraftNode)enumerator.Current;
                obj.SetExpanded(expanded: false);
                obj.HideRecursively(includeSelf: false);
            }
        }

        public void Expand()
        {
            (base.parent as uGUI_CraftNode)?.OnSiblingClicked();
            SetExpanded(expanded: true);
            using IEnumerator<uGUI_CraftNode> enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                uGUI_CraftNode current = enumerator.Current;
                current.SetVisible(visible: true);
                current.Punch(0.1f * ((float)current.index / (float)current.siblingCount), 1f);
            }
        }

        public bool Close()
        {
            if (base.parent == null)
            {
                return false;
            }
            uGUI_CraftNode obj = base.parent as uGUI_CraftNode;
            obj.OnSiblingClicked();
            obj.NavigationSelect();
            return true;
        }

        public void SetLocked(bool locked)
        {
            if (this.locked != locked)
            {
                this.locked = locked;
                view.SetDirty();
            }
        }

        protected void SetVisible(bool visible)
        {
            if (this.visible == visible)
            {
                return;
            }
            this.visible = visible;
            if (visible)
            {
                if (icon == null)
                {
                    CreateIcon();
                    view.SetDirty();
                }
            }
            else
            {
                expanded = false;
            }
            if (icon != null)
            {
                icon.SetActive(visible);
            }
        }

        public void SetExpanded(bool expanded)
        {
            if (this.expanded != expanded)
            {
                this.expanded = expanded;
                view.SetDirty();
            }
        }

        public void SetProgress(float progress)
        {
            if (icon != null)
            {
                icon.SetProgress(progress);
            }
            ((uGUI_CraftNode)base.parent)?.SetProgress(progress);
        }

        public void Remove()
        {
            if (!(icon == null))
            {
                base.OnDestroy();
            }
        }

        public void Punch(float delay = 0f, float amplitude = 0.5f)
        {
            if (!(icon == null))
            {
                float duration = 1f + Random.Range(-0.2f, 0.2f);
                icon.PunchScale(5f, amplitude, duration, delay);
            }
        }

        protected void HideRecursively(bool includeSelf = true)
        {
            if (base.childCount > 0)
            {
                using IEnumerator<TreeNode> enumerator = Traverse(includeSelf);
                while (enumerator.MoveNext())
                {
                    ((uGUI_CraftNode)enumerator.Current).SetVisible(visible: false);
                }
            }
            else if (includeSelf)
            {
                SetVisible(visible: false);
            }
        }

        public void UpdateRecursively(ref int c, ref int n)
        {
            int c2 = 0;
            int n2 = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                (nodes[i] as uGUI_CraftNode).UpdateRecursively(ref c2, ref n2);
            }
            notifications = n2;
            bool available = false;
            if (action == TreeAction.Craft)
            {
                if (view.ActionAvailable(this))
                {
                    c++;
                    available = true;
                }
                if (NotificationManager.main.Contains(NotificationManager.Group.CraftTree, techType0.EncodeKey()))
                {
                    n++;
                }
            }
            else if (action == TreeAction.Expand)
            {
                available = c2 > 0;
            }
            c += c2;
            n += n2;
            UpdateIcon(available);
        }

        protected void CreateIcon()
        {
            if (base.parent != null)
            {
                TechType techType = ((action == TreeAction.Craft) ? techType0 : TechType.None);
                RectTransform rectTransform = ((uGUI_CraftNode)base.parent).GetRectTransform();
                float width = rectTransform.rect.width;
                float num = 1f / Mathf.Pow(1.28f, base.depth - 1);
                float num2 = Mathf.Max(40f, 92f * num);
                float num3 = 0.674f * num2;
                Vector2 anchor = new Vector2(0.5f, 0.5f);
                Vector2 pivot = new Vector2(0.5f, 0.5f);
                Atlas.Sprite foregroundSprite = SpriteManager.defaultSprite;
                Atlas.Sprite backgroundSprite = null;
                switch (action)
                {
                    case TreeAction.Expand:
                        foregroundSprite = SpriteManager.Get(SpriteManager.Group.Category, $"{view.id}_{base.id}");
                        backgroundSprite = backgroundNormal;
                        break;
                    case TreeAction.Craft:
                        foregroundSprite = SpriteManager.Get(techType);
                        backgroundSprite = backgroundNormal;
                        break;
                }
                GameObject gameObject = new GameObject(base.id);
                gameObject.layer = LayerID.UI;
                icon = gameObject.AddComponent<uGUI_ItemIcon>();
                icon.Init(this, rectTransform, anchor, pivot);
                icon.SetActiveSize(num2, num2);
                icon.SetBackgroundSize(num2, num2);
                icon.SetForegroundSize(num3, num3);
                icon.SetForegroundSprite(foregroundSprite);
                icon.SetBackgroundSprite(backgroundSprite);
                icon.SetBackgroundRadius(Mathf.Min(num2, num2) * 0.5f);
                icon.SetPosition((width > 0f) ? (0.5f * (width + num2)) : 0f, (0.5f * (float)(base.siblingCount - 1) - (float)index) * num2);
                if (action == TreeAction.Craft)
                {
                    NotificationManager.main.RegisterTarget(NotificationManager.Group.CraftTree, techType.EncodeKey(), icon);
                }
            }
        }

        private bool IsLockedInHierarchy()
        {
            bool result = false;
            for (uGUI_CraftNode uGUI_CraftNode2 = this; uGUI_CraftNode2 != null; uGUI_CraftNode2 = uGUI_CraftNode2.parent as uGUI_CraftNode)
            {
                if (uGUI_CraftNode2.locked)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        protected void UpdateIcon(bool available)
        {
            if (icon == null)
            {
                return;
            }
            if (visible)
            {
                bool flag = IsLockedInHierarchy();
                icon.SetChroma((available && !flag) ? 1f : 0f);
            }
            if (expanded)
            {
                icon.SetNotificationAlpha(0f);
                return;
            }
            if (icon.SetNotificationAlpha((notifications > 0) ? 1f : 0f))
            {
                icon.SetNotificationBackgroundColor(NotificationManager.notificationColor);
                icon.SetNotificationAnchor(UIAnchor.UpperRight);
                Vector2 vector = icon.rectTransform.rect.size * 0.5f;
                Vector2 vector2 = 0.8f * vector;
                icon.SetNotificationOffset(vector2 * 0.7071068f - vector);
            }
            icon.SetNotificationNumber(notifications);
        }

        protected RectTransform GetRectTransform()
        {
            if (base.parent != null)
            {
                return icon.rectTransform;
            }
            return view.iconsCanvas;
        }

        public static uGUI_CraftNode CreateFromCraftTree(uGUI_CraftingMenu view, RectTransform parent, CraftNode sourceTree, CraftNode.Filter filter)
        {
            using IEnumerator<CraftNode> enumerator = sourceTree.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                Debug.LogError("uGUI_CraftNode : CreateFromCraftTree() : Provided sourceTree is empty!");
                return null;
            }
            CraftNode current = enumerator.Current;
            uGUI_CraftNode result = new uGUI_CraftNode(view, current.id, 0, current.action, current.techType0);
            AddCraftNodes(result, sourceTree, filter);
            return result;
        }

        private static void AddCraftNodes(uGUI_CraftNode parent, CraftNode sourceTree, CraftNode.Filter filter)
        {
            using IEnumerator<CraftNode> enumerator = sourceTree.GetEnumerator();
            int num = 0;
            while (enumerator.MoveNext())
            {
                CraftNode current = enumerator.Current;
                if (filter(current))
                {
                    uGUI_CraftNode uGUI_CraftNode2 = new uGUI_CraftNode(parent.view, current.id, num, current.action, current.techType0);
                    parent.AddNode(uGUI_CraftNode2);
                    AddCraftNodes(uGUI_CraftNode2, current, filter);
                    num++;
                }
            }
        }

        bool ISelectable.IsValid()
        {
            return this != null;
        }

        RectTransform ISelectable.GetRect()
        {
            return GetRectTransform();
        }

        bool ISelectable.OnButtonDown(GameInput.Button button)
        {
            if (view.GetIconsLocked())
            {
                if ((uint)(button - 27) <= 1u)
                {
                    view.Deselect();
                    return true;
                }
                return false;
            }
            switch (button)
            {
                case GameInput.Button.UISubmit:
                    if (action == TreeAction.Craft)
                    {
                        PerformAction();
                        return true;
                    }
                    return HandleNavigation(1, 0);
                case GameInput.Button.UICancel:
                    return HandleNavigation(-1, 0);
                default:
                    return false;
            }
        }

        public bool HandleNavigation(int dirX, int dirY)
        {
            if (view.GetIconsLocked())
            {
                return false;
            }
            if (dirX < 0)
            {
                if (!base.parent.isRoot)
                {
                    uGUI_CraftNode obj = base.parent as uGUI_CraftNode;
                    obj.Close();
                    obj.NavigationSelect();
                }
                else
                {
                    view.Deselect();
                }
                return true;
            }
            if (dirX > 0)
            {
                if (action == TreeAction.Expand)
                {
                    PerformAction();
                    (base[0] as uGUI_CraftNode)?.NavigationSelect();
                }
                return true;
            }
            if (dirY > 0)
            {
                (base.parent[index + 1] as uGUI_CraftNode)?.NavigationSelect();
                return true;
            }
            if (dirY < 0)
            {
                (base.parent[index - 1] as uGUI_CraftNode)?.NavigationSelect();
                return true;
            }
            return false;
        }

        public void NavigationSelect()
        {
            if (view.selectedNode != null)
            {
                view.selectedNode.NavigationDeselect();
            }
            UISelection.selected = this;
            SetExpanded(expanded: true);
            uGUI_Tooltip.Set(icon);
        }

        public void NavigationDeselect()
        {
            SetExpanded(expanded: false);
            uGUI_Tooltip.Clear();
            UISelection.selected = null;
        }
    }
}
