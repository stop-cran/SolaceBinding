using SolaceBinding.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Solace.Channels
{
    public class SolaceProtobufMessageFormatter : MessageFormatterBase, IClientMessageFormatter, IDispatchMessageFormatter
    {
        private readonly int? senderIdIndex;

        public SolaceProtobufMessageFormatter(OperationDescription operation, IProtobufConverterFactory converterFactory) : base(operation)
        {
            Converter = converterFactory.Create(OperationParameters, operation.IsOneWay ? typeof(void) : operation.Messages[1].Body.ReturnValue.Type);
            senderIdIndex = OperationParameters.Cast<RequestParameter?>().FirstOrDefault(p => p.Value.IsFromProperty && p.Value.Name == "SenderId")?.Index;
        }

        public IProtobufConverter Converter { get; }

        private static IEnumerable<RequestParameter> GetRequestParameters(OperationDescription operation)
        {
            var parameterNames = new HashSet<string>(from parameter in (operation.TaskMethod ?? operation.SyncMethod).GetParameters()
                                                     where parameter.GetCustomAttributes(typeof(FromMessagePropertyAttribute), true).Any()
                                                     select parameter.Name);
            var protoIndexes = (operation.TaskMethod ?? operation.SyncMethod).GetParameters().ToDictionary((ParameterInfo x) => x.Name,
                x => x.GetCustomAttributes(typeof(ProtoMemberAttribute), false).Cast<ProtoMemberAttribute>().FirstOrDefault()?.Order);

            return from part in operation.Messages[0].Body.Parts
                   let type = part.Type
                   let isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                   select new RequestParameter
                   {
                       Name = part.Name,
                       Index = part.Index,
                       ProtoIndex = (protoIndexes[part.Name] ?? (part.Index + 1)),
                       Type = part.Type,
                       IsFromProperty = parameterNames.Contains(part.Name),
                       IsRequired = (parameterNames.Contains(part.Name) || (part.Type.IsValueType && !isNullable)),
                       IsNullable = isNullable,
                       NullableTypeArgument = (isNullable ? type.GetGenericArguments()[0] : null)
                   };
        }

        public object DeserializeReply(Message message, object[] parameters) =>
            Converter.DeserializeReply(ReadMessageBinaryHelper(message));

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            var message = MessageBinaryHelper.SerializeMessage(Converter.SerializeRequest(parameters));

            if (senderIdIndex != null)
            {
                object obj = parameters[senderIdIndex.Value];

                if (obj != null)
                    message.Properties["SenderId"] = obj;
            }

            message.Properties["ApplicationMessageType"] = ApplicationMessageType;
            message.Properties["CorrelationId"] = new RequestCorrelationState();

            if (IsOneWay)
                message.Properties["IsOneWay"] = true;

            return message;
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            DeserializeMessageProperties(message, parameters);
            Converter.DeserializeRequest(ReadMessageBinaryHelper(message), parameters);

            var requestParameter = OperationParameters.FirstOrDefault(p => !p.IsFromProperty && p.IsRequired && parameters[p.Index] == null);

            if (requestParameter.IsRequired)
                throw new ArgumentException("Required parameter was not provided.", requestParameter.Name);
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            var message = MessageBinaryHelper.SerializeMessage(Converter.SerializeReply(result));

            message.Properties["ApplicationMessageType"] = ReplyApplicationMessageType;

            return message;
        }
    }
}
