using Newtonsoft.Json.Linq;
using Solace.Channels.MessageConverters;
using SolaceSystems.Solclient.Messaging;
using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace Solace.Channels
{
    internal class SolaceBaseChannel : ChannelBase
    {
        private const int maxBufferSize = 1024 * 1024;

        private ISolaceEndpoint endpoint;
        private MessageEncoder encoder;
        private BufferManager bufferManager;
        private readonly IEnumerable<IMessageConverter> converters;

        public SolaceBaseChannel(MessageEncoder encoder, BufferManager bufferManager, ChannelManagerBase channelManager, IEnumerable<IMessageConverter> converters)
            : base(channelManager)
        {
            this.encoder = encoder;
            this.bufferManager = bufferManager;
            this.converters = converters;
        }

        protected virtual void InitializeSolaceEndpoint(ISolaceEndpoint endpoint)
        {
            if (this.endpoint != null)
                throw new InvalidOperationException("SolaceEndpoint is already set");

            this.endpoint = endpoint;
        }

        private Message SendEncodedRequestMessage(ArraySegment<byte> encodedBytes, string applicationMessageType, string senderId, string topicSuffix, TimeSpan timeout)
        {
            ThrowIfDisposedOrNotOpen();

            try
            {
                var message = endpoint.SendRequest(encodedBytes, applicationMessageType, senderId, topicSuffix, timeout);

                return DecodeMessage(message);
            }
            finally
            {
                if (encodedBytes.Array != null)
                    bufferManager.ReturnBuffer(encodedBytes.Array);
            }
        }

        private void SendEncodedMessage(ArraySegment<byte> encodedBytes, string correlationId, string applicationMessageType, string senderId, string topicSuffix)
        {
            ThrowIfDisposedOrNotOpen();

            try
            {
                endpoint.Send(encodedBytes, correlationId, applicationMessageType, senderId, topicSuffix);
            }
            finally
            {
                if (encodedBytes.Array != null)
                    bufferManager.ReturnBuffer(encodedBytes.Array);
            }
        }

        private void SendEncodedReplyMessage(IDestination destination, string correlationId, string applicationMessageType, ArraySegment<byte> encodedBytes, TimeSpan timeout)
        {
            ThrowIfDisposedOrNotOpen();

            try
            {
                endpoint.SendReply(destination, correlationId, applicationMessageType, encodedBytes);
            }
            finally
            {
                if (encodedBytes.Array != null)
                    bufferManager.ReturnBuffer(encodedBytes.Array);
            }
        }

        public void SendMessage(Message message, TimeSpan timeout)
        {
            string correlationId = message.Properties.TryGetValue(SolaceConstants.CorrelationIdKey) as string;
            string applicationMessageType = (string)message.Properties[SolaceConstants.ApplicationMessageTypeKey];
            var senderId = (string)message.Properties.TryGetValue(SolaceConstants.SenderIdKey);
            var topicSuffix = (string)message.Properties.TryGetValue(SolaceConstants.TopicSuffixKey);

            SendEncodedMessage(EncodeMessage(message), correlationId, applicationMessageType, senderId, topicSuffix);
        }

        public Message SendRequestMessage(Message message, TimeSpan timeout)
        {
            var applicationMessageType = (string)message.Properties[SolaceConstants.ApplicationMessageTypeKey];
            var topicSuffix = (string)message.Properties.TryGetValue(SolaceConstants.TopicSuffixKey);
            var senderId = (string)message.Properties.TryGetValue(SolaceConstants.SenderIdKey);

            return SendEncodedRequestMessage(EncodeMessage(message), applicationMessageType, senderId, topicSuffix, timeout);
        }

        internal void SendReply(IDestination destination, string correlationId, Message reply, TimeSpan timeout)
        {
            SendEncodedReplyMessage(destination, correlationId, (string)reply.Properties[SolaceConstants.ApplicationMessageTypeKey], EncodeMessage(reply), timeout);
        }

        public IAsyncResult BeginSendMessage(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            ThrowIfDisposedOrNotOpen();

            var encodedMessage = this.EncodeMessage(message);
            var applicationMessageType = (string)message.Properties[SolaceConstants.ApplicationMessageTypeKey];
            var topicSuffix = (string)message.Properties.TryGetValue(SolaceConstants.TopicSuffixKey);
            var senderId = (string)message.Properties.TryGetValue(SolaceConstants.SenderIdKey);

            return TaskHelper.CreateTask(() => SendEncodedRequestMessage(encodedMessage, applicationMessageType, senderId, topicSuffix, timeout), callback, state);
        }

        public IAsyncResult BeginSendMessage(Message message, AsyncCallback callback, object state) =>
            BeginSendMessage(message, this.DefaultSendTimeout, callback, state);

        public void EndSendMessage(IAsyncResult result) =>
            ((Task)result).Wait();

        private ArraySegment<byte> EncodeMessage(Message message)
        {
            try
            {
                return message.IsFault
                    ? EncodeFaultMessage(message)
                    : encoder.WriteMessage(message, maxBufferSize, bufferManager);
            }
            finally
            {
                // we've consumed the message by serializing it, so clean up
                message.Close();
            }
        }

        private ArraySegment<byte> EncodeFaultMessage(Message message)
        {
            using (var reader = message.GetReaderAtBodyContents())
            {
                string error;

                reader.ReadToDescendant("Code");
                reader.ReadToDescendant("Value");
                error = reader.ReadElementContentAsString();
                reader.ReadStartElement();
                error += ": " + reader.ReadElementContentAsString().Substring(2);

                string json = new JObject
                {
                    { "error", new JObject
                        {
                            { "type", typeof(ArgumentException).FullName },
                            { "message", error },
                        } }
                }.ToString();
                int length = Encoding.UTF8.GetByteCount(json);
                var buffer = bufferManager.TakeBuffer(length);

                Encoding.UTF8.GetBytes(json, 0, json.Length, buffer, 0);

                return new ArraySegment<byte>(buffer, 0, length);
            }
        }

        public Message ReceiveMessage(TimeSpan timeout)
        {
            ThrowIfDisposedOrNotOpen();

            var request = endpoint.Receive(timeout);
            var message = DecodeMessage(request);

            message.Properties[SolaceConstants.CorrelationIdKey] = request.CorrelationId;
            message.Properties[SolaceConstants.ReplyToKey] = request.ReplyTo;

            if (converters != null)
                foreach (var converter in converters)
                    converter.Convert(request, message);

            return message;
        }

        protected IAsyncResult BeginTryReceiveMessage(TimeSpan timeout, AsyncCallback callback, object state)
        {
            try
            {
                ThrowIfDisposedOrNotOpen();
            }
            catch (Exception)
            {
                return Task.FromResult<Message>(null);
            }

            return TaskHelper.CreateTask(() =>
            {
                try
                {
                    return ReceiveMessage(timeout);
                }
                catch (ObjectDisposedException)
                {
                    return null;
                }
            },
            callback, state);
        }

        public IAsyncResult BeginReceiveMessage(TimeSpan timeout, AsyncCallback callback, object state)
        {
            try
            {
                ThrowIfDisposedOrNotOpen();
            }
            catch (Exception ex)
            {
                return Task.FromException<Message>(ex);
            }

            return TaskHelper.CreateTask(() => ReceiveMessage(timeout), callback, state);
        }

        public Message EndReceiveMessage(IAsyncResult result) =>
            ((Task<Message>)result).Result;

        protected virtual Message DecodeMessage(IMessage data)
        {
            if (data == null)
                return null;
            else
            {
                var message = this.encoder.ReadMessage(data.ToBuffer(bufferManager), bufferManager);

                message.Properties[SolaceConstants.ApplicationMessageTypeKey] = data.ApplicationMessageType;
                message.Properties[SolaceConstants.CorrelationIdKey] = data.CorrelationId;
                message.Properties[SolaceConstants.SenderIdKey] = data.SenderId;

                return message;
            }
        }

        protected override void OnAbort() =>
            endpoint?.Close(0);

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            OnClose(timeout);

            return new CompletedAsyncResult(callback, state);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            OnOpen(timeout);

            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnClose(TimeSpan timeout) =>
            endpoint.Close((int)timeout.TotalMilliseconds);

        protected override void OnEndClose(IAsyncResult result) =>
            CompletedAsyncResult.End(result);

        protected override void OnEndOpen(IAsyncResult result) =>
            CompletedAsyncResult.End(result);

        protected override void OnOpen(TimeSpan timeout)
        {
        }
    }
}
