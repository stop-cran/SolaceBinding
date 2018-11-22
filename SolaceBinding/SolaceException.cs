using Newtonsoft.Json.Linq;
using System;

namespace Solace.Channels
{
    public class SolaceException : Exception
    {
        public SolaceException(string message) : base(message)
        {
        }

        public SolaceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class SolaceJsonException : SolaceException
    {
        public JToken JsonException { get; }

        public SolaceJsonException(JToken json)
            : base(json.ToString())
        {
            JsonException = json;
        }

        public SolaceJsonException(JToken json, string message)
            : base(message)
        {
            JsonException = json;
        }
    }

    public class SolaceProtobufException : SolaceException
    {
        public SolaceProtobufException(Error error) : base(error.message)
        {
            Error = error;
        }

        public Error Error { get; }
    }
}
