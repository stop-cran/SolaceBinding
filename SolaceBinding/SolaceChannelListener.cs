using Solace.Channels.MessageConverters;
using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

namespace Solace.Channels
{
    internal class SolaceChannelListener : ChannelListenerBase<IReplyChannel>
    {
        private readonly BufferManager bufferManager;
        private readonly MessageEncoderFactory encoderFactory;
        private SolaceEndpoint endpoint;
        private readonly Uri uri;
        private readonly SolaceEndpointCache enpointCache;
        private readonly IEnumerable<IMessageConverter> converters;

        public SolaceChannelListener(SolaceTransportBindingElement bindingElement, BindingContext context)
            : base(context.Binding)
        {
            // populate members from binding element
            int maxBufferSize = (int)bindingElement.MaxReceivedMessageSize;
            bufferManager = BufferManager.CreateBufferManager(bindingElement.MaxBufferPoolSize, maxBufferSize);
            enpointCache = bindingElement.EndpointCache;

            converters = context.BindingParameters.FindAll<IMessageConverter>();

            var messageEncoderBindingElements
                = context.BindingParameters.FindAll<MessageEncodingBindingElement>();

            if (messageEncoderBindingElements.Count > 1)
                throw new InvalidOperationException("More than one MessageEncodingBindingElement was found in the BindingParameters of the BindingContext");
            else if (messageEncoderBindingElements.Count == 1)
            {
                if (!(messageEncoderBindingElements[0] is ByteStreamMessageEncodingBindingElement))
                    throw new InvalidOperationException("This transport must be used with the ByteStreamMessageEncodingBindingElement.");

                encoderFactory = messageEncoderBindingElements[0].CreateMessageEncoderFactory();
            }
            else
                encoderFactory = new ByteStreamMessageEncodingBindingElement().CreateMessageEncoderFactory();

            uri = new Uri(context.ListenUriBaseAddress, context.ListenUriRelativeAddress);
        }

        protected override IReplyChannel OnAcceptChannel(TimeSpan timeout)
        {
            try
            {
                return new SolaceReplyChannel(encoderFactory.Encoder, bufferManager, uri, endpoint.Accept(), this, converters);
            }
            catch (ObjectDisposedException)
            {
                // endpoint closed
                return null;
            }
        }

        protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return TaskHelper.CreateTask(() => OnAcceptChannel(timeout), callback, state);
        }

        protected override IReplyChannel OnEndAcceptChannel(IAsyncResult result)
        {
            return ((Task<IReplyChannel>)result).Result;
        }

        protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            throw new NotSupportedException("No peeking support");
        }

        protected override bool OnEndWaitForChannel(IAsyncResult result)
        {
            throw new NotSupportedException("No peeking support");
        }

        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            throw new NotSupportedException("No peeking support");
        }

        public override Uri Uri => uri;

        protected override void OnAbort()
        {
            CloseEndpoint(TimeSpan.Zero);
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            CloseEndpoint(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            OpenEndpoint();
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnClose(TimeSpan timeout) =>
            CloseEndpoint(timeout);

        protected override void OnClosed()
        {
            base.OnClosed();
            if (State != System.ServiceModel.CommunicationState.Closed)
                1.ToString();
        }

        protected override void OnEndClose(IAsyncResult result) =>
            CompletedAsyncResult.End(result);

        protected override void OnEndOpen(IAsyncResult result) =>
            CompletedAsyncResult.End(result);

        protected override void OnOpen(TimeSpan timeout) =>
            OpenEndpoint();

        private void OpenEndpoint()
        {
            endpoint = enpointCache.Create(uri);
            endpoint.Connect();
            endpoint.Listen();
        }

        private void CloseEndpoint(TimeSpan timeout) =>
            endpoint.Close((int)timeout.TotalMilliseconds);
    }
}
