using ProtoBuf;
using System.IO;

namespace Solace.Channels
{
    public interface IProtobufConverter
    {
        byte[] SerializeRequest(object[] parameters);
        byte[] SerializeReply(object result);
        void DeserializeRequest(byte[] binary, object[] parametersToSet);
        object DeserializeReply(byte[] binary);
    }

    public abstract class ProtobufConverterBase : IProtobufConverter
    {
        public byte[] SerializeRequest(object[] parameters)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.NonGeneric.Serialize(stream, ToObject(parameters));

                return stream.ToArray();
            }
        }

        public abstract void DeserializeRequest(byte[] binary, object[] parametersToSet);
        public abstract byte[] SerializeReply(object result);
        public abstract object DeserializeReply(byte[] binary);

        protected abstract object ToObject(object[] parameters);
        protected abstract void FromObject(object value, object[] parametersToSet);
    }


    public abstract class ProtobufConverterBase<TParameters> : ProtobufConverterBase
    {
        public override void DeserializeRequest(byte[] binary, object[] parametersToSet)
        {
            using (var stream = new MemoryStream(binary))
                FromObject(Serializer.Deserialize<TParameters>(stream), parametersToSet);
        }

        public override byte[] SerializeReply(object result)
        {
            return null;
        }

        public override object DeserializeReply(byte[] binary)
        {
            if (binary.Length > 0)
                using (var stream = new MemoryStream(binary))
                    throw new SolaceProtobufException(Serializer.Deserialize<Error>(stream));
            return null;
        }
    }


    public abstract class ProtobufConverterBase<TParameters, TResult> : ProtobufConverterBase
    {
        public override void DeserializeRequest(byte[] binary, object[] parametersToSet)
        {
            using (var stream = new MemoryStream(binary))
                FromObject(Serializer.Deserialize<TParameters>(stream), parametersToSet);
        }

        public override byte[] SerializeReply(object result)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.NonGeneric.Serialize(stream, result);

                return stream.ToArray();
            }
        }

        public override object DeserializeReply(byte[] binary)
        {
            using (var stream = new MemoryStream(binary))
                try
                {
                    return Serializer.Deserialize<TResult>(stream);
                }
                catch (ProtoException)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    throw new SolaceProtobufException(Serializer.Deserialize<Error>(stream));
                }
        }
    }
}