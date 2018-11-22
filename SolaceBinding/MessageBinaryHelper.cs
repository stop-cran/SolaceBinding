using System;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using Solace.Utils;

namespace Solace.Channels
{
    public static class MessageBinaryHelper
    {
        public static bool IsUntypedMessage(OperationDescription operation)
        {
            switch (operation.Messages[0].Body.Parts.Count)
            {
                case 1:
                    return operation.Messages[0].Body.Parts[0].Type == typeof(Message);
                case 0:
                    if (operation.IsOneWay)
                        return false;
                    Type returnType = operation.Messages[1].Body.ReturnValue.Type;
                    return returnType == typeof(void) || returnType == typeof(Message);
                default:
                    return false;
            }
        }

        public static Message SerializeMessage(byte[] body)
        {
            return Message.CreateMessage(MessageVersion.None, null, new RawBodyWriter(body ?? new byte[0]));
        }

        public static byte[] ReadMessageBinary(Message message)
        {
            using (var bodyReader = message.GetReaderAtBodyContents())
            {
                bodyReader.ReadStartElement("Binary");
                return bodyReader.ReadContentAsBase64();
            }
        }
    }
}
