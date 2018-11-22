namespace Solace.Channels
{
    static class SolaceConstants
    {
        public const string ReplyToKey = "SolaceReplyTo";
        public const string CorrelationIdKey = "CorrelationId";
        public const string ApplicationMessageTypeKey = "ApplicationMessageType";
        public const string SenderIdKey = "SenderId";
        public const string ErrorKey = "error";
        public const string ReplyReaderKey = "ReplyReader";
        public const string TopicSuffixKey = "TopicSuffix";
        public const string IsOneWayKey = "IsOneWay";
        public const string SdtStreamContainerKey = "SdtStreamContainer";
    }

    public class RequestCorrelationState { }
}
