using System.Collections.Generic;

namespace AssemblyCSharp
{
    public interface IBatchUpgrade
    {
        int GetChangeset();

        IEnumerable<Int3> GetBatches();
    }
}
