using System;
using System.Globalization;
using System.Windows.Data;
using HighWire16Stacks.Core;

namespace HighWire16Stacks.Converters
{
    public sealed class OverlayStatusIconConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null &&
                value.Length == 2 &&
                value[0] is int statusId &&
                value[1] is bool use2x)
                return FResource.GetImage(statusId, use2x);
            else
                return null;
        }
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public sealed class StatusIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int statusId)
                return FResource.GetImage(statusId, false);
            else
                return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
