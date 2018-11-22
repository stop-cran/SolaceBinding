using SolaceSystems.Solclient.Messaging;
using System;
using System.ServiceModel.Channels;
using System.Threading;

namespace Solace.Channels
{
    public class SolaceTransportBindingElement : TransportBindingElement
    {
        private readonly Lazy<SolaceEndpointCache> cache;

        public SolaceTransportBindingElement() : base()
        {
            cache = new Lazy<SolaceEndpointCache>(() =>
                new SolaceEndpointCache(VPN, UserName, Password,
                    ReplySessionCount,
                    ClientNameSuffix,
                    (sender, e) => RaiseSessionEvent(e)));
        }

        public SolaceTransportBindingElement(SolaceTransportBindingElement other)
            : base(other)
        {
            VPN = other.VPN;
            UserName = other.UserName;
            Password = other.Password;
            SessionEvent += other.SessionEvent;
            cache = other.cache;
        }

        public override string Scheme => "solace.net";

        internal SolaceEndpointCache EndpointCache => cache.Value;

        public string VPN { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public int ReplySessionCount { get; set; }

        public string ClientNameSuffix { get; set; }

        public event EventHandler<SessionEventArgs> SessionEvent;

        public override BindingElement Clone() =>
            new SolaceTransportBindingElement(this);

        internal void RaiseSessionEvent(SessionEventArgs args) =>
            Volatile.Read(ref SessionEvent)?.Invoke(this, args);

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context) =>
            typeof(TChannel) == typeof(IRequestChannel);

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context) =>
            (IChannelFactory<TChannel>)(object)new SolaceChannelFactory(this, context);

        public override bool CanBuildChannelListener<TChannel>(BindingContext context) =>
            typeof(TChannel) == typeof(IReplyChannel);

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context) =>
            (IChannelListener<TChannel>)(object)new SolaceChannelListener(this, context);

        public override T GetProperty<T>(BindingContext context)
        {
            if (typeof(T) == typeof(MessageVersion))
                return (T)(object)MessageVersion.None;

            return base.GetProperty<T>(context);
        }
    }
}
