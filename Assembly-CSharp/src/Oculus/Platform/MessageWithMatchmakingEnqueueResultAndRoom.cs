using System;
using AssemblyCSharp.Oculus.Platform.Models;

namespace AssemblyCSharp.Oculus.Platform
{
    public class MessageWithMatchmakingEnqueueResultAndRoom : Message<MatchmakingEnqueueResultAndRoom>
    {
        public MessageWithMatchmakingEnqueueResultAndRoom(IntPtr c_message)
            : base(c_message)
        {
        }

        protected override MatchmakingEnqueueResultAndRoom GetDataFromMessage(IntPtr c_message)
        {
            return Message.Deserialize<MatchmakingEnqueueResultAndRoom>(c_message);
        }

        public override MatchmakingEnqueueResultAndRoom GetMatchmakingEnqueueResultAndRoom()
        {
            return base.Data;
        }
    }
}
