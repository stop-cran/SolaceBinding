using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using SolaceBinding;
using System.Collections.ObjectModel;

namespace Solace.Channels
{
    class SolaceMessageFormatter : IClientMessageFormatter, IDispatchMessageFormatter
    {
        private readonly string applicationMessageType;
        private readonly string replyApplicationMessageType;
        private readonly ReadOnlyCollection<RequestParameter> operationParameters;
        private readonly Type returnType;

        public SolaceMessageFormatter(OperationDescription operation)
        {
            var replyActionPrefix = operation.DeclaringContract.Namespace + operation.DeclaringContract.Name;
            var replyAction = operation.Messages[1].Action;

            applicationMessageType = operation.Name;
            replyApplicationMessageType = replyAction.StartsWith(replyActionPrefix)
                ? replyAction.Substring(replyActionPrefix.Length).TrimStart('/')
                : replyAction;

            var properties = new HashSet<string>(from parameter in (operation.TaskMethod ?? operation.SyncMethod).GetParameters()
                                                 where parameter.GetCustomAttributes(typeof(MessageParameterAttribute), true).Any()
                                                 select parameter.Name);

            returnType = operation.Messages[1].Body.ReturnValue.Type;
            operationParameters = operation.Messages[0].Body.Parts
                .Select(part => new RequestParameter
                {
                    Name = part.Name,
                    Index = part.Index,
                    Type = part.Type,
                    IsFromProperty = properties.Contains(part.Name),
                    IsRequired = properties.Contains(part.Name) ||
                        part.Type.IsValueType && (!part.Type.IsGenericType || part.Type.GetGenericTypeDefinition() != typeof(Nullable<>))
                }).ToList()
                .AsReadOnly();
        }

        public object DeserializeReply(Message message, object[] parameters)
        {
            JObject json = SolaceHelpers.DeserializeMessage(message);
            return JsonConvert.DeserializeObject(json.ToString(), returnType);
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            var json = new JObject();

            foreach (var part in operationParameters)
            {
                object paramValue = parameters[part.Index];

                if (paramValue != null)
                    json.Add(part.Name, JToken.FromObject(paramValue));
            }

            var message = SolaceHelpers.SerializeMessage(json, null);

            message.Properties[SolaceConstants.ApplicationMessageTypeKey] = applicationMessageType;
            message.Properties[SolaceConstants.CorrelationIdKey] = new RequestCorrelationState();

            return message;
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            var json = SolaceHelpers.DeserializeMessage(message);

            foreach (var part in operationParameters)
                try
                {
                    int index = part.Index;

                    JToken value;

                    if (part.IsFromProperty)
                    {
                        object property;
                        if (message.Properties.TryGetValue(part.Name, out property))
                            parameters[index] = property;
                        else
                            throw new ArgumentException("Required parameter was not provided.", part.Name);
                    }
                    else if (json.TryGetValue(part.Name, out value))
                        parameters[index] = DeserializeParameterValue(part, value);
                    else if (part.IsRequired)
                        throw new ArgumentException("Required parameter was not provided.", part.Name);
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

        private static object DeserializeParameterValue(RequestParameter part, JToken value)
        {
            switch (value.Type)
            {
                case JTokenType.Object:
                case JTokenType.Array:
                    return JsonConvert.DeserializeObject(
                        value.ToString(), part.Type);
                case JTokenType.None:
                case JTokenType.Null:
                case JTokenType.Comment:
                    if (part.Type.IsValueType)
                        throw new ArgumentException("Required parameter was not provided.", part.Name);
                    return null;
                case JTokenType.String:
                    return JsonConvert.DeserializeObject($"\"{value}\"", part.Type);
                case JTokenType.Boolean:
                    return JsonConvert.DeserializeObject(((JValue)value).Value?.ToString().ToLowerInvariant(), part.Type);
                default:
                    return JsonConvert.DeserializeObject(((JValue)value).Value?.ToString(), part.Type);
            }
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            var reply = SolaceHelpers.SerializeMessage(result == null ? JValue.CreateNull() : JToken.FromObject(result), null);

            reply.Properties[SolaceConstants.ApplicationMessageTypeKey] = replyApplicationMessageType;

            return reply;
        }
    }
}
