using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.ServiceModel;
using Newtonsoft.Json.Linq;

namespace Solace.Channels
{
    class SolaceMessageInspector : IClientMessageInspector, IDispatchMessageInspector
    {
        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            var json = SolaceHelpers.GetJObjectPreservingMessage(ref reply) as JObject;
            string replyId = (string)reply.Properties[SolaceConstants.CorrelationIdKey];
            if (!(correlationState is RequestCorrelationState) && replyId != (string)correlationState)
            {
                throw new SolaceException("id mismatch", "Reply does not correspond to the request!");
            }

            
            var error = json?[SolaceConstants.ErrorKey];

            if (error != null && error.Type != JTokenType.Null)
                throw new SolaceException(error);
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            return request.Properties[SolaceConstants.CorrelationIdKey];
        }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            return request.Properties[SolaceConstants.CorrelationIdKey];
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            if (!reply.Properties.ContainsKey(SolaceConstants.ApplicationMessageTypeKey))
                reply.Properties[SolaceConstants.ApplicationMessageTypeKey] = "Fault";
            reply.Properties[SolaceConstants.CorrelationIdKey] = correlationState;
        }
    }
}
