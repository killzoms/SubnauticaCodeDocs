using System;
using AssemblyCSharp.Oculus.Platform.Models;

namespace AssemblyCSharp.Oculus.Platform
{
    public class MessageWithPurchaseList : Message<PurchaseList>
    {
        public MessageWithPurchaseList(IntPtr c_message)
            : base(c_message)
        {
        }

        protected override PurchaseList GetDataFromMessage(IntPtr c_message)
        {
            return Message.Deserialize<PurchaseList>(c_message);
        }

        public override PurchaseList GetPurchaseList()
        {
            return base.Data;
        }
    }
}
