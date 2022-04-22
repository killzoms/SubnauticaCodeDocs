using System.Collections.Generic;
using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    public static class PingManager
    {
        public class PingTypeComparer : IEqualityComparer<PingType>
        {
            public bool Equals(PingType x, PingType y)
            {
                int num = (int)x;
                return num.Equals((int)y);
            }

            public int GetHashCode(PingType obj)
            {
                return (int)obj;
            }
        }

        public delegate void OnAdd(int id, PingInstance instance);

        public delegate void OnRemove(int id);

        public delegate void OnRename(int id, PingInstance instance);

        public delegate void OnColor(int id, Color color);

        public delegate void OnVisible(int id, bool visible);

        public static readonly PingTypeComparer sPingTypeComparer = new PingTypeComparer();

        public static readonly CachedEnumString<PingType> sCachedPingTypeStrings = new CachedEnumString<PingType>(sPingTypeComparer);

        public static readonly CachedEnumString<PingType> sCachedPingTypeTranslationStrings = new CachedEnumString<PingType>("Ping", sPingTypeComparer);

        public static readonly Color[] colorOptions = new Color[5]
        {
            new Color32(73, 190, byte.MaxValue, byte.MaxValue),
            new Color32(byte.MaxValue, 146, 71, byte.MaxValue),
            new Color32(219, 95, 64, byte.MaxValue),
            new Color32(93, 205, 200, byte.MaxValue),
            new Color32(byte.MaxValue, 209, 0, byte.MaxValue)
        };

        [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
        public static OnAdd onAdd;

        [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
        public static OnRemove onRemove;

        [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
        public static OnRename onRename;

        [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
        public static OnColor onColor;

        [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
        public static OnVisible onVisible;

        private static Dictionary<int, PingInstance> pings = new Dictionary<int, PingInstance>();

        public static void Register(PingInstance instance)
        {
            int id = GetId(instance);
            if (id != 0 && !pings.ContainsKey(id))
            {
                pings.Add(id, instance);
                if (onAdd != null)
                {
                    onAdd(id, instance);
                }
            }
        }

        public static void Unregister(PingInstance instance)
        {
            int id = GetId(instance);
            if (id != 0 && pings.Remove(id) && onRemove != null)
            {
                onRemove(id);
            }
        }

        public static void NotifyVisible(PingInstance instance)
        {
            if (onVisible != null)
            {
                int id = GetId(instance);
                if (id != 0)
                {
                    onVisible(id, instance.visible);
                }
            }
        }

        public static void NotifyRename(PingInstance instance)
        {
            if (onRename != null)
            {
                int id = GetId(instance);
                if (id != 0)
                {
                    onRename(id, instance);
                }
            }
        }

        public static void NotifyColor(PingInstance instance)
        {
            if (onColor == null)
            {
                return;
            }
            int id = GetId(instance);
            if (id != 0)
            {
                int num = instance.colorIndex;
                if (num < 0 || num >= colorOptions.Length)
                {
                    num = 0;
                }
                Color color = colorOptions[num];
                onColor(id, color);
            }
        }

        public static void SetVisible(int id, bool visible)
        {
            if (pings.TryGetValue(id, out var value))
            {
                value.SetVisible(visible);
            }
        }

        public static void SetColor(int id, int colorIndex)
        {
            if (pings.TryGetValue(id, out var value))
            {
                value.SetColor(colorIndex);
            }
        }

        public static Dictionary<int, PingInstance>.Enumerator GetEnumerator()
        {
            return pings.GetEnumerator();
        }

        public static PingInstance Get(int id)
        {
            if (pings.TryGetValue(id, out var value))
            {
                return value;
            }
            return null;
        }

        public static void Deinitialize()
        {
            pings.Clear();
        }

        private static int GetId(PingInstance instance)
        {
            if (!(instance != null))
            {
                return 0;
            }
            return instance.GetInstanceID();
        }
    }
}
