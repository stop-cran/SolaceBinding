using System.Threading;
using SolaceSystems.Solclient.Messaging;
using System.Collections.Concurrent;
using System;

namespace Solace.Channels
{
    class SolaceEndpoint : SolaceEndpointBase, IDisposable
    {
        readonly BlockingCollection<ISession> replySessions = new BlockingCollection<ISession>();
        readonly BlockingCollection<IMessage> messages = new BlockingCollection<IMessage>();
        readonly EventHandler<SessionEventArgs> sessionEvent;
        bool subscribed;
        readonly Func<ISession> createSession;

        public SolaceEndpoint(Uri address, string vpn, string user, string password, int replySessionCount, EventHandler<SessionEventArgs> sessionEvent, IContext context)
            : base(address)
        {
            this.sessionEvent = sessionEvent;
            createSession = () => CreateSession(address, vpn, user, password,
                (sender, e) => messages.Add(e.Message),
                sessionEvent, context);

            session = createSession();

            replySessions.Add(session);

            for (int i = 0; i < replySessionCount; i++)
            {
                var replySession = createSession();
                replySession.Connect().EnsureSuccess();

                replySessions.Add(replySession);
            }
        }

        static ISession CreateSession(Uri address, string vpn, string user, string password, EventHandler<MessageEventArgs> messageEvent,
            EventHandler<SessionEventArgs> sessionEvent, IContext context)
        {
            return context.CreateSession(new SessionProperties
            {
                Host = address.Authority,
                VPNName = vpn,
                UserName = user,
                Password = password,
                ReconnectRetries = -1,
                ReconnectRetriesWaitInMsecs = 10000,
                ReapplySubscriptions = true
            }, messageEvent, sessionEvent);
        }

        public override ISolaceEndpoint Accept()
        {
            return new SolaceReplyEndpoint(RemoteEndPoint, replySessions.Take(), Receive(), replySessions.Add);
        }

        public override void Listen()
        {
            if (!subscribed)
            {
                session.Subscribe(topic, true).EnsureSuccess();
                subscribed = true;
            }
        }

        public override IMessage Receive(TimeSpan timeout)
        {
            if (timeout.TotalDays > 1)
                return messages.Take();
            else
                using (var c = new CancellationTokenSource(timeout))
                    return messages.Take(c.Token);
        }

        public override IMessage Receive()
        {
            return messages.Take();
        }

        public override void Close(int timeout)
        {
            if (subscribed)
            {
                session.Unsubscribe(topic, true);
                subscribed = false;
            }

            base.Close(timeout);
        }

        public void Dispose()
        {
            session.Dispose();
        }
    }
}