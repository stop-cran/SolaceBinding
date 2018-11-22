using System.ServiceModel.Channels;
using System.Xml;

namespace Solace.Utils
{
    internal class RawBodyWriter : BodyWriter
    {
        private byte[] bytes;

        public RawBodyWriter(byte[] bytes)
            : base(true)
        {
            this.bytes = bytes;
        }

        protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
        {
            writer.WriteStartElement("Binary");
            writer.WriteBase64(bytes, 0, bytes.Length);
            writer.WriteEndElement();
        }
    }
}
