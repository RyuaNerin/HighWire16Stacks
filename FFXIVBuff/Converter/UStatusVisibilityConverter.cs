using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FFXIVBuff.Converter
{
    internal class BoolToVisibilityConverter : IValueConverter
    {
        public Visibility WhenTrue { get; set; }
        public Visibility WhenFalse { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool && (bool)value) ? this.WhenTrue : this.WhenFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility && (Visibility)value == this.WhenTrue;
        }
    }
}
