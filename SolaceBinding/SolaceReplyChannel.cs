using System;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.Threading.Tasks;
using SolaceSystems.Solclient.Messaging;
using System.Collections.Generic;
using Solace.Channels.MessageConverters;

namespace Solace.Channels
{
    class SolaceReplyChannel : SolaceBaseChannel, IReplyChannel
    {
        Uri localAddress;
        ISolaceEndpoint endpoint;
        bool began;

        public SolaceReplyChannel(MessageEncoder encoder, BufferManager bufferManager, Uri localAddress, ISolaceEndpoint endpoint, ChannelManagerBase channelManager, IEnumerable<IMessageConverter> converters)
            : base(encoder, bufferManager, channelManager, converters)
        {
            this.localAddress = localAddress;
            this.endpoint = endpoint;
            this.InitializeSolaceEndpoint(endpoint);
        }

        public IAsyncResult BeginReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return BeginReceiveMessage(timeout, callback, state);
        }

        public IAsyncResult BeginReceiveRequest(AsyncCallback callback, object state)
        {
            return this.BeginReceiveRequest(this.DefaultReceiveTimeout, callback, state);
        }

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

        public RequestContext EndReceiveRequest(IAsyncResult result)
        {
            var res = new SolaceRequestContext(this, ((Task<Message>)result).Result, TimeSpan.MaxValue);

            return res;
        }

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

        public EndpointAddress LocalAddress
        {
            get { return new EndpointAddress(this.localAddress); }
        }

        public RequestContext ReceiveRequest(TimeSpan timeout)
        {
            var request = this.ReceiveMessage(timeout);

            return new SolaceRequestContext(this, request, timeout);
        }

        public RequestContext ReceiveRequest()
        {
            return this.ReceiveRequest(this.DefaultReceiveTimeout);
        }

        public bool TryReceiveRequest(TimeSpan timeout, out RequestContext context)
        {
            try
            {
                context = this.ReceiveRequest(timeout);
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
                result.Headers.To = this.localAddress;
                Uri remoteEndpoint = endpoint.RemoteEndPoint;
                RemoteEndpointMessageProperty property = new RemoteEndpointMessageProperty(remoteEndpoint.ToString(), remoteEndpoint.Port == -1 ? 55555 : remoteEndpoint.Port);
                result.Properties.Add(RemoteEndpointMessageProperty.Name, property);
            }

            return result;
        }
    }
}
