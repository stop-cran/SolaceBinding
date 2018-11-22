using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Solace.Channels
{
    internal class SolaceChannelFactory : ChannelFactoryBase<IRequestChannel>
    {
        private readonly BufferManager bufferManager;
        private readonly MessageEncoderFactory encoderFactory;
        private readonly SolaceEndpointCache enpointCache;

        public SolaceChannelFactory(SolaceTransportBindingElement bindingElement, BindingContext context)
            : base(context.Binding)
        {
            enpointCache = bindingElement.EndpointCache;

            // populate members from binding element
            int maxBufferSize = (int)bindingElement.MaxReceivedMessageSize;
            bufferManager = BufferManager.CreateBufferManager(bindingElement.MaxBufferPoolSize, maxBufferSize);

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
        }

        protected override IRequestChannel OnCreateChannel(EndpointAddress address, Uri via) =>
            new SolaceRequestChannel(
                encoderFactory.Encoder,
                bufferManager,
                this,
                address,
                via,
                enpointCache);

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            OnOpen(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnEndOpen(IAsyncResult result) =>
            CompletedAsyncResult.End(result);

        protected override void OnOpen(TimeSpan timeout)
        {
        }
    }
}
