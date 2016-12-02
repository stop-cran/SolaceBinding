using System;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using System.IO;
using Newtonsoft.Json;
using Solace.Utils;

namespace Solace.Channels
{
    static class SolaceHelpers
    {
        public static bool IsUntypedMessage(OperationDescription operation)
        {
            switch (operation.Messages[0].Body.Parts.Count)
            {
                case 1:
                    return operation.Messages[0].Body.Parts[0].Type == typeof(Message);
                case 0:
                    Type returnType = operation.Messages[1].Body.ReturnValue.Type;
                    return returnType == typeof(void) || returnType == typeof(Message);
                default:
                    return false;
            }
        }

        public static Message SerializeMessage(byte[] body)
        {
            return Message.CreateMessage(MessageVersion.None, null, new RawBodyWriter(body));
        }

        public static JsonTextReader DeserializeMessage(Message message)
        {
            object reader;

            if (message.Properties.TryGetValue(SolaceConstants.ReplyReaderKey, out reader))
                return (JsonTextReader)reader;

            using (var bodyReader = message.GetReaderAtBodyContents())
            {
                bodyReader.ReadStartElement("Binary");
                return new JsonTextReader(new StreamReader(new MemoryStream(bodyReader.ReadContentAsBase64()))) { CloseInput = true };
            }
        }
    }
}
