using System;
using System.Globalization;
using System.Windows.Data;

namespace FFXIVBuff.Converters
{
    public sealed class MilliConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is double) ? ((double)value / 1000) : 0d;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
