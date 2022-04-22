using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AssemblyCSharp
{
    public class CollectionsTest : MonoBehaviour
    {
        private void Start()
        {
            List<int> list = Enumerable.Range(0, 100).ToList();
            int[] entries = list.ToArray();
            HashSet<int> entries2 = new HashSet<int>(list);
            Dictionary<int, int> entries3 = list.ToDictionary((int p) => p);
            HashSet<Int3> hashSet = new HashSet<Int3>();
            hashSet.Add(Int3.zero);
            hashSet.Add(Int3.one);
            hashSet.Add(Int3.xUnit);
            hashSet.Add(Int3.yUnit);
            hashSet.Add(Int3.zUnit);
            Dictionary<Int3, int> dictionary = new Dictionary<Int3, int>();
            dictionary.Add(Int3.zero, 0);
            dictionary.Add(Int3.one, 1);
            dictionary.Add(Int3.xUnit, 2);
            dictionary.Add(Int3.yUnit, 3);
            dictionary.Add(Int3.zUnit, 4);
            StartCoroutine(ArrayForEach(entries));
            StartCoroutine(ListForEach(list));
            StartCoroutine(HashSetForEach(entries2));
            StartCoroutine(HashSetContains(entries2));
            StartCoroutine(DictionaryForEach(entries3));
            StartCoroutine(DictionaryContains(entries3));
            StartCoroutine(HashSetForEach(hashSet));
            StartCoroutine(HashSetContains(hashSet));
            StartCoroutine(DictionaryForEach(dictionary));
            StartCoroutine(DictionaryContains(dictionary));
        }

        private IEnumerator ArrayForEach(int[] entries)
        {
            int sum = 0;
            while (true)
            {
                foreach (int num in entries)
                {
                    sum += num;
                }
                yield return null;
            }
        }

        private IEnumerator ListForEach(List<int> entries)
        {
            int sum = 0;
            while (true)
            {
                foreach (int entry in entries)
                {
                    sum += entry;
                }
                yield return null;
            }
        }

        private IEnumerator HashSetForEach(HashSet<int> entries)
        {
            int sum = 0;
            while (true)
            {
                foreach (int entry in entries)
                {
                    sum += entry;
                }
                yield return null;
            }
        }

        private IEnumerator HashSetContains(HashSet<int> entries)
        {
            bool result2 = true;
            while (true)
            {
                result2 ^= entries.Contains(1);
                result2 ^= entries.Contains(-1);
                yield return null;
            }
        }

        private IEnumerator DictionaryForEach(Dictionary<int, int> entries)
        {
            int sum = 0;
            while (true)
            {
                foreach (KeyValuePair<int, int> entry in entries)
                {
                    sum += entry.Key;
                }
                yield return null;
            }
        }

        private IEnumerator DictionaryContains(Dictionary<int, int> entries)
        {
            bool result2 = true;
            while (true)
            {
                result2 ^= entries.ContainsKey(1);
                result2 ^= entries.ContainsKey(-1);
                yield return null;
            }
        }

        private IEnumerator HashSetForEach(HashSet<Int3> entries)
        {
            Int3 sum = Int3.zero;
            while (true)
            {
                foreach (Int3 entry in entries)
                {
                    sum += entry;
                }
                yield return null;
            }
        }

        private IEnumerator HashSetContains(HashSet<Int3> entries)
        {
            bool result2 = true;
            Int3 unseen = new Int3(2, 2, 2);
            while (true)
            {
                result2 ^= entries.Contains(Int3.one);
                result2 ^= entries.Contains(unseen);
                yield return null;
            }
        }

        private IEnumerator DictionaryForEach(Dictionary<Int3, int> entries)
        {
            Int3 sum = Int3.zero;
            while (true)
            {
                foreach (KeyValuePair<Int3, int> entry in entries)
                {
                    sum += entry.Key;
                }
                yield return null;
            }
        }

        private IEnumerator DictionaryContains(Dictionary<Int3, int> entries)
        {
            bool result2 = true;
            Int3 unseen = new Int3(2, 2, 2);
            while (true)
            {
                result2 ^= entries.ContainsKey(Int3.one);
                result2 ^= entries.ContainsKey(unseen);
                yield return null;
            }
        }
    }
}
