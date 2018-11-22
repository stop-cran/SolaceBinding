using SolaceSystems.Solclient.Messaging;
using System;
using System.Threading;

namespace Solace.Channels
{
    internal class SolaceReplyEndpoint : SolaceEndpointBase
    {
        private readonly Action<ISession> reuseSession;
        private IMessage message;

        public SolaceReplyEndpoint(Uri address, ISession session, IMessage message, Action<ISession> reuseSession) : base(address)
        {
            this.message = message;
            Session = session;
            this.reuseSession = reuseSession;
        }

        public override ISolaceEndpoint Accept()
        {
            throw new NotSupportedException();
        }

        public override void Listen()
        {
            throw new NotSupportedException();
        }

        public override IMessage Receive(TimeSpan timeout) => Receive();

        public override IMessage Receive()
        {
            if (message == null)
                throw new InvalidOperationException();

            return Interlocked.Exchange(ref message, null);
        }

        public override void Close(int timeout)
        {
            base.Close(timeout);
            reuseSession(Session);
            Session = null;
        }
    }
}
