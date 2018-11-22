using SolaceSystems.Solclient.Messaging;
using System.ServiceModel.Channels;

namespace Solace.Channels.MessageConverters
{
    public sealed class TopicSuffixMessageConverter : IMessageConverter
    {
        private readonly string rootTopic;

        public TopicSuffixMessageConverter(string rootTopic)
        {
            this.rootTopic = (rootTopic.EndsWith(">") ? rootTopic.Substring(0, rootTopic.Length - 1) : null);
        }

        public void Convert(IMessage from, Message to)
        {
            if (rootTopic != null && from.Destination.Name.StartsWith(rootTopic))
                to.Properties["TopicSuffix"] = from.Destination.Name.Substring(rootTopic.Length);
        }
    }
}
