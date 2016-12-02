﻿using System;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Newtonsoft.Json;

namespace Solace.Channels
{
    public class SolaceEndpointBehavior : IEndpointBehavior
    {
        readonly Func<JsonSerializerSettings> settingsProvider;

        public SolaceEndpointBehavior()
        {
            settingsProvider = JsonConvert.DefaultSettings;
        }

        public SolaceEndpointBehavior(Func<JsonSerializerSettings> settingsProvider)
        {
            this.settingsProvider = settingsProvider;
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
                    clientOperation.Formatter = new SolaceMessageFormatter(operation, settingsProvider);
                }
            }
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new SolaceMessageInspector());
            endpointDispatcher.DispatchRuntime.OperationSelector = new SolaceOperationSelector();
            endpointDispatcher.ChannelDispatcher.ErrorHandlers.Add(new SolaceErrorHandler());
            endpointDispatcher.ContractFilter = new MatchAllMessageFilter();
            foreach (OperationDescription operation in endpoint.Contract.Operations)
            {
                if (!SolaceHelpers.IsUntypedMessage(operation))
                {
                    DispatchOperation dispatchOperation = endpointDispatcher.DispatchRuntime.Operations[operation.Name];
                    dispatchOperation.DeserializeRequest = true;
                    dispatchOperation.SerializeReply = true;
                    dispatchOperation.Formatter = new SolaceMessageFormatter(operation, settingsProvider);
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
