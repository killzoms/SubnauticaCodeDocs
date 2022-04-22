using System;
using System.Collections.Generic;
using Gendarme;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class CraftingAnalytics : MonoBehaviour, IProtoEventListener
    {
        [ProtoContract]
        public struct EntryData
        {
            [NonSerialized]
            [ProtoMember(1)]
            public int timeScanFirst;

            [NonSerialized]
            [ProtoMember(2)]
            public int timeScanLast;

            [NonSerialized]
            [ProtoMember(3)]
            public int craftCount;
        }

        private const ManagedUpdate.Queue updateQueue = ManagedUpdate.Queue.LateUpdateAfterInput;

        private static CraftingAnalytics _main;

        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int _serializedVersion = 1;

        [NonSerialized]
        [ProtoMember(2)]
        public bool _active;

        [NonSerialized]
        [ProtoMember(3)]
        public readonly Dictionary<TechType, EntryData> entries = new Dictionary<TechType, EntryData>(TechTypeExtensions.sTechTypeComparer);

        private bool _lastActive;

        private List<TechType> toUpdate = new List<TechType>();

        public static CraftingAnalytics main => _main;

        public bool active
        {
            get
            {
                return _active;
            }
            private set
            {
                _active = value;
                Initialize();
            }
        }

        [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
        private void Awake()
        {
            if (_main != null)
            {
                Debug.LogErrorFormat("Multiple {0} in scene!", GetType().ToString());
                global::UnityEngine.Object.Destroy(this);
            }
            else
            {
                _main = this;
                _active |= !Utils.GetContinueMode();
            }
        }

        private void OnEnable()
        {
            ManagedUpdate.Subscribe(ManagedUpdate.Queue.LateUpdateAfterInput, OnUpdate);
        }

        private void OnDisable()
        {
            ManagedUpdate.Subscribe(ManagedUpdate.Queue.LateUpdateAfterInput, OnUpdate);
        }

        private void OnUpdate()
        {
            for (int num = toUpdate.Count - 1; num >= 0; num--)
            {
                TechType techType = toUpdate[num];
                toUpdate.RemoveAt(num);
                UpdateUnlock(techType);
            }
        }

        private void OnLockedChange(PDAScanner.Entry entry)
        {
            if (active)
            {
                TechType blueprint = PDAScanner.GetEntryData(entry.techType).blueprint;
                if (!toUpdate.Contains(blueprint))
                {
                    toUpdate.Add(blueprint);
                }
            }
        }

        private void OnCompoundAdd(TechType techType, int unlocked, int total)
        {
            if (active && !toUpdate.Contains(techType))
            {
                toUpdate.Add(techType);
            }
        }

        private void OnCompoundRemove(TechType techType)
        {
            if (active && !toUpdate.Contains(techType))
            {
                toUpdate.Add(techType);
            }
        }

        public void OnConstruct(TechType techType, Vector3 position)
        {
            GameAnalytics.LegacyEvent(GameAnalytics.Event.LegacyConstruct, techType.AsString());
            UpdateCreate(GameAnalytics.Event.TechConstructed, techType, position);
        }

        public void OnCraft(TechType techType, Vector3 position)
        {
            UpdateCreate(GameAnalytics.Event.TechCrafted, techType, position);
        }

        [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
        private void Initialize()
        {
            if (_lastActive != active)
            {
                _lastActive = active;
                if (active)
                {
                    PDAScanner.onAdd = (PDAScanner.OnEntryEvent)Delegate.Combine(PDAScanner.onAdd, new PDAScanner.OnEntryEvent(OnLockedChange));
                    PDAScanner.onRemove = (PDAScanner.OnEntryEvent)Delegate.Combine(PDAScanner.onRemove, new PDAScanner.OnEntryEvent(OnLockedChange));
                    KnownTech.onCompoundAdd += OnCompoundAdd;
                    KnownTech.onCompoundRemove += OnCompoundRemove;
                }
                else
                {
                    toUpdate.Clear();
                    PDAScanner.onAdd = (PDAScanner.OnEntryEvent)Delegate.Remove(PDAScanner.onAdd, new PDAScanner.OnEntryEvent(OnLockedChange));
                    PDAScanner.onRemove = (PDAScanner.OnEntryEvent)Delegate.Remove(PDAScanner.onRemove, new PDAScanner.OnEntryEvent(OnLockedChange));
                    KnownTech.onCompoundAdd -= OnCompoundAdd;
                    KnownTech.onCompoundRemove -= OnCompoundRemove;
                }
            }
        }

        private EntryData EnsureEntry(TechType techType)
        {
            if (!entries.TryGetValue(techType, out var value))
            {
                EntryData entryData = default(EntryData);
                entryData.timeScanFirst = -1;
                entryData.timeScanLast = -1;
                entryData.craftCount = 0;
                value = entryData;
                entries.Add(techType, value);
            }
            return value;
        }

        private void UpdateUnlock(TechType techType)
        {
            if (!active)
            {
                return;
            }
            int num = (int)DayNightCycle.main.timePassed;
            EntryData value = EnsureEntry(techType);
            int unlocked;
            int total;
            TechUnlockState techUnlockState = KnownTech.GetTechUnlockState(techType, out unlocked, out total);
            if (value.timeScanFirst < 0)
            {
                value.timeScanFirst = num;
            }
            if (techUnlockState == TechUnlockState.Available && value.timeScanLast < 0)
            {
                value.timeScanLast = num;
                using GameAnalytics.EventData eventData = GameAnalytics.CustomEvent(GameAnalytics.Event.BlueprintUnlocked);
                eventData.Add("tech_type", (int)techType);
                eventData.Add("time", num - value.timeScanFirst);
            }
            entries[techType] = value;
        }

        private void UpdateCreate(GameAnalytics.Event eventId, TechType techType, Vector3 position)
        {
            if (!active)
            {
                return;
            }
            int num = (int)DayNightCycle.main.timePassed;
            EntryData value = EnsureEntry(techType);
            value.craftCount++;
            if (value.craftCount == 1 && value.timeScanLast > 0)
            {
                using GameAnalytics.EventData eventData = GameAnalytics.CustomEvent(GameAnalytics.Event.FirstUnlockedCreate);
                eventData.Add("tech_type", (int)techType);
                eventData.Add("time", num - value.timeScanLast);
            }
            using (GameAnalytics.EventData eventData2 = GameAnalytics.CustomEvent(eventId))
            {
                eventData2.Add("tech_type", (int)techType);
                eventData2.AddPosition(position);
            }
            entries[techType] = value;
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            Initialize();
        }
    }
}
