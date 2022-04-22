namespace AssemblyCSharp.Exploder.MeshCutter
{
    internal class ArrayDictionary<T>
    {
        private struct DicItem
        {
            public T data;

            public bool valid;
        }

        public int Count;

        public int Size;

        private readonly DicItem[] dictionary;

        public T this[int key]
        {
            get
            {
                return dictionary[key].data;
            }
            set
            {
                dictionary[key].data = value;
            }
        }

        public ArrayDictionary(int size)
        {
            dictionary = new DicItem[size];
            Size = size;
        }

        public bool ContainsKey(int key)
        {
            if (key < Size)
            {
                return dictionary[key].valid;
            }
            return false;
        }

        public void Clear()
        {
            for (int i = 0; i < Size; i++)
            {
                dictionary[i].data = default(T);
                dictionary[i].valid = false;
            }
            Count = 0;
        }

        public void Add(int key, T data)
        {
            dictionary[key].valid = true;
            dictionary[key].data = data;
            Count++;
        }

        public void Remove(int key)
        {
            dictionary[key].valid = false;
            Count--;
        }

        public T[] ToArray()
        {
            T[] array = new T[Count];
            int num = 0;
            for (int i = 0; i < Size; i++)
            {
                if (dictionary[i].valid)
                {
                    array[num++] = dictionary[i].data;
                    if (num == Count)
                    {
                        return array;
                    }
                }
            }
            return null;
        }

        public bool TryGetValue(int key, out T value)
        {
            DicItem dicItem = dictionary[key];
            if (dicItem.valid)
            {
                value = dicItem.data;
                return true;
            }
            value = default(T);
            return false;
        }

        public T GetFirstValue()
        {
            for (int i = 0; i < Size; i++)
            {
                DicItem dicItem = dictionary[i];
                if (dicItem.valid)
                {
                    return dicItem.data;
                }
            }
            return default(T);
        }
    }
}
