using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Solace.Channels
{
    public class SolaceProtobufMessageFormatter : IClientMessageFormatter, IDispatchMessageFormatter
    {
        readonly string applicationMessageType;
        readonly string replyApplicationMessageType;
        readonly IReadOnlyList<RequestParameter> operationParameters;

        public SolaceProtobufMessageFormatter(OperationDescription operation, IProtobufConverterFactory converterFactory)
        {
            applicationMessageType = operation.Name;
            replyApplicationMessageType = GetReplyApplicationMessageType(operation);

            operationParameters = GetRequestParameters(operation)
                .ToList()
                .AsReadOnly();

            this.Converter = converterFactory.Create(operationParameters, operation.Messages[1].Body.ReturnValue.Type);
        }

        public IProtobufConverter Converter { get; }

        static string GetReplyApplicationMessageType(OperationDescription operation)
        {
            var replyActionPrefix = operation.DeclaringContract.Namespace + operation.DeclaringContract.Name;
            var replyAction = operation.Messages[1].Action;

            return replyAction.StartsWith(replyActionPrefix)
                ? replyAction.Substring(replyActionPrefix.Length).TrimStart('/')
                : replyAction;
        }

        static IEnumerable<RequestParameter> GetRequestParameters(OperationDescription operation)
        {
            var properties = new HashSet<string>(from parameter in (operation.TaskMethod ?? operation.SyncMethod).GetParameters()
                                                 where parameter.GetCustomAttributes(typeof(MessageParameterAttribute), true).Any()
                                                 select parameter.Name);

            return operation.Messages[0].Body.Parts
                .Select(part => new RequestParameter
                {
                    Name = part.Name,
                    Index = part.Index,
                    Type = part.Type,
                    IsFromProperty = properties.Contains(part.Name),
                    IsRequired = properties.Contains(part.Name) ||
                        part.Type.IsValueType && (!part.Type.IsGenericType || part.Type.GetGenericTypeDefinition() != typeof(Nullable<>))
                });
        }

        public object DeserializeReply(Message message, object[] parameters)
        {
            return Converter.DeserializeReply(ReadMessageBinaryHelper(message));
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            var message = MessageBinaryHelper.SerializeMessage(Converter.SerializeRequest(parameters));

            message.Properties[SolaceConstants.ApplicationMessageTypeKey] = applicationMessageType;
            message.Properties[SolaceConstants.CorrelationIdKey] = new RequestCorrelationState();

            return message;
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            foreach (var part in operationParameters.Where(p => p.IsFromProperty))
            {
                object property;
                if (message.Properties.TryGetValue(part.Name, out property))
                    parameters[part.Index] = property;
                else
                    throw new ArgumentException("Required parameter was not provided.", part.Name);
            }

            Converter.DeserializeRequest(ReadMessageBinaryHelper(message), parameters);

            var notFilled = operationParameters.FirstOrDefault(p => !p.IsFromProperty && p.IsRequired && parameters[p.Index] == null);

            if (notFilled.IsRequired)
                throw new ArgumentException("Required parameter was not provided.", notFilled.Name);
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            var reply = MessageBinaryHelper.SerializeMessage(Converter.SerializeReply(result));

            reply.Properties[SolaceConstants.ApplicationMessageTypeKey] = replyApplicationMessageType;

            return reply;
        }

        static readonly byte[] empty = Encoding.UTF8.GetBytes("{}");

        static byte[] ReadMessageBinaryHelper(Message message)
        {
            var binary = MessageBinaryHelper.ReadMessageBinary(message);

            return binary.SequenceEqual(empty) ? new byte[0] : binary;
        }
    }
}
