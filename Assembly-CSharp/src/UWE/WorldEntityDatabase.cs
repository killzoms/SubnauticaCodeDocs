using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp.UWE
{
    public class WorldEntityDatabase
    {
        private static WorldEntityDatabase _main;

        private const string dataPath = "WorldEntities/WorldEntityData";

        public readonly Dictionary<string, WorldEntityInfo> infos = new Dictionary<string, WorldEntityInfo>();

        public static WorldEntityDatabase main
        {
            get
            {
                if (_main == null)
                {
                    _main = new WorldEntityDatabase();
                }
                return _main;
            }
        }

        public static bool TryGetInfo(string classId, out WorldEntityInfo info)
        {
            return main.infos.TryGetValue(classId, out info);
        }

        public WorldEntityDatabase()
        {
            WorldEntityData worldEntityData = Resources.Load<WorldEntityData>("WorldEntities/WorldEntityData");
            if (!worldEntityData)
            {
                Debug.LogErrorFormat("Failed to load WorldEntityData at '{0}'", "WorldEntities/WorldEntityData");
                return;
            }
            for (int i = 0; i < worldEntityData.infos.Length; i++)
            {
                WorldEntityInfo worldEntityInfo = worldEntityData.infos[i];
                infos[worldEntityInfo.classId] = worldEntityInfo;
            }
        }
    }
}
