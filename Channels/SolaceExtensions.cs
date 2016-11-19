using SolaceSystems.Solclient.Messaging;
using System;
using System.ServiceModel.Channels;

namespace Solace.Channels
{
    public static class SolaceExtensions
    {
        public static void EnsureSuccess(this ReturnCode returnCode)
        {
            if (returnCode != ReturnCode.SOLCLIENT_OK)
                throw new Exception($"ReturnCode doesn't indicate success: {returnCode}");
        }

        public static ArraySegment<byte> ToBuffer(this IMessage message, BufferManager manager)
        {
            var attachment = message.BinaryAttachment;
            var buffer = manager.TakeBuffer(attachment.Length);

            Buffer.BlockCopy(attachment, 0, buffer, 0, attachment.Length);

            return new ArraySegment<byte>(buffer, 0, attachment.Length);
        }
    }
}
