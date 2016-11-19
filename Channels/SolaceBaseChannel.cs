using System;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using SolaceSystems.Solclient.Messaging;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Solace.Channels
{
    class SolaceBaseChannel : ChannelBase
    {
        const int maxBufferSize = 1024 * 1024;

        SolaceEndpoint endpoint;
        MessageEncoder encoder;
        BufferManager bufferManager;

        public SolaceBaseChannel(MessageEncoder encoder, BufferManager bufferManager, ChannelManagerBase channelManager)
            : base(channelManager)
        {
            this.encoder = encoder;
            this.bufferManager = bufferManager;
        }

        protected virtual void InitializeSolaceEndpoint(SolaceEndpoint endpoint)
        {
            if (this.endpoint != null)
            {
                throw new InvalidOperationException("SolaceEndpoint is already set");
            }

            this.endpoint = endpoint;
        }

        byte[] SendEncodedMessage(ArraySegment<byte> encodedBytes, string applicationMessageType, TimeSpan timeout)
        {
            base.ThrowIfDisposedOrNotOpen();
            try
            {
                return endpoint.SendRequest(encodedBytes, applicationMessageType, timeout);
            }
            finally
            {
                if (encodedBytes.Array != null)
                    this.bufferManager.ReturnBuffer(encodedBytes.Array);
            }
        }

        void SendEncodedReplyMessage(IDestination destination, string correlationId, ArraySegment<byte> encodedBytes, TimeSpan timeout)
        {
            base.ThrowIfDisposedOrNotOpen();
            try
            {
                endpoint.SendReply(destination, correlationId, encodedBytes);
            }
            finally
            {
                if (encodedBytes.Array != null)
                    this.bufferManager.ReturnBuffer(encodedBytes.Array);
            }
        }

        public void SendMessage(Message message, TimeSpan timeout)
        {
            SendEncodedMessage(this.EncodeMessage(message), (string)message.Properties["ApplicationMessageType"], timeout);
        }

        internal void Send(IDestination destination, string correlationId, Message reply, TimeSpan timeout)
        {
            SendEncodedReplyMessage(destination, correlationId, this.EncodeMessage(reply), timeout);
        }

        public IAsyncResult BeginSendMessage(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            base.ThrowIfDisposedOrNotOpen();
            var encodedMessage = this.EncodeMessage(message);
            var applicationMessageType = (string)message.Properties["ApplicationMessageType"];

            return TaskHelper.CreateTask(() => SendEncodedMessage(encodedMessage, applicationMessageType, timeout), callback, state);
        }

        public IAsyncResult BeginSendMessage(Message message, AsyncCallback callback, object state)
        {
            return this.BeginSendMessage(message, this.DefaultSendTimeout, callback, state);
        }

        public void EndSendMessage(IAsyncResult result)
        {
            ((Task)result).Wait();
        }

        ArraySegment<byte> EncodeMessage(Message message)
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
                    {"error", new JObject
                        {
                            { "type", typeof(ArgumentException).FullName },
                            { "message", "error" },
                        }
                    }
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
            var res = DecodeMessage(request);

            res.Properties["SolaceRequest"] = request;

            return res;
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
            }, callback, state);
        }

        public Message EndReceiveMessage(IAsyncResult result)
        {
            return ((Task<Message>)result).Result;
        }

        protected virtual Message DecodeMessage(IMessage data)
        {
            if (data == null)
                return null;
            else
            {
                var message = this.encoder.ReadMessage(data.ToBuffer(bufferManager), bufferManager);

                message.Properties["ApplicationMessageType"] = data.ApplicationMessageType;
                message.Properties["CorrelationId"] = data.CorrelationId;
                message.Properties["SenderId"] = data.SenderId;

                return message;
            }
        }

        protected override void OnAbort()
        {
            if (this.endpoint != null)
            {
                endpoint.Close(0);
            }
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnClose(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnOpen(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnClose(TimeSpan timeout)
        {
            this.endpoint.Close((int)timeout.TotalMilliseconds);
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            CompletedAsyncResult.End(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
        }
    }
}
