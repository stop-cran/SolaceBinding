using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

namespace Solace.Channels
{
    class SolaceRequestChannel : SolaceBaseChannel, IRequestChannel
    {
        Uri via;
        EndpointAddress remoteAddress;
        readonly string vpn, user, password;

        public SolaceRequestChannel(MessageEncoder encoder, BufferManager bufferManager, ChannelManagerBase channelManager, EndpointAddress remoteAddress, Uri via,
            string vpn, string user, string password)
            : base(encoder, bufferManager, channelManager)
        {
            this.via = via;
            this.remoteAddress = remoteAddress;
            this.vpn = vpn;
            this.user = user;
            this.password = password;
        }

        public IAsyncResult BeginRequest(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return TaskHelper.CreateTask(() => Request(message, timeout), callback, state);
        }

        public IAsyncResult BeginRequest(Message message, AsyncCallback callback, object state)
        {
            return BeginRequest(message, DefaultSendTimeout, callback, state);
        }

        public Message EndRequest(IAsyncResult result)
        {
            return (result as Task<Message>)?.Result;
        }

        public EndpointAddress RemoteAddress
        {
            get { return remoteAddress; }
        }

        public Message Request(Message message, TimeSpan timeout)
        {
            base.SendMessage(message, timeout);
            return base.ReceiveMessage(timeout);
        }

        public Message Request(Message message)
        {
            return Request(message, DefaultSendTimeout);
        }

        public Uri Via
        {
            get { return via; }
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            Connect();
            base.OnOpen(timeout);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return TaskHelper.CreateTask(Connect, callback, state);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            ((Task)result).Wait();
        }

        void Connect()
        {
            var endpoint = new SolaceEndpoint(Via, vpn, user, password);
            endpoint.Connect();

            base.InitializeSolaceEndpoint(endpoint);
        }
    }
}
