using UnityEngine;

namespace AssemblyCSharp.UWE
{
    [CreateAssetMenu(fileName = "WorldEntityData.asset", menuName = "Subnautica/Create WorldEntityData Asset")]
    public class WorldEntityData : ScriptableObject
    {
        public WorldEntityInfo[] infos;
    }
}
