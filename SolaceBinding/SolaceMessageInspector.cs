using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace Solace.Channels
{
    internal class SolaceMessageInspector : IClientMessageInspector, IDispatchMessageInspector
    {
        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            if (!(correlationState is RequestCorrelationState) &&
                (string)reply.Properties[SolaceConstants.CorrelationIdKey] != (string)correlationState)
                throw new SolaceException("Reply does not correspond to the request - correlation id mismatch!");
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel) =>
            request.Properties[SolaceConstants.CorrelationIdKey];

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext) =>
            request.Properties.TryGetValue(SolaceConstants.CorrelationIdKey);

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
