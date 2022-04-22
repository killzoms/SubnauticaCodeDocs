using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UWE
{
    public class ArrayPool<T> : IArrayPool<T>
    {
        private readonly object bucketsLock = new object();

        private readonly List<ArrayPoolBucket<T>> buckets = new List<ArrayPoolBucket<T>>();

        private readonly ILRUQueue<T[]> leastRecentlyUsed;

        private readonly T[] emptyArray = new T[0];

        private readonly ILRUQueue<T[]> mockLRUQueue = new MockLRUQueue<T[]>();

        public readonly int numBucketsToCheck;

        public readonly int desiredMemoryCap;

        public int numArraysPooled
        {
            get
            {
                lock (bucketsLock)
                {
                    int num = 0;
                    foreach (ArrayPoolBucket<T> bucket in buckets)
                    {
                        num += bucket.numArraysPooled;
                    }
                    return num;
                }
            }
        }

        public int numArraysOutstanding
        {
            get
            {
                lock (bucketsLock)
                {
                    int num = 0;
                    foreach (ArrayPoolBucket<T> bucket in buckets)
                    {
                        num += bucket.numArraysOutstanding;
                    }
                    return num;
                }
            }
        }

        public long totalBytesAllocated
        {
            get
            {
                lock (bucketsLock)
                {
                    long num = 0L;
                    foreach (ArrayPoolBucket<T> bucket in buckets)
                    {
                        num += bucket.totalBytesAllocated;
                    }
                    return num;
                }
            }
        }

        public int numBuckets
        {
            get
            {
                lock (bucketsLock)
                {
                    return buckets.Count;
                }
            }
        }

        public int poolHits { get; private set; }

        public int poolMisses { get; private set; }

        public int elementSize { get; private set; }

        public int bucketStride { get; private set; }

        public int NumArraysAllocated => numArraysOutstanding + numArraysPooled;

        public ArrayPool(int elementSize, int bucketStride, int numBucketsToCheck = 0, int desiredMemoryCap = 0)
        {
            this.elementSize = elementSize;
            this.bucketStride = bucketStride;
            this.numBucketsToCheck = numBucketsToCheck;
            this.desiredMemoryCap = desiredMemoryCap;
            ILRUQueue<T[]> iLRUQueue;
            if (desiredMemoryCap <= 0)
            {
                iLRUQueue = mockLRUQueue;
            }
            else
            {
                ILRUQueue<T[]> iLRUQueue2 = new LRUQueue<T[]>();
                iLRUQueue = iLRUQueue2;
            }
            leastRecentlyUsed = iLRUQueue;
        }

        private int BucketForLength(int len)
        {
            return (len - 1) / bucketStride;
        }

        private int SizeForBucket(int bucketIndex)
        {
            return (bucketIndex + 1) * bucketStride;
        }

        private void LockedEnsureSize(int size)
        {
            while (buckets.Count < size)
            {
                int count = buckets.Count;
                int arraySize = SizeForBucket(count);
                buckets.Add(new ArrayPoolBucket<T>(arraySize));
            }
        }

        private void LockedWarmupElement(int bucketIndex, int count)
        {
            LockedEnsureSize(bucketIndex + 1);
            ArrayPoolBucket<T> arrayPoolBucket = buckets[bucketIndex];
            SizeForBucket(bucketIndex);
            for (int i = 0; i < count; i++)
            {
                T[] keyElement = arrayPoolBucket.AddArray();
                leastRecentlyUsed.PushBack(keyElement);
            }
        }

        public void WarmupElement(int bucketIndex, int count)
        {
            lock (bucketsLock)
            {
                LockedWarmupElement(bucketIndex, count);
            }
        }

        public void Warmup(AnimationCurve warmupCurve, int maxEntriesPerBucket, int numBuckets, int startBucket)
        {
            lock (bucketsLock)
            {
                for (int i = startBucket; i < numBuckets; i++)
                {
                    float time = (float)i / (float)numBuckets;
                    int count = (int)(warmupCurve.Evaluate(time) * (float)maxEntriesPerBucket);
                    LockedWarmupElement(i, count);
                }
            }
        }

        public int GetArraySize(int minLength)
        {
            int bucketIndex = BucketForLength(minLength);
            return SizeForBucket(bucketIndex);
        }

        public T[] Get(int minLength)
        {
            if (minLength <= 0)
            {
                return emptyArray;
            }
            lock (bucketsLock)
            {
                int num = BucketForLength(minLength);
                LockedEnsureSize(num + 1);
                int num2 = Mathf.Min(num + numBucketsToCheck, buckets.Count - 1);
                for (int i = num; i <= num2; i++)
                {
                    if (buckets[i].TryGet(out var result))
                    {
                        poolHits++;
                        leastRecentlyUsed.RemoveElement(result);
                        return result;
                    }
                }
                poolMisses++;
                return buckets[num].AllocateWasteArray(minLength);
            }
        }

        public void Return(T[] arr)
        {
            if (arr == null || arr.Length == 0)
            {
                return;
            }
            lock (bucketsLock)
            {
                int num = BucketForLength(arr.Length);
                LockedEnsureSize(num + 1);
                buckets[num].Return(arr);
                leastRecentlyUsed.PushBack(arr);
                if (desiredMemoryCap > 0 && totalBytesAllocated > desiredMemoryCap)
                {
                    LockedTryCleanPool();
                }
            }
        }

        private void LockedTryCleanPool(long targetMemorySize)
        {
            while (leastRecentlyUsed.Count > 0 && totalBytesAllocated > targetMemorySize)
            {
                T[] array = leastRecentlyUsed.Peek();
                int index = BucketForLength(array.Length);
                T[] b = buckets[index].RemoveArray();
                leastRecentlyUsed.SwapElements(array, b);
                leastRecentlyUsed.Pop();
            }
        }

        private void LockedTryCleanPool()
        {
            if (leastRecentlyUsed.Count != 0)
            {
                float num = 0.8f;
                long targetMemorySize = (long)((float)desiredMemoryCap * num);
                LockedTryCleanPool(targetMemorySize);
            }
        }

        public void Reset()
        {
            lock (bucketsLock)
            {
                leastRecentlyUsed.Clear();
                foreach (ArrayPoolBucket<T> bucket in buckets)
                {
                    bucket.Clear();
                }
                buckets.Clear();
                poolHits = 0;
                poolMisses = 0;
            }
        }

        public void ResetCacheStats()
        {
            lock (bucketsLock)
            {
                poolHits = 0;
                poolMisses = 0;
            }
        }

        public long EstimateBytes()
        {
            return totalBytesAllocated;
        }

        public void PrintDebugStats()
        {
            StringBuilder stringBuilder = new StringBuilder();
            lock (bucketsLock)
            {
                stringBuilder.AppendFormat("ArrayPool / Bucket Stride:  {0} / Total Allocated  {1} => {2} bytes / Desired Memory Cap {3} \n", bucketStride, numArraysPooled + numArraysOutstanding, totalBytesAllocated, desiredMemoryCap);
                stringBuilder.AppendFormat("Pool Hits {0} / Pool Misses {1}\n", poolHits, poolMisses);
                stringBuilder.AppendLine("Pool Index,Array Size(Bytes),In,Out,Count,Total Size,Waste,BytesIn,BytesOut");
                int num = 0;
                int num2 = 0;
                long num3 = 0L;
                long num4 = 0L;
                long num5 = 0L;
                int num6 = 0;
                int num7 = 0;
                for (int i = 0; i < buckets.Count; i++)
                {
                    ArrayPoolBucket<T> arrayPoolBucket = buckets[i];
                    int arraySize = arrayPoolBucket.arraySize;
                    int num8 = arrayPoolBucket.numArraysPooled;
                    int num9 = arrayPoolBucket.numArraysOutstanding;
                    int num10 = num8 + num9;
                    int num11 = arraySize * num10;
                    long totalBytesWasted = arrayPoolBucket.totalBytesWasted;
                    int num12 = num8 * arraySize;
                    int num13 = num9 * arraySize;
                    stringBuilder.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8}\n", i, arraySize, num8, num9, num10, num11, totalBytesWasted, num12, num13);
                    num3 += totalBytesWasted;
                    num += num10;
                    num2 += num11;
                    num6 += num12;
                    num7 += num13;
                    num4 += num8;
                    num5 += num9;
                }
                stringBuilder.AppendFormat(",,{0},{1},{2},{3},{4},{5},{6}\n", num4, num5, num, num2, num3, num6, num7);
            }
            Debug.Log(stringBuilder.ToString());
        }

        public void GetBucketInfo(ref int[] arraysPooled, ref int[] arraysOutstanding, ref int[] peakArraysOustanding, ref long[] bytesWasted)
        {
            lock (bucketsLock)
            {
                int count = buckets.Count;
                for (int i = 0; i < count; i++)
                {
                    arraysPooled[i] = buckets[i].numArraysPooled;
                    arraysOutstanding[i] = buckets[i].numArraysOutstanding;
                    bytesWasted[i] = buckets[i].totalBytesWasted;
                    peakArraysOustanding[i] = buckets[i].peakArraysOutstanding;
                }
            }
        }
    }
}
