using System;

namespace AssemblyCSharp.Oculus.Platform
{
    public class MessageWithString : Message<string>
    {
        public MessageWithString(IntPtr c_message)
            : base(c_message)
        {
        }

        protected override string GetDataFromMessage(IntPtr c_message)
        {
            return CAPI.ovr_Message_GetString(c_message);
        }

        public override string GetString()
        {
            return base.Data;
        }
    }
}
