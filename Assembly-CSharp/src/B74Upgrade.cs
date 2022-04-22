using System.Collections.Generic;

namespace AssemblyCSharp
{
    public class B74Upgrade : IBatchUpgrade
    {
        private static readonly Int3[] batches = new Int3[542]
        {
            new Int3(11, 17, 9),
            new Int3(12, 17, 9),
            new Int3(15, 18, 19),
            new Int3(15, 18, 20),
            new Int3(15, 19, 19),
            new Int3(15, 19, 20),
            new Int3(9, 14, 7),
            new Int3(9, 14, 8),
            new Int3(9, 15, 7),
            new Int3(9, 15, 8),
            new Int3(10, 14, 8),
            new Int3(10, 15, 8),
            new Int3(9, 15, 6),
            new Int3(10, 14, 6),
            new Int3(10, 14, 7),
            new Int3(10, 15, 7),
            new Int3(11, 14, 6),
            new Int3(11, 15, 6),
            new Int3(12, 11, 9),
            new Int3(12, 11, 10),
            new Int3(12, 11, 11),
            new Int3(12, 12, 9),
            new Int3(12, 13, 9),
            new Int3(12, 13, 10),
            new Int3(12, 13, 11),
            new Int3(12, 14, 6),
            new Int3(12, 14, 7),
            new Int3(12, 14, 8),
            new Int3(12, 15, 6),
            new Int3(12, 15, 7),
            new Int3(13, 11, 9),
            new Int3(13, 11, 10),
            new Int3(13, 11, 12),
            new Int3(13, 12, 9),
            new Int3(13, 12, 10),
            new Int3(13, 13, 7),
            new Int3(13, 13, 8),
            new Int3(13, 13, 9),
            new Int3(13, 13, 10),
            new Int3(13, 13, 11),
            new Int3(13, 14, 6),
            new Int3(13, 14, 7),
            new Int3(13, 14, 8),
            new Int3(13, 14, 9),
            new Int3(13, 14, 10),
            new Int3(13, 15, 6),
            new Int3(13, 15, 7),
            new Int3(13, 15, 8),
            new Int3(14, 11, 9),
            new Int3(14, 11, 10),
            new Int3(14, 12, 9),
            new Int3(14, 12, 10),
            new Int3(14, 13, 7),
            new Int3(14, 13, 8),
            new Int3(14, 13, 9),
            new Int3(14, 13, 10),
            new Int3(14, 13, 11),
            new Int3(14, 13, 12),
            new Int3(14, 14, 6),
            new Int3(14, 14, 7),
            new Int3(14, 14, 8),
            new Int3(14, 14, 9),
            new Int3(14, 14, 10),
            new Int3(14, 15, 6),
            new Int3(14, 15, 7),
            new Int3(14, 15, 8),
            new Int3(15, 11, 9),
            new Int3(15, 11, 10),
            new Int3(15, 11, 11),
            new Int3(15, 12, 9),
            new Int3(15, 12, 10),
            new Int3(15, 13, 9),
            new Int3(15, 13, 10),
            new Int3(11, 14, 13),
            new Int3(10, 13, 15),
            new Int3(10, 14, 15),
            new Int3(11, 13, 15),
            new Int3(11, 14, 15),
            new Int3(13, 11, 11),
            new Int3(14, 11, 11),
            new Int3(14, 11, 12),
            new Int3(14, 12, 11),
            new Int3(14, 12, 12),
            new Int3(14, 12, 13),
            new Int3(15, 11, 12),
            new Int3(15, 12, 13),
            new Int3(10, 15, 6),
            new Int3(11, 14, 7),
            new Int3(11, 14, 8),
            new Int3(11, 15, 7),
            new Int3(11, 15, 8),
            new Int3(12, 15, 8),
            new Int3(12, 16, 7),
            new Int3(12, 16, 8),
            new Int3(13, 16, 7),
            new Int3(13, 16, 8),
            new Int3(8, 14, 6),
            new Int3(8, 15, 6),
            new Int3(9, 14, 6),
            new Int3(5, 15, 9),
            new Int3(6, 15, 10),
            new Int3(14, 9, 8),
            new Int3(14, 9, 9),
            new Int3(14, 9, 10),
            new Int3(14, 10, 8),
            new Int3(14, 10, 9),
            new Int3(3, 17, 15),
            new Int3(3, 17, 16),
            new Int3(4, 17, 15),
            new Int3(4, 17, 16),
            new Int3(3, 15, 15),
            new Int3(3, 15, 16),
            new Int3(4, 15, 15),
            new Int3(4, 15, 16),
            new Int3(11, 17, 6),
            new Int3(11, 17, 8),
            new Int3(11, 18, 5),
            new Int3(11, 18, 6),
            new Int3(12, 17, 6),
            new Int3(12, 17, 8),
            new Int3(12, 18, 6),
            new Int3(13, 17, 6),
            new Int3(13, 17, 7),
            new Int3(13, 18, 7),
            new Int3(7, 15, 9),
            new Int3(7, 15, 10),
            new Int3(11, 16, 5),
            new Int3(11, 18, 3),
            new Int3(11, 18, 4),
            new Int3(12, 16, 5),
            new Int3(12, 18, 3),
            new Int3(12, 18, 4),
            new Int3(12, 18, 5),
            new Int3(13, 18, 3),
            new Int3(13, 18, 4),
            new Int3(2, 15, 15),
            new Int3(2, 15, 16),
            new Int3(2, 16, 15),
            new Int3(2, 16, 16),
            new Int3(7, 14, 9),
            new Int3(8, 14, 9),
            new Int3(8, 15, 9),
            new Int3(3, 16, 15),
            new Int3(3, 16, 16),
            new Int3(4, 16, 15),
            new Int3(4, 16, 16),
            new Int3(13, 9, 8),
            new Int3(13, 9, 9),
            new Int3(13, 9, 10),
            new Int3(14, 8, 10),
            new Int3(15, 9, 8),
            new Int3(15, 10, 9),
            new Int3(13, 8, 9),
            new Int3(13, 8, 10),
            new Int3(13, 8, 11),
            new Int3(13, 10, 9),
            new Int3(13, 10, 11),
            new Int3(14, 8, 11),
            new Int3(15, 9, 9),
            new Int3(11, 17, 17),
            new Int3(11, 17, 18),
            new Int3(12, 17, 17),
            new Int3(12, 17, 18),
            new Int3(19, 17, 20),
            new Int3(19, 16, 20),
            new Int3(8, 18, 12),
            new Int3(10, 18, 16),
            new Int3(5, 14, 7),
            new Int3(5, 14, 9),
            new Int3(5, 14, 10),
            new Int3(5, 15, 7),
            new Int3(5, 15, 8),
            new Int3(5, 15, 10),
            new Int3(6, 14, 7),
            new Int3(6, 14, 8),
            new Int3(6, 14, 9),
            new Int3(6, 14, 10),
            new Int3(6, 15, 7),
            new Int3(6, 15, 8),
            new Int3(7, 14, 8),
            new Int3(7, 15, 6),
            new Int3(7, 15, 8),
            new Int3(8, 14, 7),
            new Int3(8, 14, 8),
            new Int3(8, 15, 7),
            new Int3(8, 15, 8),
            new Int3(6, 14, 6),
            new Int3(6, 15, 6),
            new Int3(7, 14, 6),
            new Int3(9, 14, 10),
            new Int3(4, 15, 10),
            new Int3(4, 15, 11),
            new Int3(5, 14, 12),
            new Int3(5, 15, 11),
            new Int3(5, 15, 12),
            new Int3(8, 15, 10),
            new Int3(6, 13, 11),
            new Int3(6, 13, 12),
            new Int3(6, 14, 11),
            new Int3(7, 13, 11),
            new Int3(7, 13, 12),
            new Int3(7, 15, 11),
            new Int3(8, 13, 11),
            new Int3(8, 13, 12),
            new Int3(8, 15, 12),
            new Int3(9, 15, 12),
            new Int3(10, 13, 14),
            new Int3(9, 14, 13),
            new Int3(10, 14, 12),
            new Int3(10, 14, 13),
            new Int3(7, 15, 13),
            new Int3(8, 14, 13),
            new Int3(8, 15, 11),
            new Int3(9, 14, 12),
            new Int3(9, 15, 11),
            new Int3(8, 14, 12),
            new Int3(7, 14, 10),
            new Int3(8, 14, 10),
            new Int3(8, 14, 11),
            new Int3(9, 14, 11),
            new Int3(9, 15, 10),
            new Int3(4, 14, 10),
            new Int3(4, 14, 11),
            new Int3(4, 14, 12),
            new Int3(4, 15, 12),
            new Int3(5, 14, 11),
            new Int3(6, 13, 13),
            new Int3(6, 14, 12),
            new Int3(6, 14, 13),
            new Int3(6, 15, 11),
            new Int3(6, 15, 12),
            new Int3(6, 15, 13),
            new Int3(7, 13, 13),
            new Int3(7, 14, 11),
            new Int3(7, 14, 12),
            new Int3(7, 14, 13),
            new Int3(11, 17, 5),
            new Int3(11, 16, 3),
            new Int3(11, 17, 3),
            new Int3(13, 10, 12),
            new Int3(14, 9, 12),
            new Int3(14, 10, 12),
            new Int3(12, 9, 10),
            new Int3(12, 9, 11),
            new Int3(12, 10, 10),
            new Int3(12, 10, 11),
            new Int3(7, 14, 7),
            new Int3(7, 15, 7),
            new Int3(12, 9, 12),
            new Int3(13, 9, 12),
            new Int3(14, 10, 11),
            new Int3(15, 9, 10),
            new Int3(15, 9, 11),
            new Int3(6, 15, 9),
            new Int3(9, 15, 22),
            new Int3(9, 15, 21),
            new Int3(9, 16, 21),
            new Int3(9, 16, 22),
            new Int3(11, 12, 15),
            new Int3(12, 8, 8),
            new Int3(12, 8, 9),
            new Int3(12, 8, 10),
            new Int3(12, 9, 8),
            new Int3(13, 8, 8),
            new Int3(13, 9, 11),
            new Int3(14, 8, 8),
            new Int3(14, 8, 9),
            new Int3(14, 9, 11),
            new Int3(14, 10, 10),
            new Int3(15, 8, 8),
            new Int3(15, 8, 9),
            new Int3(15, 8, 10),
            new Int3(19, 16, 21),
            new Int3(19, 17, 21),
            new Int3(12, 16, 3),
            new Int3(12, 16, 4),
            new Int3(12, 17, 3),
            new Int3(12, 17, 4),
            new Int3(13, 16, 3),
            new Int3(13, 16, 4),
            new Int3(13, 17, 3),
            new Int3(9, 13, 14),
            new Int3(9, 13, 15),
            new Int3(9, 14, 14),
            new Int3(9, 14, 15),
            new Int3(5, 14, 8),
            new Int3(12, 9, 9),
            new Int3(12, 9, 13),
            new Int3(12, 10, 9),
            new Int3(13, 10, 8),
            new Int3(15, 9, 12),
            new Int3(15, 10, 8),
            new Int3(15, 10, 12),
            new Int3(16, 9, 8),
            new Int3(16, 9, 9),
            new Int3(16, 9, 10),
            new Int3(16, 9, 11),
            new Int3(16, 10, 8),
            new Int3(16, 10, 9),
            new Int3(12, 17, 5),
            new Int3(13, 16, 5),
            new Int3(13, 17, 5),
            new Int3(13, 18, 5),
            new Int3(14, 16, 2),
            new Int3(14, 16, 3),
            new Int3(14, 16, 4),
            new Int3(14, 16, 5),
            new Int3(14, 17, 2),
            new Int3(14, 17, 3),
            new Int3(14, 17, 4),
            new Int3(14, 17, 5),
            new Int3(14, 18, 4),
            new Int3(14, 18, 5),
            new Int3(15, 16, 2),
            new Int3(15, 16, 3),
            new Int3(15, 16, 4),
            new Int3(15, 16, 5),
            new Int3(15, 17, 2),
            new Int3(15, 17, 3),
            new Int3(15, 17, 4),
            new Int3(15, 17, 5),
            new Int3(16, 16, 2),
            new Int3(16, 16, 3),
            new Int3(16, 16, 4),
            new Int3(16, 17, 2),
            new Int3(16, 17, 3),
            new Int3(16, 17, 4),
            new Int3(15, 11, 17),
            new Int3(15, 11, 18),
            new Int3(15, 12, 17),
            new Int3(15, 12, 18),
            new Int3(8, 13, 14),
            new Int3(8, 13, 15),
            new Int3(8, 14, 14),
            new Int3(8, 14, 15),
            new Int3(13, 10, 10),
            new Int3(10, 14, 14),
            new Int3(11, 14, 14),
            new Int3(6, 17, 15),
            new Int3(6, 17, 16),
            new Int3(7, 17, 7),
            new Int3(7, 17, 15),
            new Int3(7, 17, 16),
            new Int3(17, 18, 13),
            new Int3(17, 19, 13),
            new Int3(19, 17, 16),
            new Int3(7, 12, 12),
            new Int3(7, 12, 13),
            new Int3(7, 12, 14),
            new Int3(7, 13, 14),
            new Int3(7, 14, 14),
            new Int3(7, 15, 12),
            new Int3(7, 15, 14),
            new Int3(8, 12, 13),
            new Int3(8, 12, 14),
            new Int3(8, 13, 13),
            new Int3(8, 15, 13),
            new Int3(8, 15, 14),
            new Int3(8, 15, 15),
            new Int3(8, 16, 13),
            new Int3(8, 16, 14),
            new Int3(8, 16, 15),
            new Int3(9, 12, 12),
            new Int3(9, 12, 13),
            new Int3(9, 12, 14),
            new Int3(9, 13, 12),
            new Int3(9, 13, 13),
            new Int3(9, 15, 13),
            new Int3(9, 15, 14),
            new Int3(9, 15, 15),
            new Int3(9, 16, 13),
            new Int3(9, 16, 14),
            new Int3(9, 16, 15),
            new Int3(10, 12, 12),
            new Int3(10, 12, 13),
            new Int3(10, 13, 12),
            new Int3(10, 13, 13),
            new Int3(11, 12, 11),
            new Int3(11, 12, 12),
            new Int3(11, 12, 13),
            new Int3(11, 12, 14),
            new Int3(11, 13, 11),
            new Int3(11, 13, 12),
            new Int3(11, 13, 13),
            new Int3(11, 13, 14),
            new Int3(11, 14, 11),
            new Int3(11, 14, 12),
            new Int3(12, 12, 11),
            new Int3(12, 12, 12),
            new Int3(12, 12, 13),
            new Int3(12, 12, 14),
            new Int3(12, 13, 12),
            new Int3(12, 13, 13),
            new Int3(12, 14, 11),
            new Int3(12, 14, 12),
            new Int3(12, 14, 13),
            new Int3(13, 8, 13),
            new Int3(13, 8, 16),
            new Int3(13, 9, 13),
            new Int3(13, 9, 14),
            new Int3(13, 9, 15),
            new Int3(13, 9, 16),
            new Int3(13, 10, 13),
            new Int3(13, 10, 16),
            new Int3(13, 11, 13),
            new Int3(13, 11, 14),
            new Int3(13, 11, 15),
            new Int3(13, 11, 16),
            new Int3(13, 12, 11),
            new Int3(13, 12, 12),
            new Int3(13, 12, 13),
            new Int3(13, 12, 14),
            new Int3(13, 13, 12),
            new Int3(13, 13, 13),
            new Int3(13, 14, 11),
            new Int3(13, 14, 12),
            new Int3(13, 14, 13),
            new Int3(14, 8, 13),
            new Int3(14, 8, 14),
            new Int3(14, 8, 15),
            new Int3(14, 8, 16),
            new Int3(14, 9, 13),
            new Int3(14, 9, 14),
            new Int3(14, 9, 15),
            new Int3(14, 9, 16),
            new Int3(14, 10, 13),
            new Int3(14, 10, 14),
            new Int3(14, 10, 15),
            new Int3(14, 10, 16),
            new Int3(14, 11, 13),
            new Int3(14, 11, 14),
            new Int3(14, 11, 15),
            new Int3(14, 11, 16),
            new Int3(15, 8, 13),
            new Int3(15, 8, 14),
            new Int3(15, 8, 15),
            new Int3(15, 8, 16),
            new Int3(15, 9, 13),
            new Int3(15, 9, 14),
            new Int3(15, 9, 15),
            new Int3(15, 9, 16),
            new Int3(15, 10, 10),
            new Int3(15, 10, 11),
            new Int3(15, 10, 13),
            new Int3(15, 10, 14),
            new Int3(15, 10, 15),
            new Int3(15, 10, 16),
            new Int3(15, 11, 13),
            new Int3(15, 11, 14),
            new Int3(15, 11, 15),
            new Int3(15, 11, 16),
            new Int3(15, 12, 11),
            new Int3(15, 12, 12),
            new Int3(15, 13, 11),
            new Int3(15, 13, 12),
            new Int3(16, 10, 10),
            new Int3(16, 10, 11),
            new Int3(16, 10, 12),
            new Int3(16, 11, 10),
            new Int3(16, 11, 11),
            new Int3(16, 11, 12),
            new Int3(16, 12, 11),
            new Int3(16, 12, 12),
            new Int3(16, 13, 11),
            new Int3(16, 13, 12),
            new Int3(8, 10, 12),
            new Int3(8, 10, 13),
            new Int3(8, 10, 14),
            new Int3(8, 10, 15),
            new Int3(8, 11, 12),
            new Int3(8, 11, 13),
            new Int3(9, 10, 12),
            new Int3(9, 10, 13),
            new Int3(9, 10, 14),
            new Int3(9, 10, 15),
            new Int3(9, 11, 12),
            new Int3(9, 11, 13),
            new Int3(9, 11, 14),
            new Int3(9, 11, 15),
            new Int3(10, 8, 14),
            new Int3(10, 8, 15),
            new Int3(10, 9, 14),
            new Int3(10, 9, 15),
            new Int3(10, 10, 12),
            new Int3(10, 10, 14),
            new Int3(10, 10, 15),
            new Int3(10, 11, 12),
            new Int3(10, 11, 13),
            new Int3(10, 11, 14),
            new Int3(10, 11, 15),
            new Int3(11, 8, 14),
            new Int3(11, 8, 15),
            new Int3(11, 9, 14),
            new Int3(11, 9, 15),
            new Int3(11, 10, 12),
            new Int3(11, 10, 13),
            new Int3(11, 10, 14),
            new Int3(11, 10, 15),
            new Int3(11, 11, 12),
            new Int3(11, 11, 13),
            new Int3(11, 11, 14),
            new Int3(11, 11, 15),
            new Int3(12, 8, 14),
            new Int3(12, 8, 15),
            new Int3(12, 9, 14),
            new Int3(12, 9, 15),
            new Int3(12, 10, 12),
            new Int3(12, 10, 13),
            new Int3(12, 10, 14),
            new Int3(12, 10, 15),
            new Int3(12, 11, 12),
            new Int3(12, 11, 13),
            new Int3(12, 11, 14),
            new Int3(12, 11, 15),
            new Int3(12, 13, 14),
            new Int3(12, 13, 15),
            new Int3(12, 14, 14),
            new Int3(12, 14, 15),
            new Int3(12, 15, 14),
            new Int3(12, 15, 15),
            new Int3(13, 8, 14),
            new Int3(13, 8, 15),
            new Int3(13, 10, 14),
            new Int3(13, 10, 15),
            new Int3(13, 13, 14),
            new Int3(13, 13, 15),
            new Int3(13, 14, 14),
            new Int3(13, 14, 15),
            new Int3(13, 15, 14),
            new Int3(13, 15, 15),
            new Int3(11, 16, 4),
            new Int3(11, 17, 4),
            new Int3(11, 17, 7),
            new Int3(11, 18, 7),
            new Int3(11, 18, 8),
            new Int3(12, 17, 7),
            new Int3(12, 18, 7),
            new Int3(12, 18, 8),
            new Int3(13, 17, 4),
            new Int3(13, 17, 8),
            new Int3(13, 18, 6),
            new Int3(13, 18, 8)
        };

        public int GetChangeset()
        {
            return 45274;
        }

        public IEnumerable<Int3> GetBatches()
        {
            return batches;
        }
    }
}
