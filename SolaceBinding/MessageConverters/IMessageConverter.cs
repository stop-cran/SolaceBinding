using SolaceSystems.Solclient.Messaging;
using System.ServiceModel.Channels;

namespace Solace.Channels.MessageConverters
{
    public interface IMessageConverter
    {
        void Convert(IMessage from, Message to);
    }
}
