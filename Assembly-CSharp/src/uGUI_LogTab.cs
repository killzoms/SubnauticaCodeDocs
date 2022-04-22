using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_LogTab : uGUI_PDATab, ICompileTimeCheckable, uGUI_INavigableIconGrid, uGUI_IScrollReceiver
    {
        public enum SortBy
        {
            DateDescending,
            DateAscending,
            Topic
        }

        private CachedEnumString<PDALog.EntryType> sEntryTypeStrings = new CachedEnumString<PDALog.EntryType>(PDALog.sEntryTypeComparer);

        private const string logLabelKey = "LogLabel";

        [AssertNotNull]
        public GameObject content;

        [AssertNotNull]
        public Text logLabel;

        [AssertNotNull]
        public GameObject prefabEntry;

        [AssertNotNull]
        public GameObject prefabGroupLabel;

        [AssertNotNull]
        public ScrollRect scrollRect;

        [AssertNotNull]
        public RectTransform logCanvas;

        private Comparison<int> groupComparerDateDescending = (int x, int y) => y.CompareTo(x);

        private Comparison<int> groupComparerDateAscending = (int x, int y) => x.CompareTo(y);

        private Comparison<int> groupComparerTopic = (int x, int y) => x.CompareTo(y);

        private Comparison<PDALog.Entry> entryComparerDateDescending = (PDALog.Entry x, PDALog.Entry y) => y.timestamp.CompareTo(x.timestamp);

        private Comparison<PDALog.Entry> entryComparerDateAscending = (PDALog.Entry x, PDALog.Entry y) => x.timestamp.CompareTo(y.timestamp);

        private Comparison<PDALog.Entry> entryComparerTopic = (PDALog.Entry x, PDALog.Entry y) => y.timestamp.CompareTo(x.timestamp);

        private SortBy sortBy;

        private bool _isDirty = true;

        private Dictionary<PDALog.Entry, uGUI_LogEntry> entries = new Dictionary<PDALog.Entry, uGUI_LogEntry>();

        private Dictionary<int, Text> groupLabels = new Dictionary<int, Text>();

        private List<PDALog.Entry> tempKeys = new List<PDALog.Entry>();

        private Dictionary<int, List<PDALog.Entry>> tempSort = new Dictionary<int, List<PDALog.Entry>>();

        private List<int> tempGroups = new List<int>();

        private const int poolChunkSize = 4;

        private List<uGUI_LogEntry> poolEntries = new List<uGUI_LogEntry>();

        private List<Text> poolGroups = new List<Text>();

        private bool scrollChangedPosition;

        private Func<int, int, bool> comparerLess = (int x, int y) => x < y;

        private Func<int, int, bool> comparerGreater = (int x, int y) => x > y;

        public override int notificationsCount => NotificationManager.main.GetCount(NotificationManager.Group.Log);

        private uGUI_LogEntry selectedEntry
        {
            get
            {
                if (UISelection.selected != null && UISelection.selected.IsValid())
                {
                    return UISelection.selected as uGUI_LogEntry;
                }
                return null;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            PDALog.onAdd = (PDALog.OnAdd)Delegate.Combine(PDALog.onAdd, new PDALog.OnAdd(OnAddEntry));
        }

        private void LateUpdate()
        {
            UpdateEntries();
        }

        private void OnDestroy()
        {
            PDALog.onAdd = (PDALog.OnAdd)Delegate.Remove(PDALog.onAdd, new PDALog.OnAdd(OnAddEntry));
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
            logLabel.text = Language.main.Get("LogLabel");
            Dictionary<int, Text>.Enumerator enumerator = groupLabels.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<int, Text> current = enumerator.Current;
                int key = current.Key;
                current.Value.text = GetGroupText(key);
            }
            Dictionary<PDALog.Entry, uGUI_LogEntry>.Enumerator enumerator2 = entries.GetEnumerator();
            while (enumerator2.MoveNext())
            {
                enumerator2.Current.Value.UpdateText();
            }
        }

        private void SetState(bool state)
        {
            content.SetActive(state);
        }

        private void OnAddEntry(PDALog.Entry entry)
        {
            _isDirty = true;
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
                PDALog.Entry entry = tempKeys[num];
                if (!PDALog.Contains(entry.data.key))
                {
                    uGUI_LogEntry uGUI_LogEntry2 = entries[entry];
                    entries.Remove(entry);
                    ReleaseEntry(uGUI_LogEntry2);
                    NotificationManager.main.UnregisterTarget(uGUI_LogEntry2);
                }
            }
            Dictionary<string, PDALog.Entry>.Enumerator enumerator = PDALog.GetEntries();
            while (enumerator.MoveNext())
            {
                KeyValuePair<string, PDALog.Entry> current = enumerator.Current;
                string key = current.Key;
                PDALog.Entry value = current.Value;
                if (!entries.ContainsKey(value))
                {
                    uGUI_LogEntry entry2 = GetEntry();
                    entry2.Initialize(value);
                    entries.Add(value, entry2);
                    PDALog.EntryData entryData = value.data;
                    if (entryData == null)
                    {
                        entryData = new PDALog.EntryData();
                        entryData.key = key;
                        entryData.type = PDALog.EntryType.Invalid;
                        value.data = entryData;
                    }
                    NotificationManager.main.RegisterTarget(NotificationManager.Group.Log, entryData.key, entry2);
                }
            }
            tempKeys.Clear();
            SortEntries();
        }

        private void SortEntries()
        {
            tempSort.Clear();
            Dictionary<PDALog.Entry, uGUI_LogEntry>.Enumerator enumerator = entries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                PDALog.Entry key = enumerator.Current.Key;
                int entryCriteria = GetEntryCriteria(key);
                if (!tempSort.TryGetValue(entryCriteria, out var value))
                {
                    value = new List<PDALog.Entry>();
                    tempSort.Add(entryCriteria, value);
                }
                value.Add(key);
            }
            tempGroups.Clear();
            tempGroups.AddRange(groupLabels.Keys);
            int i = 0;
            for (int count = tempGroups.Count; i < count; i++)
            {
                int key2 = tempGroups[i];
                if (!tempSort.ContainsKey(key2))
                {
                    ReleaseGroupLabel(groupLabels[key2]);
                }
            }
            tempGroups.Clear();
            tempGroups.AddRange(tempSort.Keys);
            Comparison<int> comparison = sortBy switch
            {
                SortBy.DateAscending => groupComparerDateAscending, 
                SortBy.Topic => groupComparerTopic, 
                _ => groupComparerDateDescending, 
            };
            tempGroups.Sort(comparison);
            int num = 0;
            Comparison<PDALog.Entry> comparison2 = sortBy switch
            {
                SortBy.DateAscending => entryComparerDateAscending, 
                SortBy.Topic => entryComparerTopic, 
                _ => entryComparerDateDescending, 
            };
            int j = 0;
            for (int count2 = tempGroups.Count; j < count2; j++)
            {
                int num2 = tempGroups[j];
                if (!groupLabels.TryGetValue(num2, out var value2))
                {
                    value2 = GetGroupLabel();
                    value2.gameObject.SetActive(value: true);
                    value2.text = GetGroupText(num2);
                    groupLabels.Add(num2, value2);
                }
                value2.rectTransform.SetSiblingIndex(num);
                num++;
                List<PDALog.Entry> list = tempSort[num2];
                list.Sort(comparison2);
                int k = 0;
                for (int count3 = list.Count; k < count3; k++)
                {
                    PDALog.Entry key3 = list[k];
                    entries[key3].rectTransform.SetSiblingIndex(num);
                    num++;
                }
            }
            tempGroups.Clear();
            tempSort.Clear();
        }

        private void InitializeGroupLabel(Text groupLabel)
        {
            groupLabel.gameObject.SetActive(value: true);
        }

        private void UninitializeGroupLabel(Text groupLabel)
        {
            groupLabel.text = string.Empty;
            groupLabel.gameObject.SetActive(value: false);
        }

        private string GetGroupText(int index)
        {
            switch (sortBy)
            {
                case SortBy.DateDescending:
                case SortBy.DateAscending:
                    return string.Format("{0} {1}", Language.main.Get("Day"), index);
                case SortBy.Topic:
                    return sEntryTypeStrings.Get((PDALog.EntryType)index);
                default:
                    return IntStringCache.GetStringForInt(index);
            }
        }

        private int GetEntryCriteria(PDALog.Entry entry)
        {
            int result = 0;
            switch (sortBy)
            {
                case SortBy.DateDescending:
                case SortBy.DateAscending:
                    result = DayNightCycle.ToGameDays(entry.timestamp) + 1;
                    break;
                case SortBy.Topic:
                    result = (int)entry.data.type;
                    break;
            }
            return result;
        }

        private uGUI_LogEntry GetEntry()
        {
            uGUI_LogEntry component;
            if (poolEntries.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    component = global::UnityEngine.Object.Instantiate(prefabEntry).GetComponent<uGUI_LogEntry>();
                    component.rectTransform.SetParent(logCanvas, worldPositionStays: false);
                    component.Uninitialize();
                    poolEntries.Add(component);
                }
            }
            int index = poolEntries.Count - 1;
            component = poolEntries[index];
            poolEntries.RemoveAt(index);
            return component;
        }

        private void ReleaseEntry(uGUI_LogEntry logEntry)
        {
            if (!(logEntry == null))
            {
                logEntry.Uninitialize();
                poolEntries.Add(logEntry);
            }
        }

        private Text GetGroupLabel()
        {
            Text component;
            if (poolGroups.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    component = global::UnityEngine.Object.Instantiate(prefabGroupLabel).GetComponent<Text>();
                    component.rectTransform.SetParent(logCanvas, worldPositionStays: false);
                    UninitializeGroupLabel(component);
                    poolGroups.Add(component);
                }
            }
            int index = poolGroups.Count - 1;
            component = poolGroups[index];
            poolGroups.RemoveAt(index);
            return component;
        }

        private void ReleaseGroupLabel(Text groupLabel)
        {
            if (!(groupLabel == null))
            {
                UninitializeGroupLabel(groupLabel);
                poolGroups.Add(groupLabel);
            }
        }

        public string CompileTimeCheck()
        {
            if (prefabEntry.GetComponent<uGUI_LogEntry>() == null)
            {
                return "uGUI_LogTab : uGUI_LogEntry component is missing on prefabEntry prefab!";
            }
            if (prefabGroupLabel.GetComponent<Text>() == null)
            {
                return "uGUI_LogTab : Text component is missing on prefabgroupLabel prefab!";
            }
            return null;
        }

        public object GetSelectedItem()
        {
            return selectedEntry;
        }

        public Graphic GetSelectedIcon()
        {
            uGUI_LogEntry uGUI_LogEntry2 = selectedEntry;
            if (!(uGUI_LogEntry2 != null))
            {
                return null;
            }
            return uGUI_LogEntry2.background;
        }

        public void SelectItem(object item)
        {
            uGUI_LogEntry uGUI_LogEntry2 = item as uGUI_LogEntry;
            if (!(uGUI_LogEntry2 == null))
            {
                DeselectItem();
                UISelection.selected = uGUI_LogEntry2;
                scrollRect.ScrollTo(uGUI_LogEntry2.rectTransform, xRight: true, yUp: false, new Vector4(10f, 10f, 10f, 10f));
            }
        }

        public void DeselectItem()
        {
            if (!(selectedEntry == null))
            {
                UISelection.selected = null;
            }
        }

        public bool SelectFirstItem()
        {
            uGUI_LogEntry uGUI_LogEntry2 = null;
            RectTransform viewport = scrollRect.viewport;
            Rect rect = viewport.rect;
            float yMin = rect.yMin;
            float yMax = rect.yMax;
            float num = float.MinValue;
            Dictionary<PDALog.Entry, uGUI_LogEntry>.Enumerator enumerator = entries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                uGUI_LogEntry value = enumerator.Current.Value;
                if (value.isActiveAndEnabled)
                {
                    RectTransform rectTransform = value.rectTransform;
                    float y = viewport.InverseTransformPoint(rectTransform.TransformPoint(rectTransform.rect.center)).y;
                    if (y >= yMin && y <= yMax && y > num)
                    {
                        num = y;
                        uGUI_LogEntry2 = value;
                    }
                }
            }
            if (uGUI_LogEntry2 != null)
            {
                SelectItem(uGUI_LogEntry2);
                return true;
            }
            return false;
        }

        public bool SelectItemClosestToPosition(Vector3 worldPos)
        {
            return false;
        }

        public bool SelectItemInDirection(int dirX, int dirY)
        {
            uGUI_LogEntry uGUI_LogEntry2 = selectedEntry;
            if (uGUI_LogEntry2 == null)
            {
                return SelectFirstItem();
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
            if (dirY == 0)
            {
                return false;
            }
            uGUI_LogEntry uGUI_LogEntry3 = null;
            int siblingIndex = uGUI_LogEntry2.rectTransform.GetSiblingIndex();
            int arg = ((dirY < 0) ? int.MinValue : int.MaxValue);
            Func<int, int, bool> func = ((dirY < 0) ? comparerLess : comparerGreater);
            Dictionary<PDALog.Entry, uGUI_LogEntry>.Enumerator enumerator = entries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                uGUI_LogEntry value = enumerator.Current.Value;
                if (value.isActiveAndEnabled)
                {
                    int siblingIndex2 = value.rectTransform.GetSiblingIndex();
                    if (func(siblingIndex2, siblingIndex) && func(arg, siblingIndex2))
                    {
                        arg = siblingIndex2;
                        uGUI_LogEntry3 = value;
                    }
                }
            }
            if (uGUI_LogEntry3 != null)
            {
                SelectItem(uGUI_LogEntry3);
                return true;
            }
            return false;
        }

        public uGUI_INavigableIconGrid GetNavigableGridInDirection(int dirX, int dirY)
        {
            return null;
        }

        bool uGUI_IScrollReceiver.OnScroll(float scrollDelta, float speedMultiplier)
        {
            scrollChangedPosition = true;
            scrollRect.Scroll(scrollDelta, speedMultiplier);
            return true;
        }
    }
}
