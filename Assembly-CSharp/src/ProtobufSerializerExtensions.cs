using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public static class ProtobufSerializerExtensions
    {
        public static void SaveObjectTreeToFile(this ProtobufSerializer serializer, string filePath, GameObject root)
        {
            using Stream stream = FileUtils.CreateFile(filePath);
            serializer.SerializeStreamHeader(stream);
            serializer.SerializeObjectTree(stream, root);
        }

        public static void LoadObjectTreeFromFile(this ProtobufSerializer serializer, string filePath, Action<GameObject> onLoaded, int verbose)
        {
            using (Stream stream = FileUtils.ReadFile(filePath))
            {
                if (serializer.TryDeserializeStreamHeader(stream))
                {
                    GameObject obj = serializer.DeserializeObjectTree(stream, verbose);
                    onLoaded(obj);
                    return;
                }
            }
            Debug.LogErrorFormat("Ignoring exception while fallback-deserializing file '{0}'. Carrying on.", filePath);
            GameObject obj2 = new GameObject("fallback batch root");
            onLoaded(obj2);
        }

        public static byte[] SaveObjectTreeToBytes(this ProtobufSerializer serializer, GameObject root)
        {
            using MemoryStream memoryStream = new MemoryStream();
            serializer.SerializeStreamHeader(memoryStream);
            serializer.SerializeObjectTree(memoryStream, root);
            return memoryStream.ToArray();
        }

        public static void LoadObjectTreeFromBytes(this ProtobufSerializer serializer, byte[] data, Action<GameObject> onLoaded, int verbose)
        {
            using (MemoryStream stream = new MemoryStream(data, writable: false))
            {
                if (serializer.TryDeserializeStreamHeader(stream))
                {
                    GameObject obj = serializer.DeserializeObjectTree(stream, verbose);
                    onLoaded(obj);
                    return;
                }
            }
            Debug.LogError("Exception while fallback-deserializing byte stream.");
            GameObject obj2 = new GameObject("fallback cell root");
            onLoaded(obj2);
        }

        public static GameObject SerializeAndDeserialize(this ProtobufSerializer serializer, GameObject source)
        {
            using MemoryStream memoryStream = new MemoryStream();
            ProfilingUtils.BeginSample("serialize");
            serializer.SerializeObjectTree(memoryStream, source);
            ProfilingUtils.EndSample();
            memoryStream.Seek(0L, SeekOrigin.Begin);
            ProfilingUtils.BeginSample("deserialize");
            GameObject result = serializer.DeserializeObjectTree(memoryStream, 0);
            ProfilingUtils.EndSample();
            return result;
        }

        public static void SerializeAndDeserialize<T>(this ProtobufSerializer serializer, T source, T target)
        {
            using MemoryStream memoryStream = new MemoryStream();
            ProfilingUtils.BeginSample("serialize");
            serializer.Serialize(memoryStream, source);
            ProfilingUtils.EndSample();
            memoryStream.Seek(0L, SeekOrigin.Begin);
            ProfilingUtils.BeginSample("deserialize");
            serializer.Deserialize(memoryStream, target, verbose: false);
            ProfilingUtils.EndSample();
        }

        public static void SerializeObjectTree(this ProtobufSerializer serializer, Stream stream, GameObject root)
        {
            ProfilingUtils.BeginSample("SerializeObjectTree");
            CoroutineUtils.PumpCoroutine(serializer.SerializeObjectTreeAsync(stream, root));
            ProfilingUtils.EndSample();
        }

        public static void SerializeObjects(this ProtobufSerializer serializer, Stream stream, IList<UniqueIdentifier> uids, bool storeParent)
        {
            CoroutineUtils.PumpCoroutine(serializer.SerializeObjectsAsync(stream, uids, storeParent));
        }

        public static GameObject DeserializeObjectTree(this ProtobufSerializer serializer, Stream stream, int verbose)
        {
            CoroutineTask<GameObject> coroutineTask = serializer.DeserializeObjectTreeAsync(stream, forceInactiveRoot: false, verbose);
            CoroutineUtils.PumpCoroutine(coroutineTask);
            return coroutineTask.GetResult();
        }

        public static GameObject DeserializeObjects(this ProtobufSerializer serializer, Stream stream, UniqueIdentifier rootUid, bool forceParent, Transform parent, int verbose)
        {
            CoroutineTask<GameObject> coroutineTask = serializer.DeserializeObjectsAsync(stream, rootUid, forceInactiveRoot: false, forceParent, parent, verbose);
            CoroutineUtils.PumpCoroutine(coroutineTask);
            return coroutineTask.GetResult();
        }

        public static GameObject DeserializeObjectTree(this ProtobufSerializer serializer, Stream stream, UniqueIdentifier uid, int verbose)
        {
            ProfilingUtils.BeginSample("DeserializeObjectTree");
            GameObject gameObject = serializer.DeserializeObjects(stream, uid, forceParent: false, null, verbose);
            ProfilingUtils.EndSample();
            if ((bool)gameObject)
            {
                ProfilingUtils.BeginSample("notify tree");
                if (Application.isPlaying || ProtobufSerializer.IsTestingPlayMode)
                {
                    IProtoTreeEventListener[] componentsInChildren = gameObject.GetComponentsInChildren<IProtoTreeEventListener>(includeInactive: true);
                    foreach (IProtoTreeEventListener protoTreeEventListener in componentsInChildren)
                    {
                        global::UnityEngine.Object @object = protoTreeEventListener as global::UnityEngine.Object;
                        if ((bool)@object)
                        {
                            try
                            {
                                protoTreeEventListener.OnProtoDeserializeObjectTree(serializer);
                            }
                            catch (Exception exception)
                            {
                                Debug.LogException(exception, @object);
                            }
                        }
                    }
                }
                ProfilingUtils.EndSample();
            }
            return gameObject;
        }
    }
}
