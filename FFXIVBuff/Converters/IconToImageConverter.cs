using System;
using System.Globalization;
using System.Windows.Data;
using FFXIVBuff.Core;

namespace FFXIVBuff.Converters
{
    public sealed class IconToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int ? FResource.GetImage((int)value) : null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
