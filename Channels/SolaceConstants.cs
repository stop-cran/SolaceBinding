namespace Solace.Channels
{
    static class SolaceConstants
    {
        public const string ReplyToKey = "SolaceReplyTo";
        public const string CorrelationIdKey = "CorrelationId";
        public const string ApplicationMessageTypeKey = "ApplicationMessageType";
        public const string ErrorKey = "error";
        public const string ReplyReaderKey = "ReplyReader";
    }

    public class RequestCorrelationState { }
}
