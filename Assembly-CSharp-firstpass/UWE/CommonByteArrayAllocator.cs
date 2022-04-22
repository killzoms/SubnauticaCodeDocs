using UnityEngine;

namespace UWE
{
    public static class CommonByteArrayAllocator
    {
        private sealed class ManagedAlloc : ArrayAllocator<byte>.IAlloc, IEstimateBytes
        {
            private readonly byte[] buffer;

            public int Offset => 0;

            public int Length => buffer.Length;

            public byte[] Array => buffer;

            public byte this[int Index]
            {
                get
                {
                    return buffer[Index];
                }
                set
                {
                    buffer[Index] = value;
                }
            }

            public ManagedAlloc(int length)
            {
                buffer = new byte[length];
            }

            public long EstimateBytes()
            {
                return buffer.LongLength;
            }
        }

        public static readonly ArrayAllocator<byte> largeBlock = new ArrayAllocator<byte>(1, 32, 262144, 4194304, 4, 20000, coalesceAllocs: false);

        public static readonly ArrayAllocator<byte> smallBlock = new ArrayAllocator<byte>(1, 4, 16, 65536, 10, 20000, coalesceAllocs: false);

        public static ArrayAllocator<byte>.IAlloc Allocate(int length)
        {
            if (length <= smallBlock.MaxBucketSize)
            {
                lock (smallBlock)
                {
                    return smallBlock.Allocate(length);
                }
            }
            if (length <= largeBlock.MaxBucketSize)
            {
                lock (largeBlock)
                {
                    return largeBlock.Allocate(length);
                }
            }
            Debug.LogWarningFormat("Allocating {0} bytes. Too big for CommonByteArrayAllocator.", length);
            return new ManagedAlloc(length);
        }

        public static void Free(ArrayAllocator<byte>.IAlloc ialloc)
        {
            ArrayAllocator<byte>.Alloc alloc = ialloc as ArrayAllocator<byte>.Alloc;
            if (alloc == null)
            {
                return;
            }
            if (alloc.Length <= smallBlock.MaxBucketSize)
            {
                lock (smallBlock)
                {
                    smallBlock.Free(alloc);
                }
            }
            else
            {
                lock (largeBlock)
                {
                    largeBlock.Free(alloc);
                }
            }
        }

        public static long EstimateBytes()
        {
            return largeBlock.EstimateBytes() + smallBlock.EstimateBytes();
        }
    }
}
