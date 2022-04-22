namespace rail
{
    public enum ChannelExceptionType
    {
        kExceptionNone,
        kExceptionLocalNetworkError,
        kExceptionRelayAddressFailed,
        kExceptionNegotiationRequestFailed,
        kExceptionNegotiationResponseFailed,
        kExceptionNegotiationResponseDataInvalid,
        kExceptionNegotiationResponseTimeout,
        kExceptionRelayServerOverload,
        kExceptionRelayServerInternalError,
        kExceptionRelayChannelUserFull,
        kExceptionRelayChannelNotFound,
        kExceptionRelayChannelEndByServer
    }
}
