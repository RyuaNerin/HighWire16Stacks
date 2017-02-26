using System;
using System.Globalization;
using System.Windows.Data;

namespace FFXIVBuff.Converter
{
    public sealed class PersentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? 0 : (double)value * 100;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? 0 : (double)value / 100;
        }
    }
}
