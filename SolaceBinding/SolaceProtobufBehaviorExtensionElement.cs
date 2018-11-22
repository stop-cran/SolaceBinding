using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Configuration;

namespace Solace.Channels
{
    public class SolaceProtobufBehaviorExtensionElement : BehaviorExtensionElement
    {
        public override Type BehaviorType => typeof(SolaceProtobufEndpointBehavior);

        protected virtual IEnumerable<IValueConverter> GetCustomConverters() =>
            Enumerable.Empty<IValueConverter>();

        protected override object CreateBehavior() =>
            new SolaceProtobufEndpointBehavior(new ProtobufConverterFactory(GetCustomConverters()));
    }
}
