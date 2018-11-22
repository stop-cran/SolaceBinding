using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

namespace Solace.Channels
{
    internal class SolaceRequestChannel : SolaceBaseChannel, IRequestChannel
    {
        private readonly SolaceEndpointCache enpointCache;

        public SolaceRequestChannel(MessageEncoder encoder, BufferManager bufferManager, ChannelManagerBase channelManager, EndpointAddress remoteAddress, Uri via,
            SolaceEndpointCache enpointCache)
            : base(encoder, bufferManager, channelManager, null)
        {
            Via = via;
            RemoteAddress = remoteAddress;
            this.enpointCache = enpointCache;
        }

        public IAsyncResult BeginRequest(Message message, TimeSpan timeout, AsyncCallback callback, object state) =>
            TaskHelper.CreateTask(() => Request(message, timeout), callback, state);

        public IAsyncResult BeginRequest(Message message, AsyncCallback callback, object state) =>
            BeginRequest(message, DefaultSendTimeout, callback, state);

        public Message EndRequest(IAsyncResult result) =>
            (result as Task<Message>)?.Result;

        public EndpointAddress RemoteAddress { get; }

        public Message Request(Message message, TimeSpan timeout)
        {
            if (message.Properties.TryGetValue(SolaceConstants.IsOneWayKey) as bool? ?? false)
            {
                SendMessage(message, timeout);
                return null;
            }

            return SendRequestMessage(message, timeout);
        }

        public Message Request(Message message) =>
            Request(message, DefaultSendTimeout);

        public Uri Via { get; }

        protected override void OnOpen(TimeSpan timeout)
        {
            Connect();
            base.OnOpen(timeout);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state) =>
            TaskHelper.CreateTask(Connect, callback, state);

        protected override void OnEndOpen(IAsyncResult result) =>
            ((Task)result).Wait();

        private void Connect()
        {
            var endpoint = enpointCache.Create(Via);
            endpoint.Connect();

            base.InitializeSolaceEndpoint(endpoint);
        }
    }
}
