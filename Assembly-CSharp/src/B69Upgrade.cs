using System.Collections.Generic;

namespace AssemblyCSharp
{
    public class B69Upgrade : IBatchUpgrade
    {
        private static readonly Int3[] batches = new Int3[88]
        {
            new Int3(13, 8, 8),
            new Int3(13, 8, 9),
            new Int3(13, 8, 10),
            new Int3(13, 8, 11),
            new Int3(14, 8, 8),
            new Int3(14, 8, 9),
            new Int3(14, 8, 10),
            new Int3(14, 8, 11),
            new Int3(15, 8, 9),
            new Int3(15, 8, 10),
            new Int3(15, 8, 11),
            new Int3(13, 9, 8),
            new Int3(13, 9, 9),
            new Int3(13, 9, 10),
            new Int3(13, 9, 11),
            new Int3(14, 9, 8),
            new Int3(14, 9, 9),
            new Int3(14, 9, 10),
            new Int3(14, 9, 11),
            new Int3(15, 9, 9),
            new Int3(15, 9, 10),
            new Int3(15, 9, 11),
            new Int3(16, 9, 10),
            new Int3(16, 9, 11),
            new Int3(13, 10, 12),
            new Int3(10, 11, 13),
            new Int3(12, 11, 14),
            new Int3(12, 11, 15),
            new Int3(13, 11, 13),
            new Int3(13, 11, 15),
            new Int3(4, 12, 14),
            new Int3(4, 12, 15),
            new Int3(4, 12, 16),
            new Int3(5, 12, 14),
            new Int3(5, 12, 15),
            new Int3(4, 13, 15),
            new Int3(4, 13, 16),
            new Int3(5, 13, 15),
            new Int3(5, 13, 16),
            new Int3(6, 13, 14),
            new Int3(6, 13, 15),
            new Int3(6, 13, 16),
            new Int3(7, 13, 14),
            new Int3(7, 13, 15),
            new Int3(7, 13, 16),
            new Int3(8, 13, 15),
            new Int3(6, 14, 17),
            new Int3(6, 14, 18),
            new Int3(7, 14, 11),
            new Int3(7, 14, 12),
            new Int3(7, 14, 16),
            new Int3(7, 14, 17),
            new Int3(7, 14, 18),
            new Int3(7, 14, 19),
            new Int3(8, 14, 11),
            new Int3(8, 14, 12),
            new Int3(8, 14, 16),
            new Int3(8, 14, 17),
            new Int3(8, 14, 18),
            new Int3(8, 14, 19),
            new Int3(9, 14, 11),
            new Int3(9, 14, 12),
            new Int3(9, 14, 18),
            new Int3(6, 15, 17),
            new Int3(6, 15, 18),
            new Int3(7, 15, 16),
            new Int3(7, 15, 17),
            new Int3(7, 15, 18),
            new Int3(7, 15, 19),
            new Int3(8, 15, 6),
            new Int3(8, 15, 6),
            new Int3(8, 15, 16),
            new Int3(8, 15, 17),
            new Int3(8, 15, 18),
            new Int3(8, 15, 19),
            new Int3(9, 15, 18),
            new Int3(9, 15, 21),
            new Int3(8, 16, 22),
            new Int3(8, 16, 22),
            new Int3(7, 17, 19),
            new Int3(7, 17, 19),
            new Int3(8, 17, 19),
            new Int3(8, 17, 19),
            new Int3(20, 17, 9),
            new Int3(7, 18, 10),
            new Int3(7, 18, 10),
            new Int3(14, 18, 19),
            new Int3(15, 18, 20)
        };

        public int GetChangeset()
        {
            return 38130;
        }

        public IEnumerable<Int3> GetBatches()
        {
            return batches;
        }
    }
}
