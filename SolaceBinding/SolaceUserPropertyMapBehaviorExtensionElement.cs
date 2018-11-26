using System;
using System.ServiceModel.Configuration;

namespace Solace.Channels
{
    public class SolaceUserPropertyMapBehaviorExtensionElement : BehaviorExtensionElement
    {
        public override Type BehaviorType => typeof(SolaceUserPropertyMapEndpointBehavior);

        protected override object CreateBehavior() =>
            new SolaceUserPropertyMapEndpointBehavior();
    }
}
