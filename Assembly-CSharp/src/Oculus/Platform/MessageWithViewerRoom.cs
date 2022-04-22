using System;
using AssemblyCSharp.Oculus.Platform.Models;
using Oculus.Newtonsoft.Json;

namespace AssemblyCSharp.Oculus.Platform
{
    public class MessageWithViewerRoom : Message<Room>
    {
        private class ViewerRoom
        {
            [JsonProperty("viewer_room")]
            public Room Room;
        }

        public MessageWithViewerRoom(IntPtr c_message)
            : base(c_message)
        {
        }

        protected override Room GetDataFromMessage(IntPtr c_message)
        {
            return Message.Deserialize<ViewerRoom>(c_message).Room;
        }

        public override Room GetRoom()
        {
            return base.Data;
        }
    }
}
