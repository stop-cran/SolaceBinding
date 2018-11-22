using System;

namespace Solace.Channels
{
    public struct RequestParameter
    {
        public string Name;
        public int Index;
        public int ProtoIndex;
        public Type Type;
        public bool IsFromProperty;
        public bool IsRequired;
        public bool IsBinary;
        public bool IsNullable;
        public Type NullableTypeArgument;
    }
}
