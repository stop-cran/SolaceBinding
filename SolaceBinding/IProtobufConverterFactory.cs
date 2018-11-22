using System;
using System.Collections.Generic;

namespace Solace.Channels
{
    public interface IProtobufConverterFactory
    {
        IProtobufConverter Create(
            IEnumerable<RequestParameter> parameters,
            Type returnType);
    }
}
