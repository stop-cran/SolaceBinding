using System.Threading;
using SolaceSystems.Solclient.Messaging;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;

namespace Solace.Channels
{
    public class SolaceEndpoint : IDisposable
    {
        IContext context;
        ISession session;
        BlockingCollection<IMessage> messages = new BlockingCollection<IMessage>();
        ITopic topic;

        public SolaceEndpoint(Uri address, string vpn, string user, string password)
        {
            RemoteEndPoint = address;
            context = ContextFactory.Instance.CreateContext(new ContextProperties(), null);
            session = context.CreateSession(new SessionProperties
            {
                Host = address.Authority,
                VPNName = vpn,
                UserName = user,
                Password = password,
                ReconnectRetries = 10,
                ReconnectRetriesWaitInMsecs = 10000,
                ReapplySubscriptions = true
            }, (sender, e) => messages.Add(e.Message), null);

            topic = ContextFactory.Instance.CreateTopic(address.AbsolutePath.Replace("%3E", ">").TrimStart('/'));
        }

        public Uri RemoteEndPoint
        {
            get; private set;
        }

        public bool Connected
        {
            get; private set;
        }

        public SolaceEndpoint Accept()
        {
            var message = Receive(); // block till recieving a message
            var properties = session.Properties;
            var res = new SolaceEndpoint(RemoteEndPoint, properties.VPNName, properties.UserName, properties.Password);

            res.messages.Add(message);
            res.Connect();
            res.Listen();

            return res;
        }

        public void Connect()
        {
            session.Connect().EnsureSuccess();
            Connected = true;
        }

        public void Close()
        {
            Close(session.Properties.ConnectTimeoutInMsecs);
        }

        public void Close(int timeout)
        {
            session.Disconnect().EnsureSuccess();
            Connected = false;
        }


        public void Listen()
        {
            session.Subscribe(topic, true).EnsureSuccess();
        }

        public void SendReply(IDestination destination, string correlationId, byte[] buffer)
        {
            if (buffer != null)
            {
                var request = session.CreateMessage();
                var message = session.CreateMessage();

                request.ReplyTo = destination;
                request.CorrelationId = correlationId;

                message.ApplicationMessageType = "WTS.01.v1";
                message.BinaryAttachment = buffer;

                session.SendReply(request, message).EnsureSuccess();
            }
        }

        public byte[] SendRequest(byte[] buffer, string applicationMessageType, TimeSpan timeout)
        {
            var message = session.CreateMessage();

            message.Destination = topic;
            message.BinaryAttachment = buffer;
            message.ApplicationMessageType = applicationMessageType;

            IMessage reply;

            session.SendRequest(message, out reply, (int)timeout.TotalMilliseconds).EnsureSuccess();

            return reply.BinaryAttachment;
        }

        public void SendReply(IDestination destination, string correlationId, ArraySegment<byte> buffer)
        {
            SendReply(destination, correlationId, Copy(buffer));
        }

        private static byte[] Copy(ArraySegment<byte> buffer)
        {
            byte[] attachment;

            if (buffer.Array == null || buffer.Offset == 0 && buffer.Count == buffer.Array.Length)
                attachment = buffer.Array;
            else
            {
                attachment = new byte[buffer.Count];
                Buffer.BlockCopy(buffer.Array, buffer.Offset, attachment, 0, buffer.Count);
            }

            return attachment;
        }

        public byte[] SendRequest(ArraySegment<byte> buffer, string applicationMessageType, TimeSpan timeout)
        {
            return SendRequest(Copy(buffer), applicationMessageType, timeout);
        }


        public IMessage Receive(TimeSpan timeout)
        {
            if (timeout.TotalDays > 1)
                return messages.Take();
            else
                using (var c = new CancellationTokenSource(timeout))
                    return messages.Take(c.Token);
        }

        public IMessage Receive()
        {
            return messages.Take();
        }

        public IAsyncResult BeginConnect(AsyncCallback callback, object state)
        {
            return Task.Run((Action)Connect).ContinueWith(t =>
            {
                t.Exception?.ToString();
                callback(t);
            });
        }

        public void EndConnect(IAsyncResult asyncResult)
        {
            ((Task)asyncResult).Wait();
        }

        public void EndSendReply(IAsyncResult asyncResult)
        {
            ((Task)asyncResult).Wait();
        }

        public IAsyncResult BeginReceive(AsyncCallback callback, object state)
        {
            return TaskHelper.CreateTask(() => Receive(), callback, state);
        }

        public IMessage EndReceive(IAsyncResult asyncResult)
        {
            return ((Task<IMessage>)asyncResult).Result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                session.Dispose();
                context.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SolaceEndpoint()
        {
            Dispose(false);
        }
    }
}