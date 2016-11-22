using Newtonsoft.Json.Linq;
using SolaceSystems.Solclient.Messaging;
using System;
using System.ServiceModel.Channels;
using System.Text;

namespace Solace.Channels
{
    public static class SolaceExtensions
    {
        static readonly byte[] EmptyObject = Encoding.UTF8.GetBytes(new JObject().ToString());

        public static void EnsureSuccess(this ReturnCode returnCode)
        {
            if (returnCode != ReturnCode.SOLCLIENT_OK)
                throw new Exception($"ReturnCode doesn't indicate success: {returnCode}");
        }

        public static ArraySegment<byte> ToBuffer(this IMessage message, BufferManager manager)
        {
            var attachment = message?.BinaryAttachment ?? EmptyObject;
            var buffer = manager.TakeBuffer(attachment.Length);

            Buffer.BlockCopy(attachment, 0, buffer, 0, attachment.Length);

            return new ArraySegment<byte>(buffer, 0, attachment.Length);
        }
    }
}
