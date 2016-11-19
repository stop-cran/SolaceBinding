using System;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using Newtonsoft.Json.Linq;

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
            fault = SolaceHelpers.SerializeMessage(EncodeError(error), fault);
        }

        public static JObject EncodeError(Exception error)
        {
            JObject json = new JObject();
            SolaceException jsonException = error as SolaceException;
            if (jsonException != null)
            {
                json.Add(SolaceConstants.ErrorKey, jsonException.JsonException);
            }
            else
            {
                JObject exceptionJson = new JObject
                {
                    { "type", error.GetType().FullName },
                    { "message", error.Message },
                };
                JObject temp = exceptionJson;
                while (error.InnerException != null)
                {
                    error = error.InnerException;
                    JObject innerJson = new JObject
                    {
                        { "type", error.GetType().FullName },
                        { "message", error.Message },
                    };
                    temp["inner"] = innerJson;
                    temp = innerJson;
                }

                json.Add(SolaceConstants.ErrorKey, exceptionJson);
            }

            return json;
        }
    }
}
