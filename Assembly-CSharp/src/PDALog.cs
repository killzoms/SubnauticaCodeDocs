using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    public static class PDALog
    {
        public delegate void OnAdd(Entry entry);

        public enum EntryType
        {
            Default,
            Invalid
        }

        public class EntryTypeComparer : IEqualityComparer<EntryType>
        {
            public bool Equals(EntryType x, EntryType y)
            {
                int num = (int)x;
                return num.Equals((int)y);
            }

            public int GetHashCode(EntryType obj)
            {
                return (int)obj;
            }
        }

        [Serializable]
        public class EntryData
        {
            public string key = "";

            public EntryType type;

            public Sprite icon;

            public FMODAsset sound;
        }

        [ProtoContract]
        public class Entry
        {
            [ProtoMember(1)]
            public float timestamp;

            public EntryData data;
        }

        public static readonly EntryTypeComparer sEntryTypeComparer = new EntryTypeComparer();

        private static Sprite iconDefault;

        public static OnAdd onAdd;

        private static bool initialized = false;

        private static readonly Dictionary<string, EntryData> mapping = new Dictionary<string, EntryData>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        public static void Initialize(PDAData pdaData)
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            entries.Clear();
            List<EntryData> log = pdaData.log;
            mapping.Clear();
            int i = 0;
            for (int count = log.Count; i < count; i++)
            {
                EntryData entryData = log[i];
                string key = entryData.key;
                if (string.IsNullOrEmpty(key))
                {
                    Debug.LogError("PDALog : Initialize() : Empty key found at index " + i);
                }
                else if (mapping.ContainsKey(key))
                {
                    Debug.LogErrorFormat("PDALog : Initialize() : Duplicate key '{0}' found at index {1}.", key, i);
                }
                else
                {
                    mapping.Add(key, entryData);
                }
            }
            iconDefault = pdaData.defaultLogIcon;
            InitDataForEntries();
        }

        public static void Deinitialize()
        {
            iconDefault = null;
            mapping.Clear();
            entries.Clear();
            onAdd = null;
            initialized = false;
        }

        private static void InitDataForEntries()
        {
            Dictionary<string, Entry>.Enumerator enumerator = entries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<string, Entry> current = enumerator.Current;
                string key = current.Key;
                Entry value = current.Value;
                if (GetEntryData(key, out var entryData))
                {
                    value.data = entryData;
                }
            }
        }

        public static EntryData Add(string key, bool autoPlay = true)
        {
            EntryData entryData = AddEntry(key, autoPlay);
            if (entryData != null && entryData.sound != null && autoPlay)
            {
                PDASounds.queue.PlayQueued(entryData.sound);
            }
            return entryData;
        }

        private static EntryData AddEntry(string key, bool autoPlay)
        {
            if (!entries.ContainsKey(key))
            {
                if (!GetEntryData(key, out var entryData))
                {
                    Debug.LogErrorFormat("PDALog : Add() : EntryData for key='{0}' is not found!", key);
                    entryData = new EntryData();
                    entryData.key = key;
                    entryData.type = EntryType.Invalid;
                }
                Entry entry = new Entry();
                entry.data = entryData;
                entry.timestamp = DayNightCycle.main.timePassedAsFloat;
                entries.Add(entryData.key, entry);
                NotificationManager.main.Add(NotificationManager.Group.Log, entryData.key, 3f);
                if (autoPlay)
                {
                    Subtitles.main.Add(entryData.key);
                }
                NotifyAdd(entry);
                return entryData;
            }
            return null;
        }

        public static bool GetEntryData(string key, out EntryData entryData)
        {
            return mapping.TryGetValue(key, out entryData);
        }

        public static ICollection<string> GetKeys()
        {
            return mapping.Keys;
        }

        public static Dictionary<string, Entry>.Enumerator GetEntries()
        {
            return entries.GetEnumerator();
        }

        public static bool Contains(string key)
        {
            return entries.ContainsKey(key);
        }

        public static Sprite GetIcon(Sprite sprite)
        {
            if (sprite != null)
            {
                return sprite;
            }
            return iconDefault;
        }

        private static void NotifyAdd(Entry entry)
        {
            if (onAdd != null)
            {
                onAdd(entry);
            }
        }

        public static Dictionary<string, Entry> Serialize()
        {
            return entries;
        }

        public static void Deserialize(Dictionary<string, Entry> data)
        {
            if (!initialized)
            {
                Debug.LogError("PDALog : Deserialize() : Deserializing uninitialized PDALog!");
            }
            else if (data != null)
            {
                Dictionary<string, Entry>.Enumerator enumerator = data.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    KeyValuePair<string, Entry> current = enumerator.Current;
                    entries[current.Key] = current.Value;
                }
                InitDataForEntries();
            }
        }
    }
}
