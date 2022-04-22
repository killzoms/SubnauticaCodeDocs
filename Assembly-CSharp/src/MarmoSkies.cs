using System.Collections.Generic;
using AssemblyCSharp.mset;
using UnityEngine;

namespace AssemblyCSharp
{
    public class MarmoSkies : MonoBehaviour
    {
        public static MarmoSkies main;

        public GameObject skySafeShallowsPrefab;

        public GameObject skyBaseInteriorPrefab;

        public GameObject skyBaseGlassPrefab;

        public GameObject skyExplorableWreckPrefab;

        private Dictionary<GameObject, Sky> skies = new Dictionary<GameObject, Sky>();

        private Sky skySafeShallow;

        private Sky skyBaseInterior;

        private Sky skyBaseGlass;

        private Sky skyExplorableWreck;

        private void Awake()
        {
            main = this;
        }

        private void Start()
        {
            skySafeShallow = GetSky(skySafeShallowsPrefab);
            skyBaseInterior = GetSky(skyBaseInteriorPrefab);
            skyBaseGlass = GetSky(skyBaseGlassPrefab);
            skyExplorableWreck = GetSky(skyExplorableWreckPrefab);
        }

        public Sky GetSky(GameObject skyPrefab)
        {
            if (skyPrefab == null)
            {
                return null;
            }
            if (!skies.TryGetValue(skyPrefab, out var value))
            {
                GameObject obj = Object.Instantiate(skyPrefab);
                obj.transform.SetParent(base.transform);
                value = obj.GetComponent<Sky>();
                skies.Add(skyPrefab, value);
            }
            return value;
        }

        public Sky GetSky(Skies sky)
        {
            return sky switch
            {
                Skies.SafeShallow => skySafeShallow, 
                Skies.BaseInterior => skyBaseInterior, 
                Skies.BaseGlass => skyBaseGlass, 
                Skies.ExplorableWreck => skyExplorableWreck, 
                _ => null, 
            };
        }
    }
}
