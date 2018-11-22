using System;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Solace.Channels
{
    public class SolaceBinaryMessageFormatter : MessageFormatterBase, IClientMessageFormatter, IDispatchMessageFormatter
    {
        private readonly int? binaryParameterIndex;
        private readonly bool hasResult;

        public SolaceBinaryMessageFormatter(OperationDescription operation) : base(operation)
        {
            var parameters = (from x in OperationParameters
                              where !x.IsFromProperty
                              select x).ToList();

            if (parameters.Count > 1)
                throw new ArgumentException("Binary formatter can be applied only with operations with no or one byte[] parameter");

            if (parameters.Count == 1 && parameters[0].Type != typeof(byte[]))
                throw new ArgumentException("Binary formatter can be applied only with operations with no or one byte[] parameter");

            if (parameters.Count == 1)
                binaryParameterIndex = new int?(parameters[0].Index);

            if (!IsOneWay)
            {
                var messageDescription = operation.Messages.Skip(1).FirstOrDefault();
                Type type = messageDescription?.MessageType;

                if (type != null && type != typeof(void) && type != typeof(byte[]))
                    throw new ArgumentException("Binary formatter can be applied only with operations with void or byte[] result type");

                hasResult = type == typeof(byte[]);
            }
        }

        public object DeserializeReply(Message message, object[] parameters) =>
            ReadMessageBinaryHelper(message);

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            var message = MessageBinaryHelper.SerializeMessage(
                binaryParameterIndex != null
                    ? (byte[])parameters[binaryParameterIndex.Value]
                    : null);

            message.Properties["ApplicationMessageType"] = ApplicationMessageType;
            message.Properties["CorrelationId"] = new RequestCorrelationState();

            if (IsOneWay)
                message.Properties["IsOneWay"] = true;

            return message;
        }

        public void DeserializeRequest(Message message, object[] parameters)
        {
            DeserializeMessageProperties(message, parameters);

            byte[] array = ReadMessageBinaryHelper(message);

            if (binaryParameterIndex != null)
                parameters[binaryParameterIndex.Value] = array;
        }

        public Message SerializeReply(MessageVersion messageVersion, object[] parameters, object result)
        {
            var message = MessageBinaryHelper.SerializeMessage(hasResult ? (byte[])result : null);

            message.Properties["ApplicationMessageType"] = ReplyApplicationMessageType;

            return message;
        }
    }
}
