using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class SystemsSpawner : MonoBehaviour
    {
        private static Dictionary<GameObject, GameObject> spawnedSingletons = new Dictionary<GameObject, GameObject>();

        public GameObject singletonPrefab;

        private void Awake()
        {
            Object.DontDestroyOnLoad(base.gameObject);
            if (spawnedSingletons.TryGetValue(singletonPrefab, out var value))
            {
                Object.DestroyObject(base.gameObject);
                return;
            }
            value = Object.Instantiate(singletonPrefab, Vector3.zero, Quaternion.identity);
            Object.DontDestroyOnLoad(value);
            value.transform.parent = base.transform;
            spawnedSingletons.Add(singletonPrefab, value);
        }
    }
}
