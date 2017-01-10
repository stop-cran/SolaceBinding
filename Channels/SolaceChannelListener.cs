using SolaceSystems.Solclient.Messaging;
using System;
using System.Collections.ObjectModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

namespace Solace.Channels
{
    class SolaceChannelListener : ChannelListenerBase<IReplyChannel>
    {
        BufferManager bufferManager;
        MessageEncoderFactory encoderFactory;
        SolaceEndpoint endpoint;
        Uri uri;
        readonly string vpn, user, password;
        readonly Action<SessionEventArgs> raiseSessionEvent;

        public SolaceChannelListener(SolaceTransportBindingElement bindingElement, BindingContext context)
            : base(context.Binding)
        {
            // populate members from binding element
            int maxBufferSize = (int)bindingElement.MaxReceivedMessageSize;
            this.bufferManager = BufferManager.CreateBufferManager(bindingElement.MaxBufferPoolSize, maxBufferSize);
            this.vpn = bindingElement.VPN;
            this.user = bindingElement.UserName;
            this.password = bindingElement.Password;
            this.raiseSessionEvent = bindingElement.RaiseSessionEvent;

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

            this.uri = new Uri(context.ListenUriBaseAddress, context.ListenUriRelativeAddress);
        }

        protected override IReplyChannel OnAcceptChannel(TimeSpan timeout)
        {
            try
            {
                return new SolaceReplyChannel(this.encoderFactory.Encoder, this.bufferManager, this.uri, endpoint.Accept(), this);
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

        public override Uri Uri
        {
            get { return this.uri; }
        }

        protected override void OnAbort()
        {
            this.CloseEndpoint(TimeSpan.Zero);
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.CloseEndpoint(timeout);
            return new CompletedAsyncResult(callback, state);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OpenEndpoint();
            return new CompletedAsyncResult(callback, state);
        }

        protected override void OnClose(TimeSpan timeout)
        {
            this.CloseEndpoint(timeout);
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
            this.OpenEndpoint();
        }

        void OpenEndpoint()
        {
            this.endpoint = new SolaceEndpoint(uri, vpn, user, password, (sender, args) => raiseSessionEvent(args));
            this.endpoint.Connect();
            this.endpoint.Listen();
        }

        void CloseEndpoint(TimeSpan timeout)
        {
            this.endpoint.Close((int)timeout.TotalMilliseconds);
        }
    }
}
