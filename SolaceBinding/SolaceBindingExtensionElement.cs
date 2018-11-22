using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Solace.Channels
{
    public class SolaceBindingExtensionElement : BindingElementExtensionElement
    {
        public override Type BindingElementType
        {
            get
            {
                return typeof(SolaceTransportBindingElement);
            }
        }

        protected override BindingElement CreateBindingElement()
        {
            return new SolaceTransportBindingElement
            {
                VPN = VPN,
                UserName = UserName,
                Password = Password
            };
        }

        [ConfigurationProperty(nameof(VPN), IsRequired = true)]
        public string VPN
        {
            get { return (string)base[nameof(VPN)]; }
            set { base[nameof(VPN)] = value; }
        }

        [ConfigurationProperty(nameof(UserName), IsRequired = true)]
        public string UserName
        {
            get { return (string)base[nameof(UserName)]; }
            set { base[nameof(UserName)] = value; }
        }

        [ConfigurationProperty(nameof(Password), IsRequired = true)]
        public string Password
        {
            get { return (string)base[nameof(Password)]; }
            set { base[nameof(Password)] = value; }
        }
    }
}
