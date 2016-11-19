using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.ServiceModel;
using Newtonsoft.Json.Linq;

namespace Solace.ServiceModel
{
    class SolaceMessageInspector : IClientMessageInspector, IDispatchMessageInspector
    {
        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            JObject json = SolaceHelpers.GetJObjectPreservingMessage(ref reply);
            string replyId = (string)reply.Properties["CorrelationId"];
            if (replyId != (string)correlationState)
            {
                throw new SolaceException("id mismatch", "Reply does not correspond to the request!");
            }

            var error = json[SolaceConstants.ErrorKey];

            if (error.Type != JTokenType.Null)
                throw new SolaceException(error);
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            return request.Properties["CorrelationId"];
        }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            return request.Properties["CorrelationId"];
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            reply.Properties["CorrelationId"] = correlationState;
        }
    }
}
