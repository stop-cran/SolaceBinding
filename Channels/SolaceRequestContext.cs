using System;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using SolaceSystems.Solclient.Messaging;

namespace Solace.Channels
{
    class SolaceRequestContext : RequestContext
    {
        SolaceReplyChannel replyChannel;
        Message requestMessage;
        IDestination replyTo;
        string correlationId;
        TimeSpan timeout;

        public SolaceRequestContext(SolaceReplyChannel replyChannel, Message requestMessage, TimeSpan timeout)
        {
            this.replyChannel = replyChannel;
            this.requestMessage = requestMessage;
            this.timeout = timeout;

            replyTo = (IDestination)requestMessage.Properties.TryGetValue(SolaceConstants.ReplyToKey);

            if (replyTo != null)
                correlationId = (string)requestMessage.Properties[SolaceConstants.CorrelationIdKey];
        }

        public override void Abort()
        {
            this.replyChannel.Abort();
        }

        public override IAsyncResult BeginReply(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return replyTo == null ? Task.CompletedTask : TaskHelper.CreateTask(() => this.replyChannel.SendReply(replyTo, correlationId, message, timeout), callback, state);
        }

        public override IAsyncResult BeginReply(Message message, AsyncCallback callback, object state)
        {
            return replyTo == null ? Task.CompletedTask : TaskHelper.CreateTask(() => this.replyChannel.SendReply(replyTo, correlationId, message, timeout), callback, state);
        }

        public override void Close(TimeSpan timeout)
        {
            this.replyChannel.Close(timeout);
        }

        public override void Close()
        {
            this.replyChannel.Close();
        }

        public override void EndReply(IAsyncResult result)
        {
            ((Task)result).Wait();
        }

        public override void Reply(Message message, TimeSpan timeout)
        {
            if (replyTo != null)
                this.replyChannel.SendReply(replyTo, correlationId, message, timeout);
        }

        public override void Reply(Message message)
        {
            if (replyTo != null)
                this.replyChannel.SendReply(replyTo, correlationId, message, timeout);
        }

        public override Message RequestMessage
        {
            get { return this.requestMessage; }
        }
    }
}
