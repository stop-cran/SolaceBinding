using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Solace.Channels
{
    class SolaceChannelFactory : ChannelFactoryBase<IRequestChannel>
    {
        BufferManager bufferManager;
        MessageEncoderFactory encoderFactory;
        readonly SolaceEndpointCache enpointCache;

        public SolaceChannelFactory(SolaceTransportBindingElement bindingElement, BindingContext context)
            : base(context.Binding)
        {
            enpointCache = bindingElement.EndpointCache;

            // populate members from binding element
            int maxBufferSize = (int)bindingElement.MaxReceivedMessageSize;
            this.bufferManager = BufferManager.CreateBufferManager(bindingElement.MaxBufferPoolSize, maxBufferSize);

            Collection<MessageEncodingBindingElement> messageEncoderBindingElements
                = context.BindingParameters.FindAll<MessageEncodingBindingElement>();

            if (messageEncoderBindingElements.Count > 1)
            {
                throw new InvalidOperationException("More than one MessageEncodingBindingElement was found in the BindingParameters of the BindingContext");
            }
            else if (messageEncoderBindingElements.Count == 1)
            {
                if (!(messageEncoderBindingElements[0] is ByteStreamMessageEncodingBindingElement))
                {
                    throw new InvalidOperationException("This transport must be used with the ByteStreamMessageEncodingBindingElement.");
                }

                this.encoderFactory = messageEncoderBindingElements[0].CreateMessageEncoderFactory();
            }
            else
            {
                this.encoderFactory = new ByteStreamMessageEncodingBindingElement().CreateMessageEncoderFactory();
            }
        }

        protected override IRequestChannel OnCreateChannel(EndpointAddress address, Uri via)
        {
            return new SolaceRequestChannel(
                this.encoderFactory.Encoder, 
                this.bufferManager, 
                this, 
                address, 
                via,
                enpointCache);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnOpen(timeout);
            return new CompletedAsyncResult(callback, state);
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
