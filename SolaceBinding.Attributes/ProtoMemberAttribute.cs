using System;

namespace SolaceBinding.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ProtoMemberAttribute : Attribute
    {
        public ProtoMemberAttribute(int order)
        {
            this.Order = order;
        }

        public int Order { get; }
    }
}
