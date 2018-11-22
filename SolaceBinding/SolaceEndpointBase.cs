using SolaceSystems.Solclient.Messaging;
using System;

namespace Solace.Channels
{
    internal abstract class SolaceEndpointBase : ISolaceEndpoint
    {
        private readonly object sendRequestLock = new object();

        public SolaceEndpointBase(Uri address)
        {
            RemoteEndpoint = address;
            SubscribedTopic = address.AbsolutePath.Replace("%3E", ">").TrimStart('/');
            Topic = ContextFactory.Instance.CreateTopic(SubscribedTopic);
        }

        protected ITopic Topic { get; }

        protected ISession Session { get; set; }

        public Uri RemoteEndpoint { get; }

        public string SubscribedTopic { get; }

        public bool Connected { get; private set; }

        public abstract ISolaceEndpoint Accept();

        public void Connect()
        {
            Session.Connect()
                .EnsureSuccess("Failed to connect session", Session.Properties);

            Connected = true;
        }

        public void Close() =>
            Close(Session.Properties.ConnectTimeoutInMsecs);

        public virtual void Close(int timeout)
        {
        }

        public abstract void Listen();

        private ITopic CreateTopicWithSuffix(string suffix)
        {
            if (!SubscribedTopic.EndsWith("/>") && SubscribedTopic != ">")
                throw new InvalidOperationException("To be able to apply TopicSuffix RemoteEndPoint should end with \"/>\"");

            return ContextFactory.Instance.CreateTopic(SubscribedTopic.Substring(0, SubscribedTopic.Length - 1) + suffix);
        }

        public void SendReply(IDestination destination, string correlationId, string applicationMessageType, byte[] buffer)
        {
            if (buffer != null)
                using (var message = Session.CreateMessage())
                using (var message2 = Session.CreateMessage())
                {
                    message.ReplyTo = destination;
                    message.CorrelationId = correlationId;
                    message2.BinaryAttachment = buffer;
                    message2.ApplicationMessageType = applicationMessageType;

                    Session.SendReply(message, message2)
                        .EnsureSuccess("Failed to connect reply session", Session.Properties);
                }
        }

        public IMessage SendRequest(byte[] buffer, string applicationMessageType, string senderId, string topicSuffix, TimeSpan timeout)
        {
            IMessage result;

            using (var topic = string.IsNullOrEmpty(topicSuffix) ? null : CreateTopicWithSuffix(topicSuffix))
            using (var message = Session.CreateMessage())
            {
                message.SenderId = senderId ?? Session.Properties.ClientName;
                message.Destination = topic ?? this.Topic;
                message.BinaryAttachment = buffer;
                message.ApplicationMessageType = applicationMessageType;
                int num = (int)timeout.TotalMilliseconds;

                lock (sendRequestLock)
                    Session.SendRequest(
                        message,
                        out result,
                        (num == 0 || num == -1) ? 3600000 : num)
                        .EnsureSuccess($"Failed to send request to {message.Destination}", Session.Properties);
            }

            return result;
        }

        public void Send(byte[] buffer, string correlationId, string applicationMessageType,
            string senderId, string topicSuffix)
        {
            using (var topic = string.IsNullOrEmpty(topicSuffix) ? null : CreateTopicWithSuffix(topicSuffix))
            using (var message = Session.CreateMessage())
            {
                message.SenderId = senderId ?? Session.Properties.ClientName;
                message.Destination = topic ?? this.Topic;
                message.BinaryAttachment = buffer;
                message.ApplicationMessageType = applicationMessageType;

                if (correlationId != null)
                    message.CorrelationId = correlationId;

                lock (sendRequestLock)
                    Session.Send(message).EnsureSuccess($"Failed to send message to {message.Destination}", Session.Properties);
            }
        }

        public void SendReply(IDestination destination, string correlationId,
            string applicationMessageType, ArraySegment<byte> buffer) =>
            SendReply(destination, correlationId, applicationMessageType, SolaceEndpointBase.Copy(buffer));

        private static byte[] Copy(ArraySegment<byte> buffer)
        {
            byte[] array;

            if (buffer.Array == null || (buffer.Offset == 0 && buffer.Count == buffer.Array.Length))
                array = buffer.Array;
            else
            {
                array = new byte[buffer.Count];
                Buffer.BlockCopy(buffer.Array, buffer.Offset, array, 0, buffer.Count);
            }

            return array;
        }

        public IMessage SendRequest(ArraySegment<byte> buffer, string applicationMessageType, string senderId, string topicSuffix, TimeSpan timeout) =>
            SendRequest(Copy(buffer), applicationMessageType, senderId, topicSuffix, timeout);

        public void Send(ArraySegment<byte> buffer, string correlationId, string applicationMessageType, string senderId, string topicSuffix) =>
            Send(Copy(buffer), correlationId, applicationMessageType, senderId, topicSuffix);

        public abstract IMessage Receive(TimeSpan timeout);

        public abstract IMessage Receive();
    }
}
