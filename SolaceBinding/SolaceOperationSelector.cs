using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace Solace.Channels
{
    internal class SolaceOperationSelector : IDispatchOperationSelector
    {
        public string SelectOperation(ref Message message) =>
            (string)message.Properties[SolaceConstants.ApplicationMessageTypeKey];
    }
}
