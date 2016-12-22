using System;

namespace Solace.Channels
{
    struct RequestParameter
    {
        public string Name;
        public int Index;
        public Type Type;
        public bool IsFromProperty;
        public bool IsRequired;
    }
}
