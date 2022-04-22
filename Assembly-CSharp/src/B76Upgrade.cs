using System.Collections.Generic;

namespace AssemblyCSharp
{
    public class B76Upgrade : IBatchUpgrade
    {
        private static readonly Int3[] batches = new Int3[285]
        {
            new Int3(15, 19, 20),
            new Int3(15, 19, 21),
            new Int3(18, 19, 12),
            new Int3(18, 19, 13),
            new Int3(19, 19, 12),
            new Int3(19, 19, 13),
            new Int3(4, 18, 13),
            new Int3(5, 18, 11),
            new Int3(5, 18, 12),
            new Int3(5, 18, 15),
            new Int3(5, 18, 16),
            new Int3(6, 18, 5),
            new Int3(6, 18, 8),
            new Int3(6, 18, 11),
            new Int3(6, 18, 12),
            new Int3(6, 18, 14),
            new Int3(6, 18, 15),
            new Int3(6, 18, 16),
            new Int3(6, 18, 17),
            new Int3(6, 18, 18),
            new Int3(7, 18, 14),
            new Int3(7, 18, 15),
            new Int3(7, 18, 16),
            new Int3(7, 18, 17),
            new Int3(7, 18, 18),
            new Int3(8, 18, 14),
            new Int3(8, 18, 15),
            new Int3(8, 18, 16),
            new Int3(8, 18, 17),
            new Int3(8, 18, 18),
            new Int3(9, 18, 14),
            new Int3(9, 18, 15),
            new Int3(9, 18, 16),
            new Int3(9, 18, 17),
            new Int3(9, 18, 18),
            new Int3(10, 18, 18),
            new Int3(11, 18, 5),
            new Int3(11, 18, 6),
            new Int3(11, 18, 7),
            new Int3(11, 18, 8),
            new Int3(11, 18, 20),
            new Int3(12, 18, 5),
            new Int3(12, 18, 6),
            new Int3(12, 18, 7),
            new Int3(12, 18, 8),
            new Int3(13, 18, 5),
            new Int3(13, 18, 6),
            new Int3(13, 18, 7),
            new Int3(14, 18, 4),
            new Int3(14, 18, 5),
            new Int3(15, 18, 4),
            new Int3(15, 18, 5),
            new Int3(15, 18, 13),
            new Int3(15, 18, 14),
            new Int3(15, 18, 15),
            new Int3(15, 18, 16),
            new Int3(15, 18, 17),
            new Int3(15, 18, 20),
            new Int3(15, 18, 21),
            new Int3(16, 18, 5),
            new Int3(16, 18, 13),
            new Int3(16, 18, 14),
            new Int3(16, 18, 15),
            new Int3(16, 18, 16),
            new Int3(16, 18, 17),
            new Int3(16, 18, 20),
            new Int3(16, 18, 21),
            new Int3(17, 18, 13),
            new Int3(17, 18, 14),
            new Int3(17, 18, 15),
            new Int3(17, 18, 16),
            new Int3(17, 18, 17),
            new Int3(18, 18, 12),
            new Int3(18, 18, 13),
            new Int3(18, 18, 14),
            new Int3(18, 18, 15),
            new Int3(18, 18, 16),
            new Int3(19, 18, 8),
            new Int3(19, 18, 9),
            new Int3(19, 18, 12),
            new Int3(19, 18, 13),
            new Int3(19, 18, 14),
            new Int3(20, 18, 8),
            new Int3(20, 18, 9),
            new Int3(20, 18, 19),
            new Int3(5, 17, 11),
            new Int3(5, 17, 12),
            new Int3(5, 17, 14),
            new Int3(5, 17, 15),
            new Int3(5, 17, 16),
            new Int3(5, 17, 17),
            new Int3(5, 17, 18),
            new Int3(6, 17, 11),
            new Int3(6, 17, 12),
            new Int3(6, 17, 14),
            new Int3(6, 17, 15),
            new Int3(6, 17, 16),
            new Int3(6, 17, 17),
            new Int3(6, 17, 18),
            new Int3(7, 17, 4),
            new Int3(7, 17, 8),
            new Int3(7, 17, 14),
            new Int3(7, 17, 15),
            new Int3(7, 17, 16),
            new Int3(7, 17, 17),
            new Int3(7, 17, 18),
            new Int3(7, 17, 19),
            new Int3(8, 17, 7),
            new Int3(8, 17, 8),
            new Int3(8, 17, 14),
            new Int3(8, 17, 15),
            new Int3(8, 17, 16),
            new Int3(8, 17, 17),
            new Int3(8, 17, 18),
            new Int3(9, 17, 15),
            new Int3(9, 17, 16),
            new Int3(9, 17, 17),
            new Int3(9, 17, 18),
            new Int3(10, 17, 18),
            new Int3(11, 17, 3),
            new Int3(11, 17, 4),
            new Int3(11, 17, 5),
            new Int3(11, 17, 6),
            new Int3(11, 17, 7),
            new Int3(11, 17, 8),
            new Int3(12, 17, 2),
            new Int3(12, 17, 3),
            new Int3(12, 17, 4),
            new Int3(12, 17, 5),
            new Int3(12, 17, 6),
            new Int3(12, 17, 7),
            new Int3(12, 17, 8),
            new Int3(13, 17, 2),
            new Int3(13, 17, 3),
            new Int3(13, 17, 4),
            new Int3(13, 17, 5),
            new Int3(13, 17, 6),
            new Int3(13, 17, 7),
            new Int3(14, 17, 1),
            new Int3(14, 17, 2),
            new Int3(14, 17, 3),
            new Int3(14, 17, 4),
            new Int3(14, 17, 5),
            new Int3(15, 17, 1),
            new Int3(15, 17, 2),
            new Int3(15, 17, 3),
            new Int3(15, 17, 4),
            new Int3(15, 17, 5),
            new Int3(15, 17, 14),
            new Int3(15, 17, 15),
            new Int3(15, 17, 16),
            new Int3(15, 17, 17),
            new Int3(15, 17, 20),
            new Int3(15, 17, 21),
            new Int3(16, 17, 2),
            new Int3(16, 17, 3),
            new Int3(16, 17, 4),
            new Int3(16, 17, 5),
            new Int3(16, 17, 14),
            new Int3(16, 17, 15),
            new Int3(16, 17, 16),
            new Int3(16, 17, 17),
            new Int3(16, 17, 18),
            new Int3(16, 17, 19),
            new Int3(16, 17, 20),
            new Int3(16, 17, 21),
            new Int3(17, 17, 14),
            new Int3(17, 17, 15),
            new Int3(17, 17, 16),
            new Int3(17, 17, 17),
            new Int3(17, 17, 18),
            new Int3(17, 17, 19),
            new Int3(18, 17, 13),
            new Int3(18, 17, 14),
            new Int3(18, 17, 15),
            new Int3(18, 17, 16),
            new Int3(18, 17, 17),
            new Int3(18, 17, 18),
            new Int3(18, 17, 19),
            new Int3(19, 17, 8),
            new Int3(19, 17, 9),
            new Int3(19, 17, 14),
            new Int3(19, 17, 17),
            new Int3(20, 17, 8),
            new Int3(20, 17, 9),
            new Int3(20, 17, 17),
            new Int3(20, 17, 18),
            new Int3(20, 17, 19),
            new Int3(21, 17, 16),
            new Int3(21, 17, 17),
            new Int3(21, 17, 18),
            new Int3(21, 17, 19),
            new Int3(5, 16, 3),
            new Int3(6, 16, 4),
            new Int3(6, 16, 5),
            new Int3(7, 16, 4),
            new Int3(7, 16, 5),
            new Int3(11, 16, 1),
            new Int3(11, 16, 2),
            new Int3(11, 16, 3),
            new Int3(11, 16, 4),
            new Int3(11, 16, 6),
            new Int3(12, 16, 1),
            new Int3(12, 16, 2),
            new Int3(12, 16, 3),
            new Int3(12, 16, 4),
            new Int3(13, 16, 1),
            new Int3(13, 16, 2),
            new Int3(13, 16, 3),
            new Int3(14, 16, 1),
            new Int3(14, 16, 2),
            new Int3(14, 16, 3),
            new Int3(15, 16, 1),
            new Int3(15, 16, 2),
            new Int3(16, 16, 2),
            new Int3(16, 16, 3),
            new Int3(16, 16, 4),
            new Int3(16, 16, 5),
            new Int3(16, 16, 18),
            new Int3(16, 16, 19),
            new Int3(17, 16, 18),
            new Int3(17, 16, 19),
            new Int3(18, 16, 18),
            new Int3(18, 16, 19),
            new Int3(6, 15, 18),
            new Int3(6, 15, 19),
            new Int3(7, 15, 18),
            new Int3(7, 15, 19),
            new Int3(8, 15, 11),
            new Int3(9, 15, 11),
            new Int3(11, 15, 1),
            new Int3(11, 15, 2),
            new Int3(11, 15, 3),
            new Int3(12, 15, 1),
            new Int3(12, 15, 2),
            new Int3(12, 15, 3),
            new Int3(13, 15, 1),
            new Int3(13, 15, 2),
            new Int3(13, 15, 3),
            new Int3(6, 14, 18),
            new Int3(6, 14, 19),
            new Int3(7, 14, 18),
            new Int3(7, 14, 19),
            new Int3(8, 14, 10),
            new Int3(8, 14, 11),
            new Int3(9, 14, 10),
            new Int3(9, 14, 11),
            new Int3(11, 14, 14),
            new Int3(1, 12, 18),
            new Int3(13, 10, 9),
            new Int3(13, 10, 10),
            new Int3(13, 10, 11),
            new Int3(14, 10, 9),
            new Int3(14, 10, 10),
            new Int3(14, 10, 11),
            new Int3(15, 10, 8),
            new Int3(15, 10, 9),
            new Int3(15, 10, 10),
            new Int3(15, 10, 11),
            new Int3(16, 10, 10),
            new Int3(16, 10, 11),
            new Int3(13, 9, 8),
            new Int3(13, 9, 9),
            new Int3(13, 9, 10),
            new Int3(13, 9, 11),
            new Int3(14, 9, 8),
            new Int3(14, 9, 9),
            new Int3(14, 9, 10),
            new Int3(14, 9, 11),
            new Int3(15, 9, 8),
            new Int3(15, 9, 9),
            new Int3(15, 9, 10),
            new Int3(15, 9, 11),
            new Int3(16, 9, 9),
            new Int3(16, 9, 10),
            new Int3(16, 9, 11),
            new Int3(13, 8, 9),
            new Int3(13, 8, 10),
            new Int3(14, 8, 8),
            new Int3(14, 8, 9),
            new Int3(14, 8, 10),
            new Int3(14, 8, 11),
            new Int3(15, 8, 8),
            new Int3(15, 8, 9),
            new Int3(15, 8, 10)
        };

        public int GetChangeset()
        {
            return 49833;
        }

        public IEnumerable<Int3> GetBatches()
        {
            return batches;
        }
    }
}
