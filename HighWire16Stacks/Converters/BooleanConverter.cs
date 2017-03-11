using System;
using System.Globalization;
using System.Windows.Data;

namespace HighWire16Stacks.Converters
{
    internal class BooleanConverter : IValueConverter
    {
        public object TrueValue { get; set; }
        public object FalseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool && (bool)value) ? this.TrueValue : this.FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == this.TrueValue);
        }
    }
}
