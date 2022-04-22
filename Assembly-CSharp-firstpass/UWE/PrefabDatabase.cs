using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UWE
{
    public static class PrefabDatabase
    {
        public static readonly Dictionary<string, string> prefabFiles = new Dictionary<string, string>();

        private static readonly Dictionary<string, LoadedPrefabRequest> prefabCache = new Dictionary<string, LoadedPrefabRequest>();

        public static void SavePrefabDatabase(string fullFilename)
        {
            using BinaryWriter writer = new BinaryWriter(FileUtils.CreateFile(fullFilename));
            writer.WriteInt32(prefabFiles.Count);
            foreach (KeyValuePair<string, string> prefabFile in prefabFiles)
            {
                if (string.IsNullOrEmpty(prefabFile.Key) || string.IsNullOrEmpty(prefabFile.Value))
                {
                    Debug.LogWarningFormat("Invalid prefab '{0}' at '{1}' in prefab database.", prefabFile.Key, prefabFile.Value);
                }
                else
                {
                    writer.WriteString(prefabFile.Key);
                    writer.WriteString(prefabFile.Value);
                }
            }
        }

        public static void LoadPrefabDatabase(string fullFilename)
        {
            prefabFiles.Clear();
            if (!File.Exists(fullFilename))
            {
                return;
            }
            using FileStream input = File.OpenRead(fullFilename);
            using BinaryReader binaryReader = new BinaryReader(input);
            int num = binaryReader.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                string key = binaryReader.ReadString();
                string value = binaryReader.ReadString();
                prefabFiles[key] = value;
            }
        }

        [Obsolete("Prefer using GetPrefabAsync")]
        public static bool TryGetPrefab(string classId, out GameObject prefab)
        {
            ProfilingUtils.BeginSample("PrefabDatabase.TryGetPrefab");
            try
            {
                if (ScenePrefabDatabase.TryGetScenePrefab(classId, out prefab))
                {
                    return true;
                }
                if (!TryGetPrefabFilename(classId, out var filename))
                {
                    Debug.LogWarningFormat("No filename for prefab {0} in database containing {1} entries", classId, prefabFiles.Count);
                    return false;
                }
                prefab = GetPrefabForFilename(filename);
                if (!prefab)
                {
                    Debug.LogErrorFormat("Could not load prefab {0} at '{1}'", classId, filename);
                    return false;
                }
                return true;
            }
            finally
            {
                ProfilingUtils.EndSample();
            }
        }

        public static IPrefabRequest GetPrefabAsync(string classId)
        {
            if (ScenePrefabDatabase.TryGetScenePrefab(classId, out var prefab))
            {
                return new LoadedPrefabRequest(prefab);
            }
            if (!TryGetPrefabFilename(classId, out var filename))
            {
                Debug.LogWarningFormat("No filename for prefab {0} in database containing {1} entries", classId, prefabFiles.Count);
                return new LoadedPrefabRequest(null);
            }
            return GetPrefabForFilenameAsyncImpl(filename);
        }

        [Obsolete("Prefer using GetPrefabAsync instead.")]
        public static bool TryGetPrefabForFilename(string filename, out GameObject prefab)
        {
            prefab = GetPrefabForFilename(filename);
            return prefab != null;
        }

        [Obsolete("Prefer using GetPrefabAsync instead.")]
        public static GameObject GetPrefabForFilename(string filename)
        {
            GameObject gameObject = Resources.Load<GameObject>(filename);
            if (!gameObject)
            {
                Debug.LogErrorFormat(gameObject, "Failed to load prefab at '{0}'", filename);
            }
            return gameObject;
        }

        [Obsolete("Prefer using GetPrefabAsync by class id instead.")]
        public static IPrefabRequest GetPrefabForFilenameAsync(string filename)
        {
            return GetPrefabForFilenameAsyncImpl(filename);
        }

        private static IPrefabRequest GetPrefabForFilenameAsyncImpl(string filename)
        {
            if (prefabCache.TryGetValue(filename, out var value))
            {
                return value;
            }
            return new SkippableLoadingPrefabRequest(filename);
        }

        public static bool TryGetPrefabFilename(string classId, out string filename)
        {
            if (string.IsNullOrEmpty(classId))
            {
                filename = null;
                return false;
            }
            return prefabFiles.TryGetValue(classId, out filename);
        }

        public static LoadedPrefabRequest AddToCache(string filename, GameObject prefab)
        {
            LoadedPrefabRequest loadedPrefabRequest = new LoadedPrefabRequest(prefab);
            prefabCache[filename] = loadedPrefabRequest;
            return loadedPrefabRequest;
        }

        public static int GetCacheSize()
        {
            return prefabCache.Count;
        }

        public static AsyncOperation UnloadUnusedAssets()
        {
            prefabCache.Clear();
            return Resources.UnloadUnusedAssets();
        }
    }
}
