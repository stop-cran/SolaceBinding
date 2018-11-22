using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Solace.Channels
{
    public class SolaceProtobufEndpointBehavior : IEndpointBehavior
    {
        private readonly IProtobufConverterFactory converterFactory;

        public SolaceProtobufEndpointBehavior()
        {
            converterFactory = new ProtobufConverterFactory(new List<IValueConverter>().AsReadOnly());
        }

        public SolaceProtobufEndpointBehavior(IProtobufConverterFactory converterFactory)
        {
            this.converterFactory = converterFactory;
        }

        public virtual void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

        public virtual void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(new SolaceMessageInspector());

            foreach (var operation in endpoint.Contract.Operations)
                if (!MessageBinaryHelper.IsUntypedMessage(operation))
                {
                    var clientOperation = clientRuntime.Operations[operation.Name];

                    clientOperation.SerializeRequest = true;
                    clientOperation.DeserializeReply = true;
                    clientOperation.Formatter = new SolaceProtobufMessageFormatter(operation, converterFactory);
                }
        }

        public virtual void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.ContractFilter = new MatchAllMessageFilter();
            endpointDispatcher.DispatchRuntime.OperationSelector = new SolaceOperationSelector();
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new SolaceMessageInspector());
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(new SolaceProtobufErrorHandler());

            foreach (var operation in endpoint.Contract.Operations)
                if (!MessageBinaryHelper.IsUntypedMessage(operation))
                {
                    var dispatchOperation = endpointDispatcher.DispatchRuntime.Operations[operation.Name];

                    dispatchOperation.DeserializeRequest = true;
                    dispatchOperation.SerializeReply = true;
                    dispatchOperation.Formatter = new SolaceProtobufMessageFormatter(operation, converterFactory);
                }
        }

        public virtual void Validate(ServiceEndpoint endpoint) { }
    }
}
