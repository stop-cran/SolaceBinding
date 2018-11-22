using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace Solace.Channels
{
    class SolaceOperationSelector : IDispatchOperationSelector
    {
        public string SelectOperation(ref Message message)
        {
            return (string)message.Properties[SolaceConstants.ApplicationMessageTypeKey];
        }
    }
}
