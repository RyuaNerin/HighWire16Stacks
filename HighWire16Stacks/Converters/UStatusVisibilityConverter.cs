using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using HighWire16Stacks.Core;

namespace HighWire16Stacks.Converters
{
    internal class UStatusVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            // 0 : visible
            // 1 : checked
            // 2 : ShowingModes

            if (value == null) return Visibility.Collapsed;

            if (value[0] is bool visible && !visible)
                return Visibility.Collapsed;

            if (!(value[2] is ShowingModes sm))
                return Visibility.Collapsed;

            if (sm == ShowingModes.ShowAll)
                return Visibility.Visible;

            if (!(value[1] is bool @checked))
                return Visibility.Collapsed;

            if (sm == ShowingModes.HideChecked)
                @checked = !@checked;

            return @checked ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
