using System;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Collections.Generic;

namespace Solace.Channels
{
    public class SolaceProtobufEndpointBehavior : IEndpointBehavior
    {
        readonly IProtobufConverterFactory converterFactory;
        readonly Func<IErrorHandler> errorHandlerFactory;

        public SolaceProtobufEndpointBehavior()
        {
            converterFactory = new ProtobufConverterFactory(new List<IValueConverter>().AsReadOnly());
            errorHandlerFactory = () => new SolaceProtobufErrorHandler();
        }

        public SolaceProtobufEndpointBehavior(IProtobufConverterFactory converterFactory, Func<IErrorHandler> errorHandlerFactory)
        {
            this.converterFactory = converterFactory;
            this.errorHandlerFactory = errorHandlerFactory;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(new SolaceMessageInspector());
            foreach (OperationDescription operation in endpoint.Contract.Operations)
            {
                if (!SolaceHelpers.IsUntypedMessage(operation))
                {
                    ClientOperation clientOperation = clientRuntime.Operations[operation.Name];
                    clientOperation.SerializeRequest = true;
                    clientOperation.DeserializeReply = true;
                    clientOperation.Formatter = new SolaceProtobufMessageFormatter(operation, converterFactory);
                }
            }
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new SolaceMessageInspector());
            endpointDispatcher.DispatchRuntime.OperationSelector = new SolaceOperationSelector();
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(errorHandlerFactory());
            endpointDispatcher.ContractFilter = new MatchAllMessageFilter();
            foreach (OperationDescription operation in endpoint.Contract.Operations)
            {
                if (!SolaceHelpers.IsUntypedMessage(operation))
                {
                    DispatchOperation dispatchOperation = endpointDispatcher.DispatchRuntime.Operations[operation.Name];
                    dispatchOperation.DeserializeRequest = true;
                    dispatchOperation.SerializeReply = true;
                    dispatchOperation.Formatter = new SolaceProtobufMessageFormatter(operation, converterFactory);
                }
            }
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            foreach (OperationDescription operation in endpoint.Contract.Operations)
            {
                if (operation.IsOneWay)
                {
                    throw new InvalidOperationException("One-way operations not supported in this implementation");
                }
            }
        }
    }
}
