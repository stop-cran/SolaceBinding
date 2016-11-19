using System.ServiceModel.Channels;

namespace Solace.Channels
{
    public class SolaceTransportBindingElement : TransportBindingElement
    {
        public SolaceTransportBindingElement() : base() { }

        public SolaceTransportBindingElement(SolaceTransportBindingElement other)
            : base(other)
        {
            VPN = other.VPN;
            UserName = other.UserName;
            Password = other.Password;
        }

        public override string Scheme
        {
            get { return "solace.net"; }
        }

        public string VPN { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public override BindingElement Clone()
        {
            return new SolaceTransportBindingElement(this);
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            return typeof(TChannel) == typeof(IRequestChannel);
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            return (IChannelFactory<TChannel>)(object)new SolaceChannelFactory(this, context);
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            return typeof(TChannel) == typeof(IReplyChannel);
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            return (IChannelListener<TChannel>)(object)new SolaceChannelListener(this, context, VPN, UserName, Password);
        }

        public override T GetProperty<T>(BindingContext context)
        {
            if (typeof(T) == typeof(MessageVersion))
            {
                return (T)(object)MessageVersion.None;
            }

            return base.GetProperty<T>(context);
        }
    }
}
