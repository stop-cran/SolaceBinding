using Solace.Channels.MessageConverters;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Solace.Channels
{
    public class SolaceSdtContainerEndpointBehavior : IEndpointBehavior
    {
        public virtual void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            bindingParameters.Add(new SdtMessageConverter());
        }

        public virtual void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(new SolaceMessageInspector());

            foreach (var operationDescription in endpoint.Contract.Operations)
                if (!MessageBinaryHelper.IsUntypedMessage(operationDescription))
                {
                    var clientOperation = clientRuntime.Operations[operationDescription.Name];

                    clientOperation.SerializeRequest = true;
                    clientOperation.DeserializeReply = true;
                    clientOperation.Formatter = new SolaceSdtStreamContainerMessageFormatter(operationDescription);
                }
        }

        public virtual void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.ContractFilter = new MatchAllMessageFilter();
            endpointDispatcher.DispatchRuntime.OperationSelector = new SolaceOperationSelector();
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new SolaceMessageInspector());

            foreach (var operationDescription in endpoint.Contract.Operations)
            {
                if (!MessageBinaryHelper.IsUntypedMessage(operationDescription))
                {
                    var dispatchOperation = endpointDispatcher.DispatchRuntime.Operations[operationDescription.Name];

                    dispatchOperation.DeserializeRequest = true;
                    dispatchOperation.SerializeReply = true;
                    dispatchOperation.Formatter = new SolaceSdtStreamContainerMessageFormatter(operationDescription);
                }
            }
        }

        public virtual void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}
