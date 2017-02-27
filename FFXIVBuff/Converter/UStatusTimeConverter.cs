using System;
using System.Globalization;
using System.Windows.Data;

namespace FFXIVBuff.Converter
{
    internal class UStatusTimeConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.Length != 2 || !(value[0] is float) || !(value[1] is bool))
                return " ";

            var remain = (float)value[0];
            if (remain == 0)
                return " ";

            if ((bool)value[1])
                if (remain > 60)
                    return string.Format("{0:##0.0}분", remain / 60);
                else
                    return string.Format("{0:##0.0}", remain);
            else
                if (remain > 60)
                    return string.Format("{0:##0}분", Math.Floor(remain / 60));
                else
                    return string.Format("{0:##0}", Math.Floor(remain));
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
