using SolaceSystems.Solclient.Messaging;
using System;
using System.Collections.Concurrent;

namespace Solace.Channels
{
    internal class SolaceEndpointCache
    {
        private readonly ConcurrentDictionary<Uri, SolaceEndpoint> endpoints = new ConcurrentDictionary<Uri, SolaceEndpoint>();
        private readonly string vpn, user, password, clientNameSuffix;
        private readonly EventHandler<SessionEventArgs> sessionEvent;
        private readonly IContext context;
        private readonly int replySessionCount;

        public SolaceEndpointCache(string vpn, string user, string password, int replySessionCount,
            string clientNameSuffix,
            EventHandler<SessionEventArgs> sessionEvent)
        {
            this.vpn = vpn;
            this.user = user;
            this.password = password;
            this.sessionEvent = sessionEvent;
            this.replySessionCount = replySessionCount;
            this.clientNameSuffix = clientNameSuffix;
            context = ContextFactory.Instance.CreateContext(new ContextProperties(), null);
        }

        public SolaceEndpoint Create(Uri address) =>
            endpoints.GetOrAdd(address, uri =>
                new SolaceEndpoint(uri, vpn, user, password,
                    replySessionCount,
                    clientNameSuffix,
                    sessionEvent, context));
    }
}
