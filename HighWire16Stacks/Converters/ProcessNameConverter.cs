using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace HighWire16Stacks.Converters
{
    public sealed class ProcessNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var proc = value as Process;
            if (proc == null)
                return null;

            return string.Format("{0}:{1}", proc.ProcessName, proc.Id);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
