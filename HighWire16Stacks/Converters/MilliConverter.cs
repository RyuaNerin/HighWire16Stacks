using System;
using System.Globalization;
using System.Windows.Data;

namespace HighWire16Stacks.Converters
{
    public sealed class MilliConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is double d) ? (d / 1000) : 0d;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
