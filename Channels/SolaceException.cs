using System;
using Newtonsoft.Json.Linq;

namespace Solace.Channels
{
    public class SolaceException : Exception
    {
        public SolaceException(string message) { }
        public SolaceException(string message, Exception innerException) { }
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
