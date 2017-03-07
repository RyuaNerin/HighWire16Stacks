using System;
using System.Globalization;
using System.Windows.Data;

namespace FFXIVBuff.Converters
{
    internal class Ms2FpsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is double) ? Math.Ceiling(1000 / (double)value) : 0d;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
