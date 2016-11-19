using System;
using Newtonsoft.Json.Linq;

namespace Solace.ServiceModel
{
    public class SolaceException : Exception
    {
        public JToken JsonException
        {
            get;
            private set;
        }

        public SolaceException(JToken json)
            : base()
        {
            this.JsonException = json;
        }

        public SolaceException(JToken json, string message)
            : base(message)
        {
            this.JsonException = json;
        }

        public SolaceException(JToken json, string message, Exception innerException)
            : base(message, innerException)
        {
            this.JsonException = json;
        }
    }
}
