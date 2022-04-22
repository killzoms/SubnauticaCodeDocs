using System;
using System.Collections.Generic;
using ProtoBuf;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class SceneObjectDataSet
    {
        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 1;

        [NonSerialized]
        [ProtoMember(2)]
        public readonly Dictionary<string, SceneObjectData> items = new Dictionary<string, SceneObjectData>();

        public void Reset()
        {
            items.Clear();
        }

        public void Serialize(ProtobufSerializer serializer, SceneObjectIdentifier id)
        {
            SceneObjectData sceneObjectData = new SceneObjectData();
            sceneObjectData.SerializeFrom(serializer, id);
            items[sceneObjectData.uniqueName] = sceneObjectData;
        }

        public bool TryDeserialize(ProtobufSerializer serializer, SceneObjectIdentifier id)
        {
            if (items.TryGetValue(id.uniqueName, out var value))
            {
                value.DeserializeInto(serializer, id);
                return true;
            }
            return false;
        }

        public IEnumerable<SceneObjectData> Items()
        {
            return items.Values;
        }
    }
}
