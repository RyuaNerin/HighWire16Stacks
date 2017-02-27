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
            if (value.Length != 3 ||
                !(value[0] is bool) ||
                !(value[1] is bool) ||
                !(value[2] is bool))
                return null;
            
            var visible = (bool)value[0];
            if (!visible)
                return Visibility.Collapsed;

            if (!(value[2] is bool))
                return Visibility.Collapsed;

            // c > !c | s | v
            // 0 >  1 | 0 | 1
            // 1 >  0 | 0 | 0
            // 0 >  1 | 1 | 0
            // 1 >  0 | 1 | 1
            //
            // s ^ !c

            var v =(visible && ((bool)value[1] ^ !(bool)value[2])) ? Visibility.Visible : Visibility.Collapsed; 
            return  v;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
