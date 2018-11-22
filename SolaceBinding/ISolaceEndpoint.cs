using SolaceSystems.Solclient.Messaging;
using System;

namespace Solace.Channels
{
    public interface ISolaceEndpoint
    {
        Uri RemoteEndPoint { get; }

        string SubscribedTopic { get; }

        bool Connected { get; }

        ISolaceEndpoint Accept();

        void Connect();

        void Close();

        void Close(int timeout);

        void Listen();

        void SendReply(IDestination destination, string correlationId, string applicationMessageType, byte[] buffer);

        IMessage SendRequest(byte[] buffer, string applicationMessageType, string senderId, string topicSuffix, TimeSpan timeout);

        void SendReply(IDestination destination, string correlationId, string applicationMessageType, ArraySegment<byte> buffer);

        IMessage SendRequest(ArraySegment<byte> buffer, string applicationMessageType, string senderId, string topicSuffix, TimeSpan timeout);

        void Send(ArraySegment<byte> buffer, string correlationId, string applicationMessageType, string senderId, string topicSuffix);

        IMessage Receive(TimeSpan timeout);

        IMessage Receive();
    }
}
