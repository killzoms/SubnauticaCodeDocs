using System.Collections.Generic;

namespace AssemblyCSharp
{
    public class B81Upgrade : IBatchUpgrade
    {
        public int GetChangeset()
        {
            return 57467;
        }

        public IEnumerable<Int3> GetBatches()
        {
            return new Int3.Bounds(Int3.zero, new Int3(25, 19, 25)).ToEnumerable();
        }
    }
}
