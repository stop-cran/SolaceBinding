using System;

namespace Solace.Channels
{
    public interface IValueConverter
    {
        bool CanConvert(Type objectType);

        Type ConvertedType { get; }

        object Convert(object value);

        object ConvertBack(object value);
    }
}
