using System.Collections.Generic;
using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    [SuppressMessage("Subnautica.Rules", "AvoidDoubleInitializationRule")]
    public class PooledMaterialList
    {
        public List<Material> materials = new List<Material>();
    }
}
