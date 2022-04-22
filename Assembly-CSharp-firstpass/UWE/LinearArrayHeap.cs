using System;
using UnityEngine;

namespace UWE
{
    public class LinearArrayHeap<T> : IEstimateBytes
    {
        public class Alloc
        {
            public readonly LinearArrayHeap<T> Heap;

            public readonly int Offset;

            public readonly int Length;

            public readonly T[] Array;

            public T this[int Index]
            {
                get
                {
                    return Get(Index);
                }
                set
                {
                    Set(Index, value);
                }
            }

            public Alloc(LinearArrayHeap<T> heap, int offset, int length, T[] array)
            {
                Heap = heap;
                Offset = offset;
                Length = length;
                Array = array;
            }

            public void Set(int i, T value)
            {
                Array[Offset + i] = value;
            }

            public T Get(int i)
            {
                return Array[Offset + i];
            }

            public void Write(int startOffset, T[] values)
            {
                System.Array.Copy(values, 0, Array, startOffset + Offset, values.Length);
            }
        }

        private T[] buffer;

        private int offset;

        public int Highwater { get; private set; }

        public int Outstanding { get; private set; }

        public int PeakOutstanding { get; private set; }

        public int ElementSize { get; private set; }

        public int MaxSize => buffer.Length;

        public LinearArrayHeap(int elementSize, int maxSize)
        {
            buffer = new T[maxSize];
            offset = 0;
            ElementSize = elementSize;
            Outstanding = 0;
        }

        public virtual Alloc Allocate(int size)
        {
            if (offset + size > buffer.Length)
            {
                Debug.LogWarningFormat("LinearArrayHeap: Attempting to allocate array of size {0}. Max size: {1}. Element Size: {2}. Doubling heap to fix, but this is SLOW!", size, MaxSize, ElementSize);
                int newSize = Mathf.Max(buffer.Length * 2, size);
                Array.Resize(ref buffer, newSize);
            }
            Alloc result = new Alloc(this, offset, size, buffer);
            offset += size;
            Outstanding++;
            PeakOutstanding = Mathf.Max(PeakOutstanding, Outstanding);
            Highwater = Mathf.Max(Highwater, offset);
            return result;
        }

        public virtual void Free(Alloc a)
        {
            Outstanding--;
        }

        public virtual void Reset()
        {
            offset = 0;
        }

        public long EstimateBytes()
        {
            return ElementSize * MaxSize;
        }
    }
}
