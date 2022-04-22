using System;
using AssemblyCSharp.Oculus.Platform.Models;

namespace AssemblyCSharp.Oculus.Platform
{
    public class MessageWithPurchase : Message<Purchase>
    {
        public MessageWithPurchase(IntPtr c_message)
            : base(c_message)
        {
        }

        protected override Purchase GetDataFromMessage(IntPtr c_message)
        {
            return Message.Deserialize<Purchase>(c_message);
        }

        public override Purchase GetPurchase()
        {
            return base.Data;
        }
    }
}
