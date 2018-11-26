using Solace.Channels.MessageConverters;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Solace.Channels
{
    public class SolaceUserPropertyMapEndpointBehavior : IEndpointBehavior
    {
        public virtual void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            bindingParameters.Add(new UserPropertyMapConverter());
        }

        public virtual void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public virtual void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public virtual void Validate(ServiceEndpoint endpoint)
        {
        }
    }
}
