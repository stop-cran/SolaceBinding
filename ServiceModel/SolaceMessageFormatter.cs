using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Solace.ServiceModel
{
    class SolaceMessageFormatter : IClientMessageFormatter, IDispatchMessageFormatter
    {
        private OperationDescription operation;
        private static int nextId;

        public SolaceMessageFormatter(OperationDescription operation)
        {
            this.operation = operation;
        }

        public object DeserializeReply(Message message, object[] parameters)
        {
            JObject json = SolaceHelpers.DeserializeMessage(message);
            return JsonConvert.DeserializeObject(json.ToString(),
                this.operation.Messages[1].Body.ReturnValue.Type);
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            JObject json = new JObject();
            json.Add(SolaceConstants.MethodKey, this.operation.Name);

            foreach (MessagePartDescription part in this.operation.Messages[0].Body.Parts)
            {
                object paramValue = parameters[part.Index];

                if (paramValue != null)
                    json.Add(part.Name, JToken.FromObject(paramValue));
            }

            var message = SolaceHelpers.SerializeMessage(json, null);

            message.Properties["CorrelationId"] = Interlocked.Increment(ref nextId).ToString();

            return message;
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            var json = SolaceHelpers.DeserializeMessage(message);

            foreach (var part in operation.Messages[0].Body.Parts)
                try
                {
                    int index = part.Index;

                    JToken value;

                    if (json.TryGetValue(part.Name, out value))
                        parameters[index] = DeserializeParameterValue(part, value);
                    else
                    {
                        object property;

                        if (message.Properties.TryGetValue(part.Name, out property))
                            parameters[index] = property;
                        else
                            throw new ArgumentException("Required parameter was not provided.", part.Name);
                    }
                }
                catch (ArgumentException ex) when (ex.ParamName == part.Name)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Error retrieving parameter value", part.Name, ex);
                }
        }

        private static object DeserializeParameterValue(MessagePartDescription part, JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.Object:
                case JTokenType.Array:
                    return JsonConvert.DeserializeObject(
                        value.ToString(), part.Type);
                case JTokenType.None:
                case JTokenType.Null:
                    if (part.Type.IsValueType)
                        throw new ArgumentException("Required parameter was not provided.", part.Name);
                    return null;
                default:
                    return Convert.ChangeType(((JValue)value).Value, part.Type);
            }
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            return SolaceHelpers.SerializeMessage(result == null ? JValue.CreateNull() : JToken.FromObject(result), null);
        }
    }
}
