using UnityEngine;

namespace UWE
{
    public interface IArrayPool<T>
    {
        int numArraysPooled { get; }

        int numArraysOutstanding { get; }

        long totalBytesAllocated { get; }

        int numBuckets { get; }

        int NumArraysAllocated { get; }

        int elementSize { get; }

        int bucketStride { get; }

        int poolHits { get; }

        int poolMisses { get; }

        void WarmupElement(int bucketIndex, int count);

        void Warmup(AnimationCurve warmupCurve, int maxEntriesPerBucket, int numBuckets, int startBucket);

        T[] Get(int minLength);

        int GetArraySize(int minLength);

        void Return(T[] arr);

        void Reset();

        void ResetCacheStats();

        long EstimateBytes();

        void PrintDebugStats();

        void GetBucketInfo(ref int[] arraysPooled, ref int[] arraysOutstanding, ref int[] peakArraysOustanding, ref long[] bytesWasted);
    }
}
