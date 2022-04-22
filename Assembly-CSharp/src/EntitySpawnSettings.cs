using System;
using UnityEngine;

namespace AssemblyCSharp
{
    public class EntitySpawnSettings : MonoBehaviour
    {
        [Serializable]
        public class Positioning
        {
            public static Positioning defaults = new Positioning();

            public bool isZUp;

            public bool dropToGround = true;

            public bool alignToGround = true;

            public static Positioning CreateDefaultsFor(GameObject prefab)
            {
                return new Positioning
                {
                    isZUp = global::UWE.Utils.IsPrefabZUp(prefab),
                    dropToGround = (prefab.GetComponentsInChildren<Rigidbody>().Length == 0),
                    alignToGround = true
                };
            }
        }

        public Positioning posSettings;
    }
}
