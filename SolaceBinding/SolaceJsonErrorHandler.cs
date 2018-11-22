using Newtonsoft.Json;
using System;
using System.IO;

namespace Solace.Channels
{
    internal class SolaceJsonErrorHandler : SolaceErrorHandler
    {
        public override bool HandleError(Exception error)
        {
            return !(error is SolaceJsonException);
        }

        private static void WriteException(Exception ex, JsonTextWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("type");
            writer.WriteValue(ex.GetType().FullName);
            writer.WritePropertyName("message");
            writer.WriteValue(ex.Message);

            var inner = ex.InnerException;

            if (inner != null)
            {
                writer.WritePropertyName("inner");
                WriteException(inner, writer);
            }

            writer.WriteEndObject();
        }

        protected override void WriteException(Exception error, Stream stream, string action)
        {
            using (var writer = new StreamWriter(stream))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName(SolaceConstants.ErrorKey);
                WriteException(error, jsonWriter);
                jsonWriter.WriteEndObject();
            }
        }
    }
}
