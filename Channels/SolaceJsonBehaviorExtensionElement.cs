using Newtonsoft.Json;
using System;
using System.ServiceModel.Configuration;

namespace Solace.Channels
{
    public class SolaceJsonBehaviorExtensionElement : BehaviorExtensionElement
    {
        public override Type BehaviorType
        {
            get
            {
                return typeof(SolaceJsonEndpointBehavior);
            }
        }

        protected virtual JsonSerializerSettings GetSerializerSettings()
        {
            return new JsonSerializerSettings();
        }

        protected override object CreateBehavior()
        {
            return new SolaceJsonEndpointBehavior(GetSerializerSettings);
        }
    }
}
