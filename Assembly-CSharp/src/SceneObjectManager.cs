using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssemblyCSharp
{
    public class SceneObjectManager : MonoBehaviour
    {
        private static SceneObjectManager instance;

        private Dictionary<string, SceneObjectIdentifier> registry = new Dictionary<string, SceneObjectIdentifier>();

        private readonly SceneObjectDataSet dataSet = new SceneObjectDataSet();

        private readonly ProtobufSerializer serializer = new ProtobufSerializer();

        public static SceneObjectManager Instance
        {
            get
            {
                if ((bool)instance)
                {
                    return instance;
                }
                instance = Object.FindObjectOfType<SceneObjectManager>();
                if ((bool)instance)
                {
                    return instance;
                }
                instance = new GameObject("Scene Object Manager").AddComponent<SceneObjectManager>();
                return instance;
            }
        }

        public void Register(SceneObjectIdentifier id)
        {
            if (registry.TryGetValue(id.uniqueName, out var value))
            {
                Debug.LogWarning("registering scene object " + id.name + " but unique name " + id.uniqueName + " is already registered to " + value.name, id);
                Debug.LogWarning("unregistering scene object " + value.name, value);
            }
            registry[id.uniqueName] = id;
            dataSet.TryDeserialize(serializer, id);
        }

        public void Unregister(SceneObjectIdentifier id)
        {
            registry.Remove(id.uniqueName);
        }

        public void Save(Stream stream)
        {
            foreach (KeyValuePair<string, SceneObjectIdentifier> item in registry)
            {
                if (!item.Value)
                {
                    Debug.LogErrorFormat(this, "destroyed/null item in registry for key '{0}'", item.Key);
                }
                else
                {
                    dataSet.Serialize(serializer, item.Value);
                }
            }
            serializer.Serialize(stream, dataSet);
        }

        public void Load(Stream stream)
        {
            dataSet.Reset();
            serializer.Deserialize(stream, dataSet, verbose: false);
            foreach (KeyValuePair<string, SceneObjectIdentifier> item in registry)
            {
                dataSet.TryDeserialize(serializer, item.Value);
            }
        }

        public void OnLoaded()
        {
            foreach (KeyValuePair<string, SceneObjectIdentifier> item in registry)
            {
                item.Value.BroadcastMessage("OnSceneObjectsLoaded", SendMessageOptions.DontRequireReceiver);
            }
        }

        public IEnumerable<SceneObjectIdentifier> Registry()
        {
            return registry.Values;
        }

        public IEnumerable<SceneObjectData> DataSet()
        {
            return dataSet.Items();
        }
    }
}
