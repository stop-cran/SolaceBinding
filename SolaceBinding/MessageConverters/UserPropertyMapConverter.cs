using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.SDT;
using System;
using System.ServiceModel.Channels;

namespace Solace.Channels.MessageConverters
{
    internal sealed class UserPropertyMapConverter : IMessageConverter
    {
        public void Convert(IMessage from, Message to)
        {
            var userPropertyMap = from?.UserPropertyMap;

            if (userPropertyMap != null)
                while (userPropertyMap.HasNext())
                {
                    var next = userPropertyMap.GetNext();
                    SDTFieldType type = next.Value.Type;

                    switch (type)
                    {
                        case SDTFieldType.UNKNOWN:
                            throw new NotSupportedException();
                        case SDTFieldType.NULL:
                            continue;
                        case SDTFieldType.DESTINATION:
                        case SDTFieldType.SMF_MESSAGE:
                            break;

                        default:
                            to.Properties[next.Key] = next.Value.Value;
                            continue;
                    }
                }
        }
    }
}
