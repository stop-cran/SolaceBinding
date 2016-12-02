using System;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using Newtonsoft.Json;
using System.IO;

namespace Solace.Channels
{
    class SolaceErrorHandler : IErrorHandler
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

        static void WriteException(JsonTextWriter writer, Exception ex)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("type");
            writer.WriteValue(ex.GetType().FullName);
            writer.WritePropertyName("message");
            writer.WriteValue(ex.Message);

            var inner = ex.InnerException;

            if (inner != null)
                WriteException(writer, inner);

            writer.WriteEndObject();
        }

        public static byte[] EncodeError(Exception error)
        {
            var jsonException = error as SolaceException;

            if (jsonException != null)
                return System.Text.Encoding.UTF8.GetBytes(jsonException.JsonException.ToString());
            else
                using (var stream = new MemoryStream())
                {
                    using (var writer = new StreamWriter(stream))
                    using (var jsonWriter = new JsonTextWriter(writer))
                    {
                        jsonWriter.WriteStartObject();
                        jsonWriter.WritePropertyName(SolaceConstants.ErrorKey);
                        WriteException(jsonWriter, error);
                        jsonWriter.WriteEndObject();
                    }

                    return stream.ToArray();
                }
        }
    }
}
