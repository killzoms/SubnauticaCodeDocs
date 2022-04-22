using System;
using UnityEngine;

namespace UWE
{
    public class HeightmapBoundsCache
    {
        public delegate float HeightFunc(Int2 xz);

        private int cellSize = 32;

        private HeightFunc heightFunc;

        private bool[,] cell2computed;

        private float[,] cell2min;

        private float[,] cell2max;

        public HeightmapBoundsCache(int cellSize, Int2 numCells, HeightFunc hfunc)
        {
            this.cellSize = cellSize;
            heightFunc = hfunc;
            cell2computed = new bool[numCells.x, numCells.y];
            cell2min = new float[numCells.x, numCells.y];
            cell2max = new float[numCells.x, numCells.y];
        }

        public void Reset()
        {
            lock (this)
            {
                Array.Clear(cell2computed, 0, cell2computed.Length);
            }
        }

        private void UpdateBounds(Int2 cell)
        {
            if (!cell2computed.Get(cell))
            {
                float num = float.MinValue;
                float num2 = float.MaxValue;
                Int2.RangeEnumerator enumerator = Int2.Range(cell * cellSize, (cell + 1) * cellSize - 1).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Int2 current = enumerator.Current;
                    float a = heightFunc(current);
                    num = Mathf.Max(a, num);
                    num2 = Mathf.Min(a, num2);
                }
                cell2min.Set(cell, num2);
                cell2max.Set(cell, num);
                cell2computed.Set(cell, val: true);
            }
        }

        public float GetMin(Int2 cell)
        {
            lock (this)
            {
                UpdateBounds(cell);
                return cell2min.Get(cell);
            }
        }

        public float GetMax(Int2 cell)
        {
            lock (this)
            {
                UpdateBounds(cell);
                return cell2max.Get(cell);
            }
        }
    }
}
