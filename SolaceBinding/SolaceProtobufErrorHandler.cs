using ProtoBuf;
using System;
using System.IO;

namespace Solace.Channels
{
    internal class SolaceProtobufErrorHandler : SolaceErrorHandler
    {
        public override bool HandleError(Exception error) => true;

        private static Error ToErrorObject(Exception ex) =>
            ex == null ? null : new Error
            {
                type = ex.GetType().FullName,
                message = ex.Message,
                inner = ToErrorObject(ex.InnerException)
            };

        protected override void WriteException(Exception error, Stream stream, string action) =>
            Serializer.Serialize(stream, ToErrorObject(error));
    }


    [ProtoContract]
    public class Error
    {
        [ProtoMember(1)]
        public string type { get; set; }

        [ProtoMember(2)]
        public string message { get; set; }

        [ProtoMember(3)]
        public Error inner { get; set; }
    }
}
