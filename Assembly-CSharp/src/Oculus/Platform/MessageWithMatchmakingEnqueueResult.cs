using System;
using AssemblyCSharp.Oculus.Platform.Models;

namespace AssemblyCSharp.Oculus.Platform
{
    public class MessageWithMatchmakingEnqueueResult : Message<MatchmakingEnqueueResult>
    {
        public MessageWithMatchmakingEnqueueResult(IntPtr c_message)
            : base(c_message)
        {
        }

        protected override MatchmakingEnqueueResult GetDataFromMessage(IntPtr c_message)
        {
            return Message.Deserialize<MatchmakingEnqueueResult>(c_message);
        }

        public override MatchmakingEnqueueResult GetMatchmakingEnqueueResult()
        {
            return base.Data;
        }
    }
}
