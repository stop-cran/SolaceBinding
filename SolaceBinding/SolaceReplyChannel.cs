using Solace.Channels.MessageConverters;
using SolaceSystems.Solclient.Messaging;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

namespace Solace.Channels
{
    internal class SolaceReplyChannel : SolaceBaseChannel, IReplyChannel
    {
        private Uri localAddress;
        private ISolaceEndpoint endpoint;
        private bool began;

        public SolaceReplyChannel(MessageEncoder encoder, BufferManager bufferManager, Uri localAddress, ISolaceEndpoint endpoint, ChannelManagerBase channelManager, IEnumerable<IMessageConverter> converters)
            : base(encoder, bufferManager, channelManager, converters)
        {
            this.localAddress = localAddress;
            this.endpoint = endpoint;
            InitializeSolaceEndpoint(endpoint);
        }

        public IAsyncResult BeginReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state) =>
            BeginReceiveMessage(timeout, callback, state);

        public IAsyncResult BeginReceiveRequest(AsyncCallback callback, object state) =>
            BeginReceiveRequest(this.DefaultReceiveTimeout, callback, state);

        public IAsyncResult BeginTryReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            if (!began)
                began = true;
            else
                return Task.FromResult<Message>(null);

            return BeginTryReceiveMessage(timeout, callback, state);
        }

        public IAsyncResult BeginWaitForRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotSupportedException("No peeking support");
        }

        public RequestContext EndReceiveRequest(IAsyncResult result) =>
            new SolaceRequestContext(this, ((Task<Message>)result).Result, TimeSpan.MaxValue);

        public bool EndTryReceiveRequest(IAsyncResult result, out RequestContext context)
        {
            var message = (result as Task<Message>)?.Result;

            if (message == null)
            {
                context = null;
                return false;
            }

            context = new SolaceRequestContext(this, message, TimeSpan.MaxValue);

            return true;
        }

        public bool EndWaitForRequest(IAsyncResult result)
        {
            throw new NotSupportedException("No peeking support");
        }

        public EndpointAddress LocalAddress => new EndpointAddress(localAddress);

        public RequestContext ReceiveRequest(TimeSpan timeout) =>
            new SolaceRequestContext(this, ReceiveMessage(timeout), timeout);

        public RequestContext ReceiveRequest() =>
            ReceiveRequest(this.DefaultReceiveTimeout);

        public bool TryReceiveRequest(TimeSpan timeout, out RequestContext context)
        {
            try
            {
                context = ReceiveRequest(timeout);
                return true;
            }
            catch (TimeoutException)
            {
                context = null;
                return false;
            }
        }

        public bool WaitForRequest(TimeSpan timeout)
        {
            throw new NotSupportedException("No peeking support");
        }

        protected override Message DecodeMessage(IMessage message)
        {
            var result = base.DecodeMessage(message);

            if (result != null)
            {
                result.Headers.To = localAddress;
                Uri remoteEndpoint = endpoint.RemoteEndpoint;

                var property = new RemoteEndpointMessageProperty(remoteEndpoint.ToString(), remoteEndpoint.Port == -1 ? 55555 : remoteEndpoint.Port);

                result.Properties.Add(RemoteEndpointMessageProperty.Name, property);
            }

            return result;
        }
    }
}
