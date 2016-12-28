using System;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.IO;
using ProtoBuf;

namespace Solace.Channels
{
    [ProtoContract]
    public class Error
    {
        [ProtoMember(1)]
        public string type { get; set; }

        [ProtoMember(2)]
        public string message { get; set; }

        [ProtoMember(3)]
        public Error inner { get; set; }
    }

    class SolaceProtobufErrorHandler : IErrorHandler
    {
        public bool HandleError(Exception error)
        {
            return true;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            var message = SolaceHelpers.SerializeMessage(EncodeError(error));

            if (fault != null)
            {
                message.Properties.CopyProperties(fault.Properties);
                message.Headers.CopyHeadersFrom(fault.Headers);
                fault.Close();
            }
            message.Properties[SolaceConstants.ApplicationMessageTypeKey] = "Fault";
            fault = message;
        }

        static Error WriteException(Exception ex)
        {
            return ex == null ? null : new Error
            {
                type = ex.GetType().FullName,
                message = ex.Message,
                inner = WriteException(ex.InnerException)
            };
        }

        static byte[] EncodeError(Exception error)
        {
            var jsonException = error as SolaceJsonException;

            if (jsonException != null)
                return System.Text.Encoding.UTF8.GetBytes(jsonException.JsonException.ToString());
            else
                using (var stream = new MemoryStream())
                {
                    Serializer.Serialize(stream, WriteException(error));
                    return stream.ToArray();
                }
        }
    }
}
