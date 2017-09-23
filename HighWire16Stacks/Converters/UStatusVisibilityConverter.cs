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
            // 2 : IsOwn
            // 3 : ShowingModes
            // 4 : ShowOwnOnly

            if (value == null ||
                !(value[0] is bool visible) ||
                !(value[1] is bool isChecked) ||
                !(value[2] is bool isOwn) ||
                !(value[3] is ShowingModes showingMode) ||
                !(value[4] is bool showOwnOnly))
                return Visibility.Collapsed;

            if (!visible)
                return Visibility.Collapsed;

            if (showOwnOnly && !isOwn)
                return Visibility.Collapsed;

            if (showingMode == ShowingModes.ShowAll)
                return Visibility.Visible;

            if (showingMode == ShowingModes.HideChecked)
                isChecked = !isChecked;

            return isChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
