using System;
using AssemblyCSharp.Oculus.Platform.Models;

namespace AssemblyCSharp.Oculus.Platform
{
    public class MessageWithAchievementDefinitionList : Message<AchievementDefinitionList>
    {
        public MessageWithAchievementDefinitionList(IntPtr c_message)
            : base(c_message)
        {
        }

        protected override AchievementDefinitionList GetDataFromMessage(IntPtr c_message)
        {
            return Message.Deserialize<AchievementDefinitionList>(c_message);
        }

        public override AchievementDefinitionList GetAchievementDefinitions()
        {
            return base.Data;
        }
    }
}
