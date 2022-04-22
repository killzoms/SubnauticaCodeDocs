using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_PingTab : uGUI_PDATab, uGUI_INavigableIconGrid
    {
        private struct SortingHelper
        {
            public int id;

            public int pingType;

            public SortingHelper(int id, int pingType)
            {
                this.id = id;
                this.pingType = pingType;
            }
        }

        private const string pingManagerLabelKey = "PingManagerLabel";

        [AssertNotNull]
        public Text pingManagerLabel;

        [AssertNotNull]
        public Toggle visibilityToggle;

        [AssertNotNull]
        public Image visibilityToggleIndicator;

        [AssertNotNull]
        public GameObject content;

        [AssertNotNull]
        public RectTransform pingCanvas;

        [AssertNotNull]
        public GameObject prefabEntry;

        [AssertNotNull]
        public Sprite spriteShowAll;

        [AssertNotNull]
        public Sprite spriteHideAll;

        private bool _isDirty = true;

        private Dictionary<int, uGUI_PingEntry> entries = new Dictionary<int, uGUI_PingEntry>();

        private List<int> tempKeys = new List<int>();

        private List<SortingHelper> tempSort = new List<SortingHelper>();

        private const int poolChunkSize = 4;

        private List<uGUI_PingEntry> pool = new List<uGUI_PingEntry>();

        private ISelectable selectableVisibilityToggle;

        protected override void Awake()
        {
            base.Awake();
            selectableVisibilityToggle = new SelectableWrapper(visibilityToggle, delegate(GameInput.Button button)
            {
                if (button == GameInput.Button.LeftHand)
                {
                    visibilityToggle.isOn = !visibilityToggle.isOn;
                    return true;
                }
                return false;
            });
        }

        private void OnEnable()
        {
            PingManager.onAdd = (PingManager.OnAdd)Delegate.Combine(PingManager.onAdd, new PingManager.OnAdd(OnAdd));
            PingManager.onRemove = (PingManager.OnRemove)Delegate.Combine(PingManager.onRemove, new PingManager.OnRemove(OnRemove));
            PingManager.onRename = (PingManager.OnRename)Delegate.Combine(PingManager.onRename, new PingManager.OnRename(OnRename));
        }

        private void LateUpdate()
        {
            UpdateEntries();
        }

        private void OnDestroy()
        {
            PingManager.onAdd = (PingManager.OnAdd)Delegate.Remove(PingManager.onAdd, new PingManager.OnAdd(OnAdd));
            PingManager.onRemove = (PingManager.OnRemove)Delegate.Remove(PingManager.onRemove, new PingManager.OnRemove(OnRemove));
            PingManager.onRename = (PingManager.OnRename)Delegate.Remove(PingManager.onRename, new PingManager.OnRename(OnRename));
        }

        public override void Open()
        {
            SetState(state: true);
        }

        public override void Close()
        {
            SetState(state: false);
        }

        private void SetState(bool state)
        {
            content.SetActive(state);
        }

        public override uGUI_INavigableIconGrid GetInitialGrid()
        {
            return this;
        }

        public override void OnLanguageChanged()
        {
            pingManagerLabel.text = Language.main.Get("PingManagerLabel");
            Dictionary<int, uGUI_PingEntry>.Enumerator enumerator = entries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<int, uGUI_PingEntry> current = enumerator.Current;
                PingInstance pingInstance = PingManager.Get(current.Key);
                if (pingInstance != null)
                {
                    current.Value.UpdateLabel(pingInstance.pingType, pingInstance.GetLabel());
                }
            }
        }

        public void SetEntriesVisibility(bool visible)
        {
            visibilityToggleIndicator.sprite = (visible ? spriteHideAll : spriteShowAll);
            Dictionary<int, uGUI_PingEntry>.Enumerator enumerator = entries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                enumerator.Current.Value.visibility.isOn = visible;
            }
        }

        private void OnAdd(int id, PingInstance instance)
        {
            if (instance.displayPingInManager)
            {
                _isDirty = true;
            }
        }

        private void OnRemove(int id)
        {
            if (entries.ContainsKey(id))
            {
                _isDirty = true;
            }
        }

        private void OnRename(int id, PingInstance instance)
        {
            if (instance.displayPingInManager && entries.TryGetValue(id, out var value))
            {
                value.UpdateLabel(instance.pingType, instance.GetLabel());
            }
        }

        private void UpdateEntries()
        {
            if (!_isDirty)
            {
                return;
            }
            _isDirty = false;
            tempKeys.Clear();
            tempKeys.AddRange(entries.Keys);
            for (int num = tempKeys.Count - 1; num >= 0; num--)
            {
                int num2 = tempKeys[num];
                if (PingManager.Get(num2) == null)
                {
                    uGUI_PingEntry entry = entries[num2];
                    entries.Remove(num2);
                    ReleaseEntry(entry);
                }
            }
            Dictionary<int, PingInstance>.Enumerator enumerator = PingManager.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<int, PingInstance> current = enumerator.Current;
                int key = current.Key;
                PingInstance value = current.Value;
                if (!entries.ContainsKey(key) && value != null && value.displayPingInManager)
                {
                    uGUI_PingEntry entry2 = GetEntry();
                    entry2.Initialize(key, value.visible, value.pingType, value.GetLabel(), value.colorIndex);
                    entries.Add(key, entry2);
                }
            }
            tempSort.Clear();
            if (tempSort.Capacity < entries.Count)
            {
                tempSort.Capacity = entries.Count;
            }
            Dictionary<int, uGUI_PingEntry>.Enumerator enumerator2 = entries.GetEnumerator();
            while (enumerator2.MoveNext())
            {
                int key2 = enumerator2.Current.Key;
                PingInstance pingInstance = PingManager.Get(key2);
                if (pingInstance != null)
                {
                    tempSort.Add(new SortingHelper(key2, (int)pingInstance.pingType));
                }
            }
            tempSort.Sort((SortingHelper x, SortingHelper y) => x.pingType.CompareTo(y.pingType));
            int i = 0;
            for (int count = tempSort.Count; i < count; i++)
            {
                int id = tempSort[i].id;
                entries[id].rectTransform.SetSiblingIndex(i);
            }
        }

        private uGUI_PingEntry GetEntry()
        {
            uGUI_PingEntry component;
            if (pool.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    component = global::UnityEngine.Object.Instantiate(prefabEntry).GetComponent<uGUI_PingEntry>();
                    component.rectTransform.SetParent(pingCanvas, worldPositionStays: false);
                    component.Uninitialize();
                    pool.Add(component);
                }
            }
            int index = pool.Count - 1;
            component = pool[index];
            pool.RemoveAt(index);
            return component;
        }

        private void ReleaseEntry(uGUI_PingEntry entry)
        {
            if (!(entry == null))
            {
                entry.Uninitialize();
                pool.Add(entry);
            }
        }

        object uGUI_INavigableIconGrid.GetSelectedItem()
        {
            return UISelection.selected;
        }

        Graphic uGUI_INavigableIconGrid.GetSelectedIcon()
        {
            ISelectable selected = UISelection.selected;
            if (selected != null)
            {
                RectTransform rect = selected.GetRect();
                if (rect != null)
                {
                    return rect.GetComponent<Graphic>();
                }
            }
            return null;
        }

        void uGUI_INavigableIconGrid.SelectItem(object item)
        {
            ((uGUI_INavigableIconGrid)this).DeselectItem();
            ISelectable selectable = item as ISelectable;
            if (selectable == null)
            {
                return;
            }
            UISelection.selected = selectable;
            RectTransform rect = selectable.GetRect();
            if (rect == null)
            {
                return;
            }
            uGUI_PingEntry componentInParent = rect.GetComponentInParent<uGUI_PingEntry>();
            if (!(componentInParent == null))
            {
                ScrollRect componentInParent2 = pingCanvas.GetComponentInParent<ScrollRect>();
                if (componentInParent2 != null)
                {
                    componentInParent2.ScrollTo(componentInParent.rectTransform, xRight: true, yUp: false, Vector4.zero);
                }
            }
        }

        void uGUI_INavigableIconGrid.DeselectItem()
        {
            if (UISelection.selected != null)
            {
                UISelection.selected = null;
                uGUI_Tooltip.Clear();
            }
        }

        bool uGUI_INavigableIconGrid.SelectFirstItem()
        {
            ((uGUI_INavigableIconGrid)this).SelectItem((object)selectableVisibilityToggle);
            return true;
        }

        bool uGUI_INavigableIconGrid.SelectItemClosestToPosition(Vector3 worldPos)
        {
            return false;
        }

        bool uGUI_INavigableIconGrid.SelectItemInDirection(int dirX, int dirY)
        {
            if (UISelection.selected == null)
            {
                return ((uGUI_INavigableIconGrid)this).SelectFirstItem();
            }
            if (dirX == 0 && dirY == 0)
            {
                return false;
            }
            UISelection.sSelectables.Clear();
            UISelection.sSelectables.Add(selectableVisibilityToggle);
            Dictionary<int, uGUI_PingEntry>.Enumerator enumerator = entries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                enumerator.Current.Value.GetSelectables(UISelection.sSelectables);
            }
            ISelectable selectable = UISelection.FindSelectable(pingCanvas, new Vector2(dirX, -dirY), UISelection.selected, UISelection.sSelectables, fromEdge: false);
            UISelection.sSelectables.Clear();
            if (selectable != null)
            {
                ((uGUI_INavigableIconGrid)this).SelectItem((object)selectable);
                return true;
            }
            return false;
        }

        uGUI_INavigableIconGrid uGUI_INavigableIconGrid.GetNavigableGridInDirection(int dirX, int dirY)
        {
            return null;
        }
    }
}
