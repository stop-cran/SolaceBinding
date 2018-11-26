using System;
using System.ServiceModel.Configuration;

namespace Solace.Channels
{
    public class SolaceBinaryBehaviorExtensionElement : BehaviorExtensionElement
    {
        public override Type BehaviorType => typeof(SolaceBinaryEndpointBehavior);

        protected override object CreateBehavior() =>
            new SolaceBinaryEndpointBehavior();
    }
}
