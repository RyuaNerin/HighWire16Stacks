using System;
using System.Globalization;
using System.Windows.Data;
using HighWire16Stacks.Core;

namespace HighWire16Stacks.Converters
{
    internal class ShowingModesEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ShowingModes sm ? (int)sm : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int i ? (ShowingModes)i : ShowingModes.ShowAll;
        }
    }
}
