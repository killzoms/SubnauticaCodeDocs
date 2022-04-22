using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyCSharp
{
    public static class BatchUpgrade
    {
        private static readonly IBatchUpgrade[] batchUpgrades = new IBatchUpgrade[28]
        {
            new B54Upgrade(),
            new B55Upgrade(),
            new B56Upgrade(),
            new B57Upgrade(),
            new B58Upgrade(),
            new B59Upgrade(),
            new B60Upgrade(),
            new B61Upgrade(),
            new B63Upgrade(),
            new B64Upgrade(),
            new B65Upgrade(),
            new B66Upgrade(),
            new B68Upgrade(),
            new B69Upgrade(),
            new B70Upgrade(),
            new B71Upgrade(),
            new B72Upgrade(),
            new B73Upgrade(),
            new B74Upgrade(),
            new B75Upgrade(),
            new B75aUpgrade(),
            new B76Upgrade(),
            new B77Upgrade(),
            new B78Upgrade(),
            new B79Upgrade(),
            new B81Upgrade(),
            new B82Upgrade(),
            new B89Upgrade()
        };

        public static bool NeedsUpgrade(int changeSet)
        {
            return batchUpgrades.Any((IBatchUpgrade p) => p.GetChangeset() > changeSet);
        }

        public static IEnumerator UpgradeBatches(string slotName, int changeSet)
        {
            IEnumerable<IBatchUpgrade> upgrades = batchUpgrades.Where((IBatchUpgrade p) => p.GetChangeset() > changeSet);
            return DeleteBatchesInSlotAsync(slotName, upgrades);
        }

        private static UserStorageUtils.AsyncOperation DeleteBatchesInSlotAsync(string slotName, IEnumerable<IBatchUpgrade> upgrades)
        {
            HashSet<Int3> hashSet = new HashSet<Int3>(upgrades.SelectMany((IBatchUpgrade p) => p.GetBatches()), Int3.equalityComparer);
            List<string> list = new List<string>();
            foreach (Int3 item in hashSet)
            {
                string compiledOctreesCacheFilename = LargeWorldStreamer.GetCompiledOctreesCacheFilename(item);
                string batchObjectsFilename = LargeWorldStreamer.GetBatchObjectsFilename(item);
                list.Add(LargeWorldStreamer.GetCompiledOctreesCachePath("", compiledOctreesCacheFilename));
                list.Add(LargeWorldStreamer.GetBatchObjectsPath("", batchObjectsFilename));
                list.Add(CellManager.GetCacheBatchCellsPath("", item));
            }
            return SaveLoadManager.main.DeleteFilesInSlot(slotName, list);
        }
    }
}
