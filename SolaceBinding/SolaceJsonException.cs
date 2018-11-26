using Newtonsoft.Json.Linq;

namespace Solace.Channels
{
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
}
