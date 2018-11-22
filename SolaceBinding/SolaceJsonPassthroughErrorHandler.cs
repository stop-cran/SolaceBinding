using Newtonsoft.Json;
using System;
using System.IO;

namespace Solace.Channels
{
    internal class SolaceJsonPassthroughErrorHandler : SolaceJsonErrorHandler
    {
        public override bool HandleError(Exception error)
        {
            return error is SolaceJsonException;
        }

        protected override void WriteException(Exception error, Stream stream, string action)
        {
            using (var writer = new StreamWriter(stream))
            using (var jsonWriter = new JsonTextWriter(writer))
                ((SolaceJsonException)error).JsonException.WriteTo(jsonWriter);
        }
    }
}
