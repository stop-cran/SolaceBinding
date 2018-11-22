using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.ServiceModel;

namespace Solace.Channels
{
    class SolaceMessageInspector : IClientMessageInspector, IDispatchMessageInspector
    {
        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            if (!(correlationState is RequestCorrelationState) &&
                (string)reply.Properties[SolaceConstants.CorrelationIdKey] != (string)correlationState)
                throw new SolaceException("Reply does not correspond to the request - correlation id mismatch!");
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            return request.Properties[SolaceConstants.CorrelationIdKey];
        }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            return request.Properties.TryGetValue(SolaceConstants.CorrelationIdKey);
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            if (reply != null)
            {
                if (!reply.Properties.ContainsKey(SolaceConstants.ApplicationMessageTypeKey))
                    reply.Properties[SolaceConstants.ApplicationMessageTypeKey] = "Fault";
                reply.Properties[SolaceConstants.CorrelationIdKey] = correlationState;
            }
        }
    }
}
