using System;
using System.Collections.Generic;
using System.Text;
using Gendarme;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    public class uGUI_BlueprintsTab : uGUI_PDATab, ICompileTimeCheckable, uGUI_IIconManager, uGUI_INavigableIconGrid, uGUI_IScrollReceiver
    {
        private class CategoryEntry
        {
            public RectTransform title;

            public Text titleText;

            public RectTransform canvas;

            public Dictionary<TechType, uGUI_BlueprintEntry> entries = new Dictionary<TechType, uGUI_BlueprintEntry>(TechTypeExtensions.sTechTypeComparer);
        }

        private static readonly List<TechCategory> sTechCategories = new List<TechCategory>();

        private static readonly List<TechType> sTechTypes = new List<TechType>();

        private static readonly CachedEnumString<TechCategory> techCategoryStrings = new CachedEnumString<TechCategory>("TechCategory", CraftData.sTechCategoryComparer);

        public static readonly CachedEnumString<TechType> blueprintEntryStrings = new CachedEnumString<TechType>(string.Empty, ".BlueprintsTab", TechTypeExtensions.sTechTypeComparer);

        private const NotificationManager.Group notificationGroup = NotificationManager.Group.Blueprints;

        private static readonly List<TechGroup> groups = new List<TechGroup>
        {
            TechGroup.Resources,
            TechGroup.Survival,
            TechGroup.Personal,
            TechGroup.Machines,
            TechGroup.Constructor,
            TechGroup.Workbench,
            TechGroup.VehicleUpgrades,
            TechGroup.MapRoomUpgrades,
            TechGroup.Cyclops,
            TechGroup.BasePieces,
            TechGroup.ExteriorModules,
            TechGroup.InteriorPieces,
            TechGroup.InteriorModules,
            TechGroup.Miscellaneous,
            TechGroup.Uncategorized
        };

        private const string blueprintsLabelKey = "BlueprintsLabel";

        [AssertNotNull]
        public GameObject content;

        [AssertNotNull]
        public RectTransform canvas;

        [AssertNotNull]
        public ScrollRect scrollRect;

        [AssertNotNull]
        public GameObject prefabTitle;

        [AssertNotNull]
        public GameObject prefabEntry;

        [AssertNotNull]
        public Text blueprintsLabel;

        public FMODAsset soundUnlock;

        private Dictionary<TechCategory, CategoryEntry> entries = new Dictionary<TechCategory, CategoryEntry>(CraftData.sTechCategoryComparer);

        private bool isDirty = true;

        private int _notificationsCount;

        private bool scrollChangedPosition;

        public override int notificationsCount => _notificationsCount;

        private uGUI_BlueprintEntry selectedEntry
        {
            get
            {
                if (UISelection.selected != null)
                {
                    return UISelection.selected as uGUI_BlueprintEntry;
                }
                return null;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            Close();
        }

        private void Start()
        {
            KnownTech.onChanged += OnCompletedChanged;
            KnownTech.onCompoundAdd += OnCompoundAdd;
            KnownTech.onCompoundRemove += OnCompoundRemove;
            KnownTech.onCompoundProgress += OnCompoundProgress;
            PDAScanner.onAdd = (PDAScanner.OnEntryEvent)Delegate.Combine(PDAScanner.onAdd, new PDAScanner.OnEntryEvent(OnLockedAdd));
            PDAScanner.onRemove = (PDAScanner.OnEntryEvent)Delegate.Combine(PDAScanner.onRemove, new PDAScanner.OnEntryEvent(OnLockedRemove));
            PDAScanner.onProgress = (PDAScanner.OnEntryEvent)Delegate.Combine(PDAScanner.onProgress, new PDAScanner.OnEntryEvent(OnLockedProgress));
        }

        private void OnDestroy()
        {
            KnownTech.onChanged -= OnCompletedChanged;
            KnownTech.onCompoundAdd -= OnCompoundAdd;
            KnownTech.onCompoundRemove -= OnCompoundRemove;
            KnownTech.onCompoundProgress -= OnCompoundProgress;
            PDAScanner.onAdd = (PDAScanner.OnEntryEvent)Delegate.Remove(PDAScanner.onAdd, new PDAScanner.OnEntryEvent(OnLockedAdd));
            PDAScanner.onRemove = (PDAScanner.OnEntryEvent)Delegate.Remove(PDAScanner.onRemove, new PDAScanner.OnEntryEvent(OnLockedRemove));
            PDAScanner.onProgress = (PDAScanner.OnEntryEvent)Delegate.Remove(PDAScanner.onProgress, new PDAScanner.OnEntryEvent(OnLockedProgress));
        }

        private void LateUpdate()
        {
            UpdateEntries();
            UpdateNotificationsCount();
        }

        private void UpdateNotificationsCount()
        {
            _notificationsCount = 0;
            NotificationManager main = NotificationManager.main;
            Dictionary<TechCategory, CategoryEntry>.Enumerator enumerator = entries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Dictionary<TechType, uGUI_BlueprintEntry>.Enumerator enumerator2 = enumerator.Current.Value.entries.GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    if (main.Contains(NotificationManager.Group.Blueprints, enumerator2.Current.Key.EncodeKey()))
                    {
                        _notificationsCount++;
                    }
                }
            }
        }

        public override void Open()
        {
            SetState(state: true);
        }

        public override void Close()
        {
            SetState(state: false);
        }

        public override uGUI_INavigableIconGrid GetInitialGrid()
        {
            return this;
        }

        public override void OnLanguageChanged()
        {
            blueprintsLabel.text = Language.main.Get("BlueprintsLabel");
            Dictionary<TechCategory, CategoryEntry>.Enumerator enumerator = entries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<TechCategory, CategoryEntry> current = enumerator.Current;
                TechCategory key = current.Key;
                CategoryEntry value = current.Value;
                SetCategoryTitle(value, key);
                Dictionary<TechType, uGUI_BlueprintEntry>.Enumerator enumerator2 = value.entries.GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    KeyValuePair<TechType, uGUI_BlueprintEntry> current2 = enumerator2.Current;
                    TechType key2 = current2.Key;
                    uGUI_BlueprintEntry value2 = current2.Value;
                    SetEntryText(value2, key2);
                }
            }
        }

        private void OnCompletedChanged(HashSet<TechType> tech)
        {
            isDirty = true;
        }

        private void OnCompoundAdd(TechType techType, int unlocked, int total)
        {
            isDirty = true;
        }

        private void OnCompoundRemove(TechType techType)
        {
            isDirty = true;
        }

        private void OnCompoundProgress(TechType techType, int unlocked, int total)
        {
            isDirty = true;
        }

        private void OnLockedAdd(PDAScanner.Entry entry)
        {
            isDirty = true;
        }

        private void OnLockedRemove(PDAScanner.Entry entry)
        {
            isDirty = true;
        }

        private void OnLockedProgress(PDAScanner.Entry entry)
        {
            isDirty = true;
        }

        private void SetState(bool state)
        {
            content.SetActive(state);
        }

        private void UpdateEntries()
        {
            if (!isDirty)
            {
                return;
            }
            isDirty = false;
            for (int i = 0; i < groups.Count; i++)
            {
                TechGroup group = groups[i];
                CraftData.GetBuilderCategories(group, sTechCategories);
                for (int j = 0; j < sTechCategories.Count; j++)
                {
                    TechCategory techCategory = sTechCategories[j];
                    CraftData.GetBuilderTech(group, techCategory, sTechTypes);
                    for (int k = 0; k < sTechTypes.Count; k++)
                    {
                        TechType techType = sTechTypes[k];
                        uGUI_BlueprintEntry value = null;
                        if (entries.TryGetValue(techCategory, out var value2))
                        {
                            value2.entries.TryGetValue(techType, out value);
                        }
                        int unlocked;
                        int total;
                        TechUnlockState techUnlockState = KnownTech.GetTechUnlockState(techType, out unlocked, out total);
                        bool num = value != null;
                        bool flag = techUnlockState == TechUnlockState.Available || techUnlockState == TechUnlockState.Locked;
                        if (num != flag)
                        {
                            if (flag)
                            {
                                if (value2 == null)
                                {
                                    value2 = new CategoryEntry();
                                    GameObject obj = global::UnityEngine.Object.Instantiate(prefabTitle);
                                    RectTransform component = obj.GetComponent<RectTransform>();
                                    component.SetParent(canvas, worldPositionStays: false);
                                    value2.title = component;
                                    Text text = (value2.titleText = obj.GetComponentInChildren<Text>());
                                    SetCategoryTitle(value2, techCategory);
                                    GameObject obj2 = new GameObject("CategoryCanvas");
                                    RectTransform rectTransform = obj2.AddComponent<RectTransform>();
                                    rectTransform.SetParent(canvas, worldPositionStays: false);
                                    value2.canvas = rectTransform;
                                    obj2.AddComponent<FlexibleGridLayout>();
                                    entries.Add(techCategory, value2);
                                }
                                GameObject obj3 = global::UnityEngine.Object.Instantiate(prefabEntry);
                                obj3.transform.SetParent(value2.canvas, worldPositionStays: false);
                                value = obj3.GetComponent<uGUI_BlueprintEntry>();
                                value.Initialize(this);
                                value.SetIcon(techType);
                                SetEntryText(value, techType);
                                value2.entries.Add(techType, value);
                                NotificationManager.main.RegisterTarget(NotificationManager.Group.Blueprints, techType.EncodeKey(), value);
                            }
                            else
                            {
                                NotificationManager.main.UnregisterTarget(value);
                                value2.entries.Remove(techType);
                                global::UnityEngine.Object.Destroy(value.gameObject);
                                if (value2.entries.Count == 0)
                                {
                                    global::UnityEngine.Object.Destroy(value2.title.gameObject);
                                    global::UnityEngine.Object.Destroy(value2.canvas.gameObject);
                                    entries.Remove(techCategory);
                                }
                            }
                        }
                        if (value != null)
                        {
                            value.SetValue(unlocked, total);
                        }
                    }
                }
            }
            UpdateOrder();
        }

        private void UpdateOrder()
        {
            List<TechCategory> list = new List<TechCategory>();
            int num = 0;
            for (int i = 0; i < groups.Count; i++)
            {
                TechGroup techGroup = groups[i];
                CraftData.GetBuilderCategories(techGroup, list);
                for (int j = 0; j < list.Count; j++)
                {
                    TechCategory techCategory = list[j];
                    if (entries.TryGetValue(techCategory, out var value))
                    {
                        value.title.SetSiblingIndex(num);
                        num++;
                        value.canvas.SetSiblingIndex(num);
                        num++;
                        UpdateOrder(techGroup, techCategory, value.entries);
                    }
                }
            }
        }

        private void UpdateOrder(TechGroup techGroup, TechCategory techCategory, Dictionary<TechType, uGUI_BlueprintEntry> entries)
        {
            List<TechType> list = new List<TechType>();
            int num = 0;
            CraftData.GetBuilderTech(techGroup, techCategory, list);
            for (int i = 0; i < list.Count; i++)
            {
                TechType key = list[i];
                if (entries.TryGetValue(key, out var value))
                {
                    value.rectTransform.SetSiblingIndex(num);
                    num++;
                }
            }
        }

        private void SetCategoryTitle(CategoryEntry categoryEntry, TechCategory techCategory)
        {
            categoryEntry.titleText.text = Language.main.Get(techCategoryStrings.Get(techCategory));
        }

        private void SetEntryText(uGUI_BlueprintEntry blueprintEntry, TechType techType)
        {
            string orFallback = Language.main.GetOrFallback(blueprintEntryStrings.Get(techType), techType);
            blueprintEntry.SetText(orFallback);
        }

        void uGUI_IIconManager.GetTooltip(uGUI_ItemIcon icon, out string tooltipText, List<TooltipIcon> tooltipIcons)
        {
            uGUI_BlueprintEntry componentInParent = icon.GetComponentInParent<uGUI_BlueprintEntry>();
            if (componentInParent != null)
            {
                Dictionary<TechCategory, CategoryEntry>.Enumerator enumerator = entries.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Dictionary<TechType, uGUI_BlueprintEntry>.Enumerator enumerator2 = enumerator.Current.Value.entries.GetEnumerator();
                    while (enumerator2.MoveNext())
                    {
                        KeyValuePair<TechType, uGUI_BlueprintEntry> current = enumerator2.Current;
                        if (current.Value == componentInParent)
                        {
                            TechType key = current.Key;
                            bool locked = !CrafterLogic.IsCraftRecipeUnlocked(key);
                            TooltipFactory.BuildTech(key, locked, out tooltipText, tooltipIcons);
                            return;
                        }
                    }
                }
            }
            tooltipText = null;
        }

        void uGUI_IIconManager.OnPointerEnter(uGUI_ItemIcon icon)
        {
        }

        void uGUI_IIconManager.OnPointerExit(uGUI_ItemIcon icon)
        {
        }

        bool uGUI_IIconManager.OnPointerClick(uGUI_ItemIcon icon, int button)
        {
            return false;
        }

        bool uGUI_IIconManager.OnBeginDrag(uGUI_ItemIcon icon)
        {
            return false;
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

        private uGUI_BlueprintEntry GetFirstEntry()
        {
            List<TechCategory> list = new List<TechCategory>();
            List<TechType> list2 = new List<TechType>();
            int i = 0;
            for (int count = groups.Count; i < count; i++)
            {
                TechGroup group = groups[i];
                CraftData.GetBuilderCategories(group, list);
                int j = 0;
                for (int count2 = list.Count; j < count2; j++)
                {
                    TechCategory techCategory = list[j];
                    if (!entries.TryGetValue(techCategory, out var value))
                    {
                        continue;
                    }
                    Dictionary<TechType, uGUI_BlueprintEntry> dictionary = value.entries;
                    CraftData.GetBuilderTech(group, techCategory, list2);
                    int k = 0;
                    for (int count3 = list2.Count; k < count3; k++)
                    {
                        TechType key = list2[k];
                        if (dictionary.TryGetValue(key, out var value2))
                        {
                            return value2;
                        }
                    }
                }
            }
            return null;
        }

        private CategoryEntry GetCategoryEntry(uGUI_BlueprintEntry blueprintEntry)
        {
            Dictionary<TechCategory, CategoryEntry>.Enumerator enumerator = entries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                CategoryEntry value = enumerator.Current.Value;
                Dictionary<TechType, uGUI_BlueprintEntry>.Enumerator enumerator2 = value.entries.GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    if (enumerator2.Current.Value == blueprintEntry)
                    {
                        return value;
                    }
                }
            }
            return null;
        }

        object uGUI_INavigableIconGrid.GetSelectedItem()
        {
            return selectedEntry;
        }

        Graphic uGUI_INavigableIconGrid.GetSelectedIcon()
        {
            if (!(selectedEntry != null))
            {
                return null;
            }
            return selectedEntry.icon;
        }

        void uGUI_INavigableIconGrid.SelectItem(object item)
        {
            ((uGUI_INavigableIconGrid)this).DeselectItem();
            UISelection.selected = item as ISelectable;
            if (!(selectedEntry == null))
            {
                uGUI_Tooltip.Set(selectedEntry.icon);
                bool xRight = true;
                bool yUp = false;
                CategoryEntry categoryEntry = GetCategoryEntry(selectedEntry);
                if (categoryEntry != null)
                {
                    scrollRect.ScrollTo(categoryEntry.title, xRight, yUp, Vector4.zero);
                }
                scrollRect.ScrollTo(UISelection.selected.GetRect(), xRight, yUp, new Vector4(10f, 10f, 10f, 10f));
            }
        }

        void uGUI_INavigableIconGrid.DeselectItem()
        {
            if (!(selectedEntry == null))
            {
                UISelection.selected = null;
                uGUI_Tooltip.Clear();
            }
        }

        bool uGUI_INavigableIconGrid.SelectFirstItem()
        {
            uGUI_BlueprintEntry uGUI_BlueprintEntry2 = null;
            RectTransform viewport = scrollRect.viewport;
            Rect rect = viewport.rect;
            float yMin = rect.yMin;
            float yMax = rect.yMax;
            Vector2 vector = new Vector2(float.MaxValue, float.MinValue);
            Dictionary<TechCategory, CategoryEntry>.Enumerator enumerator = entries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Dictionary<TechType, uGUI_BlueprintEntry>.Enumerator enumerator2 = enumerator.Current.Value.entries.GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    uGUI_BlueprintEntry value = enumerator2.Current.Value;
                    RectTransform rectTransform = value.rectTransform;
                    Vector3 vector2 = viewport.InverseTransformPoint(rectTransform.TransformPoint(rectTransform.rect.center));
                    if (vector2.y >= yMin && vector2.y <= yMax)
                    {
                        bool num = vector2.x < vector.x && vector2.y > vector.y - 5f;
                        bool flag = vector2.y > vector.y && vector2.x < vector.x + 5f;
                        if (num || flag)
                        {
                            vector = vector2;
                            uGUI_BlueprintEntry2 = value;
                        }
                    }
                }
            }
            if (uGUI_BlueprintEntry2 == null)
            {
                uGUI_BlueprintEntry2 = GetFirstEntry();
            }
            if (uGUI_BlueprintEntry2 != null)
            {
                ((uGUI_INavigableIconGrid)this).SelectItem((object)uGUI_BlueprintEntry2);
                return true;
            }
            return false;
        }

        bool uGUI_INavigableIconGrid.SelectItemClosestToPosition(Vector3 worldPos)
        {
            return false;
        }

        bool uGUI_INavigableIconGrid.SelectItemInDirection(int dirX, int dirY)
        {
            if (selectedEntry == null)
            {
                return ((uGUI_INavigableIconGrid)this).SelectFirstItem();
            }
            if (dirX == 0 && dirY == 0)
            {
                return false;
            }
            if (scrollChangedPosition)
            {
                scrollChangedPosition = false;
                RectTransform viewport = scrollRect.viewport;
                RectTransform rectTransform = selectedEntry.rectTransform;
                float y = viewport.InverseTransformPoint(rectTransform.TransformPoint(rectTransform.rect.center)).y;
                Rect rect = viewport.rect;
                if (y < rect.yMin || y > rect.yMax)
                {
                    return ((uGUI_INavigableIconGrid)this).SelectFirstItem();
                }
            }
            UISelection.sSelectables.Clear();
            Dictionary<TechCategory, CategoryEntry>.Enumerator enumerator = entries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Dictionary<TechType, uGUI_BlueprintEntry>.Enumerator enumerator2 = enumerator.Current.Value.entries.GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    uGUI_BlueprintEntry value = enumerator2.Current.Value;
                    if (!(value == null))
                    {
                        UISelection.sSelectables.Add(value);
                    }
                }
            }
            ISelectable selectable = UISelection.FindSelectable(canvas, new Vector2(dirX, -dirY), UISelection.selected, UISelection.sSelectables, fromEdge: false);
            UISelection.sSelectables.Clear();
            uGUI_BlueprintEntry uGUI_BlueprintEntry2 = selectable as uGUI_BlueprintEntry;
            if (uGUI_BlueprintEntry2 != null)
            {
                ((uGUI_INavigableIconGrid)this).SelectItem((object)uGUI_BlueprintEntry2);
                return true;
            }
            return false;
        }

        uGUI_INavigableIconGrid uGUI_INavigableIconGrid.GetNavigableGridInDirection(int dirX, int dirY)
        {
            return null;
        }

        public string CompileTimeCheck()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (prefabTitle.GetComponent<RectTransform>() == null)
            {
                stringBuilder.AppendFormat("RectTransform component is expected on {0} prefab assigned to prefabTitle field\n", prefabTitle.name);
            }
            if (prefabTitle.GetComponentInChildren<Text>() == null)
            {
                stringBuilder.AppendFormat("Text component is expected on {0} prefab assigned to prefabTitle field\n", prefabTitle.name);
            }
            if (prefabEntry.GetComponent<uGUI_BlueprintEntry>() == null)
            {
                stringBuilder.AppendFormat("uGUI_BlueprintEntry component is expected on {0} prefab assigned to prefabEntry field\n", prefabEntry.name);
            }
            if (stringBuilder.Length != 0)
            {
                return stringBuilder.ToString();
            }
            return null;
        }

        bool uGUI_IScrollReceiver.OnScroll(float scrollDelta, float speedMultiplier)
        {
            uGUI_Tooltip.Clear();
            scrollChangedPosition = true;
            scrollRect.Scroll(scrollDelta, speedMultiplier);
            return true;
        }
    }
}
