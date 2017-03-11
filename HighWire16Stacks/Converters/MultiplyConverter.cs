using System;
using System.Globalization;
using System.Windows.Data;

namespace HighWire16Stacks.Converters
{
    public sealed class MultiplyConverter : IValueConverter
    {
        public double Multiply { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is double) ? ((double)value * this.Multiply) : 0d;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is double) ? ((double)value / this.Multiply) : 0d;
        }
    }
}
