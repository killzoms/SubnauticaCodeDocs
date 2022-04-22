namespace UWE
{
    public class ThreadSafeLinearArrayHeap<T> : LinearArrayHeap<T>
    {
        private object lockObject = new object();

        public ThreadSafeLinearArrayHeap(int elementSize, int maxSize)
            : base(elementSize, maxSize)
        {
        }

        public override Alloc Allocate(int size)
        {
            lock (lockObject)
            {
                return base.Allocate(size);
            }
        }

        public override void Free(Alloc a)
        {
            lock (lockObject)
            {
                base.Free(a);
            }
        }

        public override void Reset()
        {
            lock (lockObject)
            {
                base.Reset();
            }
        }
    }
}
