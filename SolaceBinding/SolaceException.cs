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

    public class SolaceProtobufException : SolaceException
    {
        public SolaceProtobufException(Error error) : base(error.message)
        {
            Error = error;
        }

        public Error Error { get; }
    }
}
