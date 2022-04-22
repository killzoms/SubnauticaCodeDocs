namespace UWE
{
    public interface ISplitArrayPool<T>
    {
        IArrayPool<T> poolSmall { get; }

        IArrayPool<T> poolBig { get; }

        int NumArraysAllocated { get; }

        int NumArraysOutstanding { get; }

        int PoolHits { get; }

        int PoolMisses { get; }

        void Reset();

        void ResetCacheStats();

        T[] Get(int minLength);

        void Return(T[] arr);

        long EstimateBytes();

        void PrintDebugStats();
    }
}
