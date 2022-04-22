using System;
using ProtoBuf.Compiler;
using ProtoBuf.Meta;

namespace ProtoBuf.Serializers
{
    internal sealed class Int32Serializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(int);

        public Type ExpectedType => expectedType;

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        public Int32Serializer(TypeModel model)
        {
        }

        public object Read(object value, ProtoReader source)
        {
            return source.ReadInt32();
        }

        public void Write(object value, ProtoWriter dest)
        {
            ProfilingUtils.BeginSample("ProtoWriter.WriteInt32");
            ProtoWriter.WriteInt32((int)value, dest);
            ProfilingUtils.EndSample();
        }

        void IProtoSerializer.EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteInt32", valueFrom);
        }

        void IProtoSerializer.EmitRead(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitBasicRead("ReadInt32", ExpectedType);
        }
    }
}
