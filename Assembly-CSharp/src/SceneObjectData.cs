using System;
using System.IO;
using ProtoBuf;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class SceneObjectData
    {
        private const int currentVersion = 2;

        [NonSerialized]
        [ProtoMember(1)]
        public int version;

        [NonSerialized]
        [ProtoMember(2)]
        public string uniqueName;

        [NonSerialized]
        [ProtoMember(3, OverwriteList = true)]
        public byte[] serialData;

        [NonSerialized]
        [ProtoMember(4)]
        public bool isObjectTree;

        public void SerializeFrom(ProtobufSerializer serializer, SceneObjectIdentifier id)
        {
            using MemoryStream memoryStream = new MemoryStream();
            bool serializeObjectTree = id.serializeObjectTree;
            if (serializeObjectTree)
            {
                serializer.SerializeObjectTree(memoryStream, id.gameObject);
            }
            else
            {
                serializer.SerializeGameObject(memoryStream, id, storeParent: false);
            }
            version = 2;
            uniqueName = id.uniqueName;
            serialData = memoryStream.ToArray();
            isObjectTree = serializeObjectTree;
        }

        public void DeserializeInto(ProtobufSerializer serializer, SceneObjectIdentifier id)
        {
            using MemoryStream stream = new MemoryStream(serialData);
            if (version < 2 || !isObjectTree)
            {
                serializer.DeserializeGameObject(stream, id, forceParent: true, id.transform.parent, 0);
            }
            else
            {
                serializer.DeserializeObjectTree(stream, id, 0);
            }
        }
    }
}
