using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FFXIVBuff.Converter
{
    internal class UStatusVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.Length != 3)
                return null;

            if (!(value[1] is bool))
                return Visibility.Collapsed;
            
            var visible = (bool)value[0];
            if (!visible)
                return Visibility.Collapsed;

            // s | c | !c | v
            // 0 | 0 |  1 | 1
            // 0 | 1 |  0 | 0
            // 1 | 0 |  1 | 0
            // 1 | 1 |  0 | 1
            return (visible && ((bool)value[1] ^ !(bool)value[2])) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
