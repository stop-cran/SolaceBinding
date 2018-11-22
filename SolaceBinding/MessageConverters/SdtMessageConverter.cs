using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.SDT;
using System.Collections.Generic;
using System.ServiceModel.Channels;

namespace Solace.Channels.MessageConverters
{
    internal sealed class SdtMessageConverter : IMessageConverter
    {
        public void Convert(IMessage from, Message to)
        {
            var streamContainer = SDTUtils.GetContainer(from) as IStreamContainer;

            if (streamContainer != null)
                using (streamContainer)
                {
                    var list = new List<object>();

                    while (streamContainer.HasNext())
                        list.Add(streamContainer.GetNext().Value);

                    if (list.Count > 0)
                        to.Properties["SdtStreamContainer"] = list;
                }
        }
    }
}
