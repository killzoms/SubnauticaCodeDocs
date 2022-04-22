using System.Collections.Generic;
using System.Threading;

namespace UWE
{
    public sealed class BoundedObjectPool<T> where T : new()
    {
        private readonly T[] items;

        private int length;

        public BoundedObjectPool(int length)
        {
            this.length = length;
            items = new T[length];
            for (int i = 0; i < length; i++)
            {
                items[i] = new T();
            }
        }

        public T Get()
        {
            lock (items)
            {
                while (length < 1)
                {
                    Monitor.Wait(items);
                }
                return items[--length];
            }
        }

        public void Return(T item)
        {
            lock (items)
            {
                if (length < items.Length)
                {
                    items[length++] = item;
                    Monitor.Pulse(items);
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)items).GetEnumerator();
        }
    }
}
