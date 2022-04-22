using System;
using AssemblyCSharp.Oculus.Platform.Models;
using Oculus.Newtonsoft.Json;

namespace AssemblyCSharp.Oculus.Platform
{
    public class MessageWithMatchMadeRoom : Message<Room>
    {
        private class MatchMadeRoom
        {
            [JsonProperty("matchmade_room")]
            public Room Room;
        }

        public MessageWithMatchMadeRoom(IntPtr c_message)
            : base(c_message)
        {
        }

        protected override Room GetDataFromMessage(IntPtr c_message)
        {
            return Message.Deserialize<MatchMadeRoom>(c_message).Room;
        }

        public override Room GetRoom()
        {
            return base.Data;
        }
    }
}
