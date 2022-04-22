using System.IO;

namespace AssemblyCSharp
{
    public static class DeserializationUtils
    {
        public static void DeserializeObjects(ProtobufSerializer serializer, Stream stream, IDeserializationListener listener, int verbose)
        {
            ProtobufSerializer.LoopHeader loopHeader = new ProtobufSerializer.LoopHeader();
            loopHeader.Reset();
            serializer.Deserialize(stream, loopHeader, verbose > 3);
            int count = loopHeader.Count;
            ProtobufSerializer.GameObjectData gameObjectData = new ProtobufSerializer.GameObjectData();
            ProtobufSerializer.LoopHeader loopHeader2 = new ProtobufSerializer.LoopHeader();
            ProtobufSerializer.ComponentHeader componentHeader = new ProtobufSerializer.ComponentHeader();
            for (int i = 0; i < count; i++)
            {
                ProfilingUtils.BeginSample("game object");
                gameObjectData.Reset();
                serializer.Deserialize(stream, gameObjectData, verbose > 0);
                listener.OnGameObject(gameObjectData);
                ProfilingUtils.EndSample();
                loopHeader2.Reset();
                serializer.Deserialize(stream, loopHeader2, verbose > 3);
                int count2 = loopHeader2.Count;
                ProfilingUtils.BeginSample("components");
                for (int j = 0; j < count2; j++)
                {
                    componentHeader.Reset();
                    serializer.Deserialize(stream, componentHeader, verbose > 1);
                    if (!string.IsNullOrEmpty(componentHeader.TypeName))
                    {
                        listener.OnComponent(componentHeader);
                        serializer.SkipDeserialize(stream);
                    }
                }
                ProfilingUtils.EndSample();
            }
        }
    }
}
