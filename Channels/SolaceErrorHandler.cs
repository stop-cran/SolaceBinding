using System;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.IO;

namespace Solace.Channels
{
    public abstract class SolaceErrorHandler : IErrorHandler
    {
        public abstract bool HandleError(Exception error);

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            if (HandleError(error))
            {
                Message message;

                using (var stream = new MemoryStream())
                {
                    WriteException(error, stream,
                        fault == null ? null : fault.Properties[SolaceConstants.ApplicationMessageTypeKey].ToString());
                    message = MessageBinaryHelper.SerializeMessage(stream.ToArray());
                }

                if (fault != null)
                {
                    message.Properties.CopyProperties(fault.Properties);
                    message.Headers.CopyHeadersFrom(fault.Headers);
                    fault.Close();
                }

                message.Properties[SolaceConstants.ApplicationMessageTypeKey] = "Fault";
                fault = message;
            }
        }
        
        protected abstract void WriteException(Exception error, Stream stream, string action);
    }
}
