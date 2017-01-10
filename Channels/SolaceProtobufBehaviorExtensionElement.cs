using System;
using System.Collections.Generic;
using System.ServiceModel.Configuration;

namespace Solace.Channels
{
    public class SolaceProtobufBehaviorExtensionElement : BehaviorExtensionElement
    {
        public override Type BehaviorType
        {
            get
            {
                return typeof(SolaceProtobufEndpointBehavior);
            }
        }

        protected virtual IEnumerable<IValueConverter> GetCustomConverters()
        {
            yield break;
        }

        protected override object CreateBehavior()
        {
            return new SolaceProtobufEndpointBehavior(new ProtobufConverterFactory(GetCustomConverters()));
        }
    }
}
