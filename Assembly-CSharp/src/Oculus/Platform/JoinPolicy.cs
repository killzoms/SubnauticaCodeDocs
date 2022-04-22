using System.ComponentModel;

namespace AssemblyCSharp.Oculus.Platform
{
    public enum JoinPolicy : uint
    {
        [Description("NONE")]
        None,
        [Description("EVERYONE")]
        Everyone,
        [Description("FRIENDS_OF_MEMBERS")]
        FriendsOfMembers,
        [Description("FRIENDS_OF_OWNER")]
        FriendsOfOwner,
        [Description("INVITED_USERS")]
        InvitedUsers
    }
}
