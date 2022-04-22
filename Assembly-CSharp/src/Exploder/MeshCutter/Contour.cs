using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp.Exploder.MeshCutter
{
    public class Contour
    {
        private struct MidPoint
        {
            public int id;

            public int vertexId;

            public int idNext;

            public int idPrev;
        }

        public List<Dictionary<int, int>> contour;

        private ArrayDictionary<MidPoint> midPoints;

        private LSHash lsHash;

        public int MidPointsCount { get; private set; }

        public Contour(int trianglesNum)
        {
            AllocateBuffers(trianglesNum);
        }

        public void AllocateBuffers(int trianglesNum)
        {
            if (lsHash == null || lsHash.Capacity() < trianglesNum * 2)
            {
                midPoints = new ArrayDictionary<MidPoint>(trianglesNum * 2);
                contour = new List<Dictionary<int, int>>();
                lsHash = new LSHash(0.001f, trianglesNum * 2);
                return;
            }
            lsHash.Clear();
            foreach (Dictionary<int, int> item in contour)
            {
                item.Clear();
            }
            contour.Clear();
            if (midPoints.Size < trianglesNum * 2)
            {
                midPoints = new ArrayDictionary<MidPoint>(trianglesNum * 2);
            }
            else
            {
                midPoints.Clear();
            }
        }

        public void AddTriangle(int triangleID, int id0, int id1, Vector3 v0, Vector3 v1)
        {
            lsHash.Hash(v0, v1, out var hash, out var hash2);
            if (hash == hash2)
            {
                return;
            }
            MidPoint data;
            if (midPoints.TryGetValue(hash, out var value))
            {
                if (value.idNext == int.MaxValue && value.idPrev != hash2)
                {
                    value.idNext = hash2;
                }
                else if (value.idPrev == int.MaxValue && value.idNext != hash2)
                {
                    value.idPrev = hash2;
                }
                midPoints[hash] = value;
            }
            else
            {
                ArrayDictionary<MidPoint> arrayDictionary = midPoints;
                int key = hash;
                data = new MidPoint
                {
                    id = hash,
                    vertexId = id0,
                    idNext = hash2,
                    idPrev = int.MaxValue
                };
                arrayDictionary.Add(key, data);
            }
            if (midPoints.TryGetValue(hash2, out value))
            {
                if (value.idNext == int.MaxValue && value.idPrev != hash)
                {
                    value.idNext = hash;
                }
                else if (value.idPrev == int.MaxValue && value.idNext != hash)
                {
                    value.idPrev = hash;
                }
                midPoints[hash2] = value;
            }
            else
            {
                ArrayDictionary<MidPoint> arrayDictionary2 = midPoints;
                int key2 = hash2;
                data = new MidPoint
                {
                    id = hash2,
                    vertexId = id1,
                    idPrev = hash,
                    idNext = int.MaxValue
                };
                arrayDictionary2.Add(key2, data);
            }
            MidPointsCount = midPoints.Count;
        }

        public bool FindContours()
        {
            if (midPoints.Count == 0)
            {
                return false;
            }
            Dictionary<int, int> dictionary = new Dictionary<int, int>(midPoints.Count);
            int num = midPoints.Count * 2;
            MidPoint firstValue = midPoints.GetFirstValue();
            dictionary.Add(firstValue.id, firstValue.vertexId);
            midPoints.Remove(firstValue.id);
            int num2 = firstValue.idNext;
            while (midPoints.Count > 0)
            {
                if (num2 == int.MaxValue)
                {
                    return false;
                }
                if (!midPoints.TryGetValue(num2, out var value))
                {
                    contour.Clear();
                    return false;
                }
                dictionary.Add(value.id, value.vertexId);
                midPoints.Remove(value.id);
                if (dictionary.ContainsKey(value.idNext))
                {
                    if (dictionary.ContainsKey(value.idPrev))
                    {
                        contour.Add(new Dictionary<int, int>(dictionary));
                        dictionary.Clear();
                        if (midPoints.Count == 0)
                        {
                            break;
                        }
                        firstValue = midPoints.GetFirstValue();
                        dictionary.Add(firstValue.id, firstValue.vertexId);
                        midPoints.Remove(firstValue.id);
                        num2 = firstValue.idNext;
                        continue;
                    }
                    num2 = value.idPrev;
                }
                else
                {
                    num2 = value.idNext;
                }
                num--;
                if (num == 0)
                {
                    contour.Clear();
                    return false;
                }
            }
            return true;
        }
    }
}
