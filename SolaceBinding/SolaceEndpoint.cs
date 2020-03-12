using SolaceSystems.Solclient.Messaging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Solace.Channels
{
    internal class SolaceEndpoint : SolaceEndpointBase, IDisposable
    {
        private static readonly long processId = Process.GetCurrentProcess().Id;
        private static long sessionNumber;
        private readonly BlockingCollection<ISession> replySessions = new BlockingCollection<ISession>();
        private readonly BlockingCollection<IMessage> messages = new BlockingCollection<IMessage>();
        private readonly EventHandler<SessionEventArgs> sessionEvent;
        private bool subscribed;
        private readonly Func<ISession> createSession;

        public SolaceEndpoint(Uri address, string vpn, string user, string password, int replySessionCount,
            string clientNameSuffix,
            EventHandler<SessionEventArgs> sessionEvent, IContext context)
            : base(address)
        {
            this.sessionEvent = sessionEvent;
            createSession = () => CreateSession(address, vpn, user, password,
                clientNameSuffix == null ? null : $"{clientNameSuffix}/{Environment.MachineName}/{processId}/{{0}}",
                (sender, e) => messages.Add(e.Message),
                sessionEvent, context);

            Session = createSession();

            replySessions.Add(Session);

            for (int i = 0; i < replySessionCount; i++)
            {
                var replySession = createSession();
                replySession.Connect()
                    .EnsureSuccess($"Failed to connect reply session", replySession.Properties);

                replySessions.Add(replySession);
            }
        }

        private static ISession CreateSession(Uri address, string vpn, string user, string password,
            string clientNameFormat,
            EventHandler<MessageEventArgs> messageEvent,
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
                ConnectRetries = -1,
                ConnectTimeoutInMsecs = 10000,
                ReapplySubscriptions = true,
                ClientName = clientNameFormat == null ? null : string.Format(clientNameFormat, Interlocked.Increment(ref sessionNumber))
            }, messageEvent, sessionEvent);
        }

        public override ISolaceEndpoint Accept() =>
            new SolaceReplyEndpoint(RemoteEndpoint, replySessions.Take(), Receive(), replySessions.Add);

        public override void Listen()
        {
            if (!subscribed)
            {
                Session.Subscribe(Topic, true)
                    .EnsureSuccess($"Failed to subscribe on topic {Topic}", Session.Properties);
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

        public override IMessage Receive() => messages.Take();

        public override void Close(int timeout)
        {
            if (subscribed)
            {
                Session.Unsubscribe(Topic, true);
                subscribed = false;
            }

            base.Close(timeout);
        }

        public void Dispose() =>
            Session.Dispose();
    }
}