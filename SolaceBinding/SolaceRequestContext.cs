using SolaceSystems.Solclient.Messaging;
using System;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

namespace Solace.Channels
{
    internal class SolaceRequestContext : RequestContext
    {
        private readonly SolaceReplyChannel replyChannel;
        private readonly IDestination replyTo;
        private readonly string correlationId;
        private readonly TimeSpan timeout;

        public SolaceRequestContext(SolaceReplyChannel replyChannel, Message requestMessage, TimeSpan timeout)
        {
            this.replyChannel = replyChannel;
            RequestMessage = requestMessage;
            this.timeout = timeout;

            replyTo = (IDestination)requestMessage.Properties.TryGetValue(SolaceConstants.ReplyToKey);

            if (replyTo != null)
                correlationId = (string)requestMessage.Properties[SolaceConstants.CorrelationIdKey];
        }

        public override void Abort() => replyChannel.Abort();

        public override IAsyncResult BeginReply(Message message, TimeSpan timeout, AsyncCallback callback, object state) =>
            BeginReply(message, callback, state);

        public override IAsyncResult BeginReply(Message message, AsyncCallback callback, object state) =>
            replyTo == null ? Task.CompletedTask : TaskHelper.CreateTask(() => replyChannel.SendReply(replyTo, correlationId, message, timeout), callback, state);

        public override void Close(TimeSpan timeout) =>
            replyChannel.Close(timeout);

        public override void Close() =>
            replyChannel.Close();

        public override void EndReply(IAsyncResult result) =>
            ((Task)result).Wait();

        public override void Reply(Message message, TimeSpan timeout)
        {
            if (replyTo != null)
                replyChannel.SendReply(replyTo, correlationId, message, timeout);
        }

        public override void Reply(Message message)
        {
            if (replyTo != null)
                replyChannel.SendReply(replyTo, correlationId, message, timeout);
        }

        public override Message RequestMessage { get; }
    }
}
