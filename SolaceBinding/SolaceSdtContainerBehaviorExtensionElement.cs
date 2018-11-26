using System;
using System.ServiceModel.Configuration;

namespace Solace.Channels
{
    public class SolaceSdtContainerBehaviorExtensionElement : BehaviorExtensionElement
    {
        public override Type BehaviorType => typeof(SolaceSdtContainerEndpointBehavior);

        protected override object CreateBehavior() =>
            new SolaceSdtContainerEndpointBehavior();
    }
}
