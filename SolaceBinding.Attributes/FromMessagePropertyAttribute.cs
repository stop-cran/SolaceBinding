using System;

namespace SolaceBinding.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromMessagePropertyAttribute : Attribute
    {
        public FromMessagePropertyAttribute()
        {
        }

        public FromMessagePropertyAttribute(string propertyName)
        {
            this.PropertyName = propertyName;
        }

        public string PropertyName { get; }
    }
}
